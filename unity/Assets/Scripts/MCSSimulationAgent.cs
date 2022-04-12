using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public enum AgentType : int {
	ToonPeopleFemale = 0,
	ToonPeopleMale = 1
}

public class MCSSimulationAgent : MonoBehaviour {
    private static int ELDER_BLEND_SHAPE = 26;

    public AgentType type;
    public ObjectMaterialOption beard = null;
    public HeadObjectMaterialOption head;
    public ObjectMaterialOption glasses;

    public Material[] skinOptions;
    public Material[] elderSkinOptions;

    public ChestObjectMaterialOption[] chestOptions;
    public SkinObjectMaterialOption[] feetOptions;
    public HairObjectMaterialOption[] hairOptions;
    public ObjectMaterialOption[] jacketOptions;
    public SkinObjectMaterialOption[] legsOptions;
    public ObjectMaterialOption[] tieOptions;

    private ChestObjectMaterialOption chest = null;
    private bool elder = false;
    private SkinObjectMaterialOption feet = null;
    private HairObjectMaterialOption hair = null;
    private ObjectMaterialOption jacket = null;
    private SkinObjectMaterialOption legs = null;
    public SkinObjectMaterialOption facelessHead = null;
    private int skin = 0;
    private ObjectMaterialOption tie = null;
    private MCSController mcsController;


    [Header("Animation")]
    [SerializeField] private int currentAnimationFrame = 0;
    private Animator animator;
    private static int ANIMATION_FRAME_RATE = 25;
    [SerializeField] private string currentClip;
    private Dictionary<string, float> clipNamesAndDurations = new Dictionary<string,float>();
    [SerializeField] private bool resetAnimationToIdleAfterPlayingOnce = false;
    [SerializeField] private int stepToEndAnimation = -1;
    
    
    //local position adjustments of the held object throughout the animation sequence, calculated by hand in the editor
    private static Vector3 REACH_INTO_BACK_POSITION_1 = new Vector3(0.0057f, 1.41f, -0.156f);
    private static Vector3 REACH_INTO_BACK_POSITION_2 = new Vector3(0.1296f, 1.4736f, 0.1247f);
    private static Vector3 REACH_INTO_BACK_POSITION_3 = new Vector3(0.032f, 1.417f, 0.191f);
    private static Vector3 HOLDING_POSITION = new Vector3(0.002f, 1.119f, 0.1907f);
    private static Vector3[] AGENT_INTERACTION_ACTION_OBJECT_POSITIONS = {Vector3.zero, REACH_INTO_BACK_POSITION_1, REACH_INTO_BACK_POSITION_2, REACH_INTO_BACK_POSITION_3};
    private static string[] AGENT_INTERACTION_ACTION_ANIMATIONS = {"TPF_phone1", "TPM_phone1", "TPM_phone1", "TPM_phone2"};
    private static string NOT_HOLDING_OBJECT_ANIMATION = "TPM_idle5";
    private static int NOT_HOLDING_OBJECT_ANIMATION_LENGTH = 5;
    private static int NOT_HOLDING_OBJECT_STARTING_FRAME = 150;
    private static int ANIMATION_FRAME_TO_ENHANCE_INTERACTION_ACTION = 34; //this makes the interaction action looks more believable.
    private static string TURN_LEFT = "TPM_turnL45";
    private static string TURN_RIGHT = "TPM_turnR45";
    private static float MIMIMUM_ROTATION_ANGLE_FOR_ANIMATION = 15f;
    
    [Header("Interaction Animation")]
    public SimObjPhysics heldObject;
    public bool isHoldingHeldObject;
    [SerializeField] private int currentGetHeldObjectAnimation = 0;
    public bool rotatingToFacePerformer = false;
    [SerializeField] private float rotationPercent = 0;
    [SerializeField] private Vector3 originalRotation;
    [SerializeField] private Vector3 targetRotation;
    public SimAgentActionState simAgentActionState = SimAgentActionState.Idle;
    [SerializeField] private bool resetOncePickedUp = false;
    [SerializeField] private bool previousClipWasLoop = false;
    public string previousClip = "";
    [SerializeField] private int previousCurrentFrame = 0;
    [SerializeField] private int previousClipStepEnd = -1;
    public int delayedStepBeginAction = -1;
    public int delayedStepEnd = -1;
    public bool delayedIsLoopAnimation = false;
    public string delayedAnimation = "";
    private static string IDLE_FEMALE = "TPF_idle1";
    private static string IDLE_MALE = "TPM_idle1";
    private int notHoldingObjectCurrentFrame = 0;
    public enum SimAgentActionState {
        Action,
        Idle,
        InteractingHoldingHeldObject,
        InteractingNotHoldingHeldObject,
        HoldingOutHeldObject,
        Moving,
        Rotating,
        None
    }
    
    [Header("Movement")]
    [SerializeField] private MCSConfigSimAgentMovement agentMovement = null;
    public List<int> actionsStepBegins = null;
    [SerializeField] private int moveIndex = 0;
    [SerializeField] private Vector3 targetPos = Vector3.zero;
    [SerializeField] private Vector3 direction;
    [SerializeField] private bool previouslyWasMoving;
    [SerializeField] private bool doneWithMovementSequence = false;
    [SerializeField] private int delayedStepBeginMovement = -1;
    private static string MOVEMENT_TURNS_LEFT = "TPM_turnL45";
    private static string MOVEMENT_TURNS_RIGHT = "TPM_turnR45";
    private static float MOVE_MAGNITUDE = 0.04f;

    [SerializeField] private bool obstructed = false;
    private bool obstructedAnimationSet = false;
    private List<Collider> collisions = new List<Collider>();
    private CapsuleCollider cc;


    void Awake() {
        // Activate a default chest, legs, and feet option so we won't have a disembodied floating head.
        this.GetFacelessHead();
        this.SetChest(0, 0);
        this.SetFeet(0, 0);
        this.SetLegs(0, 0);
        this.SetEyes(0);
        this.SetSkin(0);
        this.SetElder(false);
        // Deactivate all the optional body parts and accessories by default.
        this.glasses.gameObject.SetActive(false);
        this.DeactivateGameObjects(this.hairOptions);
        this.DeactivateGameObjects(this.jacketOptions);
        this.DeactivateGameObjects(this.tieOptions);
        if (this.beard != null && this.beard.gameObject != null) {
            this.beard.gameObject.SetActive(false);
        }
        this.animator = this.gameObject.GetComponent<Animator>();
        animator.speed = 0;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips) {
            clipNamesAndDurations.Add(clip.name, clip.length);
        }
        mcsController = FindObjectOfType<MCSController>();
        mcsController.simulationAgents.Add(this);
        SetDefaultAnimation();
        IncrementAnimationFrame();
        
        currentGetHeldObjectAnimation = 0;
        isHoldingHeldObject = false;
        resetOncePickedUp = false;
        
        cc = GetComponent<CapsuleCollider>();
    }
    
    void Start() {
        if(heldObject != null) {
            isHoldingHeldObject = true;
            foreach(Collider c in heldObject.MyColliders)
                c.enabled = false;
            heldObject.transform.parent = this.transform;
            heldObject.transform.localPosition = Vector3.zero;
            heldObject.GetComponent<Rigidbody>().isKinematic = true;
            heldObject.gameObject.SetActive(false);
        }
    }

    void Update() {
        Vector3 p1 = transform.TransformPoint(new Vector3(cc.center.x, cc.center.y - cc.height/2 + cc.radius, cc.center.z)) + (transform.forward.normalized * MOVE_MAGNITUDE);
        Vector3 p2 = transform.TransformPoint(new Vector3(cc.center.x, cc.center.y + cc.height/2 - cc.radius, cc.center.z)) + (transform.forward.normalized * MOVE_MAGNITUDE);
        collisions = Physics.OverlapCapsule(p1, p2, cc.radius * transform.localScale.x, 1 << 8 | 1 << 10, QueryTriggerInteraction.Ignore).ToList();
        foreach (Collider c in collisions) {
            //if not the floor or this capsule collider then the agent is colliding with something and needs to stop
            bool isFloor = c.GetComponent<StructureObject>() != null && (c.name.Contains("Floor") || c.name.Contains("floor")); 
            if (!isFloor && c!=cc) {
                if(!obstructed)
                    obstructedAnimationSet = false;
                obstructed = true;
                return;
            }
        }
        obstructed = false;
    }

    public void SetDefaultAnimation(bool usePreviousClip = false, string name = null, bool interactionComplete = false) {
        if(previouslyWasMoving && !doneWithMovementSequence && !obstructed) {
            bool completed = MoveSimAgent(interactionComplete: interactionComplete);
            if(completed) 
                return;
        }
        if(usePreviousClip && previousClip != "") {
            AssignClip(previousClip);
            AnimationPlaysOnce(isLoopAnimation: previousClipWasLoop);
            if(previousClip == IDLE_FEMALE || previousClip == IDLE_MALE)
                simAgentActionState = SimAgentActionState.Idle;
            else
                simAgentActionState = SimAgentActionState.Action;
            return;
        }
        if (this.type == AgentType.ToonPeopleFemale) {
            AssignClip(name != null ? name : IDLE_FEMALE);
        }
        if (this.type == AgentType.ToonPeopleMale) {
            AssignClip(name != null ? name : IDLE_MALE);
        }
        simAgentActionState = SimAgentActionState.Idle;
        AnimationPlaysOnce(isLoopAnimation: true);
        currentAnimationFrame = 0;

    }

    public void PlayGetHeldObjectAnimation() {
        heldObject.gameObject.SetActive(true);
        heldObject.transform.localPosition = AGENT_INTERACTION_ACTION_OBJECT_POSITIONS[currentGetHeldObjectAnimation];
        AssignClip(AGENT_INTERACTION_ACTION_ANIMATIONS[currentGetHeldObjectAnimation]);
        if(currentGetHeldObjectAnimation == 2) {
            currentAnimationFrame = ANIMATION_FRAME_TO_ENHANCE_INTERACTION_ACTION;
        }
    }

    public void HoldHeldObjectOutForPickup() {
        heldObject.transform.localPosition = HOLDING_POSITION;
        foreach(Collider c in heldObject.MyColliders)
            c.enabled = true;
        AssignClip(AGENT_INTERACTION_ACTION_ANIMATIONS[AGENT_INTERACTION_ACTION_ANIMATIONS.Length-1]);
        AnimationPlaysOnce(isLoopAnimation: true);
        simAgentActionState = SimAgentActionState.HoldingOutHeldObject;
    }


    public void AssignClip(string clipId) {
        if(simAgentActionState == SimAgentActionState.HoldingOutHeldObject) {
            simAgentActionState = SimAgentActionState.Action;
        }
        
        currentAnimationFrame = 0;
        currentClip = clipId;
    }

    public void RotateAgentToLookAtPerformer() {
        originalRotation = new Vector3(0, transform.eulerAngles.y, 0);
        transform.LookAt(mcsController.transform);
        targetRotation = new Vector3(0, transform.eulerAngles.y, 0);
        bool doRotationAnimation = false;
        float degreeChange = CalculateRotation(ref doRotationAnimation);
        rotatingToFacePerformer = doRotationAnimation;
        if(!doRotationAnimation) {
            transform.eulerAngles = targetRotation;
            if(isHoldingHeldObject && simAgentActionState == SimAgentActionState.InteractingHoldingHeldObject) { 
                PlayGetHeldObjectAnimation();
            }
            else {
                PlayNotHoldingObject();
            }
        }
        else {
            transform.eulerAngles = originalRotation;
            AssignClip(degreeChange > 0 ? TURN_RIGHT : TURN_LEFT);
            AnimationPlaysOnce(isLoopAnimation: true);
        }
    }

    void PlayNotHoldingObject() {
        this.simAgentActionState = SimAgentActionState.InteractingNotHoldingHeldObject;
        int totalNotHoldingObjectFrames = Mathf.FloorToInt(MCSSimulationAgent.ANIMATION_FRAME_RATE * clipNamesAndDurations[MCSSimulationAgent.NOT_HOLDING_OBJECT_ANIMATION]);
        AssignClip(MCSSimulationAgent.NOT_HOLDING_OBJECT_ANIMATION);
        AnimationPlaysOnce(isLoopAnimation: false);
        currentAnimationFrame = NOT_HOLDING_OBJECT_STARTING_FRAME;
        notHoldingObjectCurrentFrame = 0;
    }

    float CalculateRotation (ref bool doAnimation) {
        float target = targetRotation.y;
        float original = originalRotation.y;
        float degreeChange = 0;
        
        if(target > original + 180) {
            target -= 360;
        }
        else if(target < original - 179) {
            target += 360;
        }
        degreeChange = target - original;
        doAnimation = Mathf.Abs(degreeChange) > MIMIMUM_ROTATION_ANGLE_FOR_ANIMATION;
        return degreeChange;
    }

    private void AssignPrevious() {
        previousClip = currentClip;
        previousClipWasLoop = !resetAnimationToIdleAfterPlayingOnce;
        previousCurrentFrame = currentAnimationFrame;
        previousClipStepEnd = stepToEndAnimation;
    }

    public bool SetPrevious() {
        if((previousClipStepEnd > -1 && mcsController.step < previousClipStepEnd) || previousClipStepEnd == -1) {
            SetDefaultAnimation(usePreviousClip: previousClip != "");
            currentAnimationFrame = previousCurrentFrame;
            return true;
        }
        return false;
    }

    private void ResetDelayedActionStepTriggers() {
        delayedStepBeginAction = -1;
        delayedStepEnd = -1;
        delayedIsLoopAnimation = false;
        delayedAnimation = "";
        resetOncePickedUp = false;
    }

    public void IncrementAnimationFrame() {
        //increment the current animation frame and get total frames
        currentAnimationFrame++;
        int totalFrames = Mathf.FloorToInt(MCSSimulationAgent.ANIMATION_FRAME_RATE * clipNamesAndDurations[this.currentClip]);
        if(simAgentActionState == SimAgentActionState.Action) {
            AssignPrevious();
        }

        //if the step to begin movement occured during an interaction and its step begin was later than an action that was also triggered during the interaction, 
        //start the movement sequence when the interaction is complete
        if(delayedStepBeginMovement > -1 && delayedStepBeginMovement > delayedStepBeginAction && !IsDoingAnyInteractions() && !rotatingToFacePerformer) {
            delayedStepBeginMovement = -1;
            ResetDelayedActionStepTriggers();
            MoveSimAgent();
        }

        //otherwise if the step to begin movement occured during an interaction and the interaction is complete but another action was triggered after the step begin of movement
        //then do not start movement until the triggered action is complete
        else if (delayedStepBeginMovement > -1 && !IsDoingAnyInteractions() && !rotatingToFacePerformer) {
            delayedStepBeginMovement = -1;
        }

        //if the agent is interacting with the performer and currently not rotating to face the performer play through the not holding object animation
        if(simAgentActionState == SimAgentActionState.InteractingNotHoldingHeldObject && !rotatingToFacePerformer) {
            notHoldingObjectCurrentFrame++;
            if(notHoldingObjectCurrentFrame >= NOT_HOLDING_OBJECT_ANIMATION_LENGTH) {
                //if there was an action before make sure to finish it
                if (SetPrevious())
                    return;
                //if on a movement cycle before the interaction return to it
                if(previouslyWasMoving)
                    MoveSimAgent(interactionComplete: true);
                //otherwise reset to the default idle
                else
                    SetDefaultAnimation();
            }
        }


        //if there is a delayed start because an action's step beging was called while the agent was interacting with performer and now the performer is done interacting
        if (delayedStepBeginAction > -1 && !IsDoingAnyInteractions()) {
            //if the step end of that delayed action is past the current mcs step then we dont play it and reset to idle
            if(mcsController.step >= delayedStepEnd && delayedStepEnd != -1 && (!actionsStepBegins.Contains(mcsController.step))) {
                SetDefaultAnimation();
                AnimationPlaysOnce(isLoopAnimation: true);
            }
            //otherwise play the action 
            else if (!actionsStepBegins.Contains(mcsController.step)) {
                simAgentActionState = SimAgentActionState.Action;
                AssignClip(delayedAnimation);
                stepToEndAnimation = delayedStepEnd;
                AnimationPlaysOnce(isLoopAnimation: delayedIsLoopAnimation);
                AssignPrevious();
            }
            //then reset these to their defaults to prepare for the next occurence of delayed action calls
            ResetDelayedActionStepTriggers();
        }

        //reset to idle after the agent was holding out its object and the performer picked it up
        else if (resetOncePickedUp && (simAgentActionState != SimAgentActionState.InteractingHoldingHeldObject && simAgentActionState != SimAgentActionState.HoldingOutHeldObject)) {
            if(!actionsStepBegins.Contains(mcsController.step))
                SetDefaultAnimation();
            resetOncePickedUp = false;
        }
        //if the animation currently playing is not supposed to loop, and the current frame is greater than the total frames, and the agent is not interacting with a held object
        //then set the agent animation to the default
        else if (resetAnimationToIdleAfterPlayingOnce && currentAnimationFrame > totalFrames && 
                (simAgentActionState != SimAgentActionState.InteractingHoldingHeldObject || simAgentActionState != SimAgentActionState.HoldingOutHeldObject)) {
            SetDefaultAnimation(usePreviousClip: simAgentActionState == SimAgentActionState.InteractingNotHoldingHeldObject);
        }
        //if the the step to reset to idle occurs while the agent is interacting while holding an object signify that we need to reset to the default after the object is picked up
        else if(mcsController.step == stepToEndAnimation && (simAgentActionState == SimAgentActionState.InteractingHoldingHeldObject || simAgentActionState == SimAgentActionState.HoldingOutHeldObject)) {
            resetOncePickedUp = true;
        }
        //if step to end animation is the current step we are not interacting then reset or move the sim agent 
        else if(mcsController.step == stepToEndAnimation && simAgentActionState != SimAgentActionState.InteractingNotHoldingHeldObject) {
            stepToEndAnimation = -1;
            SetDefaultAnimation();
            if(previouslyWasMoving)
                MoveSimAgent();
        }
        //if step to end animation and interacting with the performer but not holding anything then increment the end step so it
        //ends when the not holding object interaction is complete
        else if(mcsController.step == stepToEndAnimation && simAgentActionState == SimAgentActionState.InteractingNotHoldingHeldObject) {
            stepToEndAnimation += Mathf.FloorToInt(MCSSimulationAgent.ANIMATION_FRAME_RATE * clipNamesAndDurations[MCSSimulationAgent.NOT_HOLDING_OBJECT_ANIMATION]) - currentAnimationFrame + 1;
        }

        //play a single frame of the animation
        currentAnimationFrame = currentAnimationFrame > totalFrames ? 0 : currentAnimationFrame;
        float percentOfAnimation = currentAnimationFrame / (float)(totalFrames);
        animator.Play(currentClip, 0, percentOfAnimation);

        //if the agent is playing its get held object animation and completed its rotation toward the agent
        if(simAgentActionState == SimAgentActionState.InteractingHoldingHeldObject && !rotatingToFacePerformer) {
            currentGetHeldObjectAnimation++;
            //hold out the held object for pickup once the animation is complete
            if(currentGetHeldObjectAnimation >= AGENT_INTERACTION_ACTION_ANIMATIONS.Length) {
                HoldHeldObjectOutForPickup();
            }
            //otherwise keep playing the animation
            else {
                PlayGetHeldObjectAnimation();
            }
        }

        //if the agent is currently interacting with performer and completed its rotation animation then always look at the agent
        if (IsDoingAnyInteractions() && !rotatingToFacePerformer) {
            transform.LookAt(mcsController.transform);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }

        //handles rotations
        if(rotatingToFacePerformer) {
            float speedRatio = 500f; //this is so shorter rotations are quicker and longer rotations are slower to sync with the animation
            float speedMultiplier = Mathf.Clamp(speedRatio / Mathf.Abs(targetRotation.y - originalRotation.y), 5, 10);
            rotationPercent = ((float) currentAnimationFrame / (float) MCSSimulationAgent.ANIMATION_FRAME_RATE) * speedMultiplier;
            transform.rotation = Quaternion.Slerp(Quaternion.Euler(originalRotation), Quaternion.Euler(targetRotation), rotationPercent);
            float snapToLookAtPerformerPercent = 0.75f;
            if(rotationPercent >= snapToLookAtPerformerPercent) { //snap to face the performer after the rotation is 75 percent done
                rotatingToFacePerformer = false;
                if(this.isHoldingHeldObject) {
                    PlayGetHeldObjectAnimation();
                }
                else {
                    PlayNotHoldingObject();
                }
            }
        }

        //if the agent is doing a rotation for its movement path and not interacting with the agent and not rotating to face the agent
        //then rotate to the movement path direction
        if(simAgentActionState == SimAgentActionState.Rotating && !IsDoingAnyInteractions() && !rotatingToFacePerformer) {
            float speedRatio = 500f; //this is so shorter rotations are quicker and longer rotations are slower to sync with the animation
            float speedMultiplier = Mathf.Clamp(speedRatio / Mathf.Abs(targetRotation.y - originalRotation.y), 5, 10);
            rotationPercent = ((float) currentAnimationFrame / (float) MCSSimulationAgent.ANIMATION_FRAME_RATE) * speedMultiplier;
            transform.rotation = Quaternion.Slerp(Quaternion.Euler(originalRotation), Quaternion.Euler(targetRotation), rotationPercent);
            float snapToLookAtTargetDirectionPercent = 0.85f;
            if(rotationPercent >= snapToLookAtTargetDirectionPercent) { //when done rotating move the agent
                transform.eulerAngles = targetRotation;
                MoveSimAgent(true);
            }
        }

        //otherwise if there is a movement path and the agent is not interacting or rotating torward the performer or doing an action
        //move the agent toward its target direction
        else if(agentMovement != null && !IsDoingAnyInteractions() && !rotatingToFacePerformer && simAgentActionState != SimAgentActionState.Action)
            MoveSimAgent();

        //if there is a movement path and the step to begin for movement is the current step and the agent is not interacting then start the movement sequence
        if(agentMovement != null && mcsController.step == agentMovement.stepBegin && !rotatingToFacePerformer && !IsDoingAnyInteractions()) {
            MoveSimAgent();
            previousClipStepEnd = -2; //this makes sure that the agent moves and does not think an action needs to be played
            return;
        }
        //if there is a movement path and the step to begin for movement is the current step and the agent is interacting then delay the movement till when the action is complete
        if(agentMovement != null && mcsController.step == agentMovement.stepBegin && (rotatingToFacePerformer || IsDoingAnyInteractions())) {
            delayedStepBeginMovement = agentMovement.stepBegin;
            return;
        }
    }

    public bool IsDoingAnyInteractions() {
        return (simAgentActionState == SimAgentActionState.InteractingNotHoldingHeldObject || 
            simAgentActionState == SimAgentActionState.InteractingHoldingHeldObject || 
            simAgentActionState == SimAgentActionState.HoldingOutHeldObject);
    }


    public bool MoveSimAgent(bool moveFromRotatingAnimation = false, bool interactionComplete = false) {
        if(doneWithMovementSequence) 
            return true;

        if(obstructed) {
            if(!obstructedAnimationSet) {
                SetDefaultAnimation();
                AnimationPlaysOnce(isLoopAnimation: true);
                obstructedAnimationSet = true;
            }
            return false;
        }
        //if there is a movment config assinged to this agent then move it
        if(agentMovement != null && agentMovement.sequence != null && agentMovement.sequence.Count > 0 && mcsController.step >= agentMovement.stepBegin) {
            //if the movement sequence is complete and it should not repeat then set the agent to its default animation
            if(moveIndex >= agentMovement.sequence.Count && !agentMovement.repeat) {
                SetDefaultAnimation();
                AnimationPlaysOnce(isLoopAnimation: true);
                return true;
            }

            //if doing a rotation to look at its new direction, dont move
            if(simAgentActionState == SimAgentActionState.Rotating && !moveFromRotatingAnimation) {
                previouslyWasMoving = true;
                return true;
            }

            //if any interaction are not complete dont move
            if(IsDoingAnyInteractions() && !interactionComplete) {
                return false;
            }
            
            //if any interactions are complete then rotate the agent back to its previous direction
            if(IsDoingAnyInteractions() && interactionComplete) {
                if(actionsStepBegins.Contains(mcsController.step)) {
                    return false;
                }
                previouslyWasMoving = true;
                SetMovementRotation();
                return true;
            }

            //if not currently moving but needs to move, then move the agent
            if (simAgentActionState != SimAgentActionState.Moving && !interactionComplete) {
                previouslyWasMoving = true;
                //if not looking the way we need to then set the movement rotation instead
                if(SetMovementRotation())
                    return true;
                AssignClip(agentMovement.sequence[moveIndex].animation);
                AnimationPlaysOnce(isLoopAnimation: true);
                simAgentActionState = SimAgentActionState.Moving;
                return true;
            }

            //calculate movement direction
            float x = agentMovement.sequence[moveIndex].endPoint.x;
            float z = agentMovement.sequence[moveIndex].endPoint.z;
            targetPos = new Vector3(x, transform.position.y, z);
            direction = (targetPos-transform.position).normalized;

            //if direction magnitude greater than zero then move
            if(direction.sqrMagnitude > float.Epsilon) {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.Translate(direction * MOVE_MAGNITUDE, Space.World);
                previouslyWasMoving = true;
            }

            //if close enough then snap to position
            if(Mathf.Abs(transform.position.x - x) <= MOVE_MAGNITUDE && Mathf.Abs(transform.position.z - z) <= MOVE_MAGNITUDE) {
                transform.position = targetPos;
                moveIndex++;

                //dont repeat is the config says so
                if((simAgentActionState != SimAgentActionState.Idle || simAgentActionState != SimAgentActionState.Action) && moveIndex >= agentMovement.sequence.Count && !agentMovement.repeat) {
                    doneWithMovementSequence = true;
                    SetDefaultAnimation();
                    AnimationPlaysOnce(isLoopAnimation: true);
                    previouslyWasMoving = false;
                    return true;
                }

                //if at the end of the movement loop and need to repeat then reset
                if(moveIndex >= agentMovement.sequence.Count && agentMovement.repeat) {
                    previouslyWasMoving = true;
                    moveIndex = 0;
                    SetMovementRotation();
                    return true;
                }

                //if the next endpoint exists then rotate to next point
                if(agentMovement.sequence[moveIndex].endPoint.x != Mathf.NegativeInfinity) {
                    previouslyWasMoving = true;
                    SetMovementRotation();
                    return true;
                }
            } 
        }
        return false;
    }

    private bool SetMovementRotation() {
        float x = agentMovement.sequence[moveIndex].endPoint.x;
        float z = agentMovement.sequence[moveIndex].endPoint.z;
        simAgentActionState = SimAgentActionState.Rotating;

        x = agentMovement.sequence[moveIndex].endPoint.x;
        z = agentMovement.sequence[moveIndex].endPoint.z;

        targetPos = new Vector3(x, transform.position.y, z);
        direction = (targetPos-transform.position).normalized;

        originalRotation = transform.eulerAngles;
        if(direction.sqrMagnitude > float.Epsilon) {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
        targetRotation = transform.eulerAngles;
        transform.eulerAngles = originalRotation;
        if(Mathf.Approximately(transform.eulerAngles.y, targetRotation.y)) {
            transform.eulerAngles = targetRotation;
            return false;
        }
        float degreeChange = CalculateRotationForMovement();
        AssignClip(degreeChange > 0 ? MOVEMENT_TURNS_RIGHT : MOVEMENT_TURNS_LEFT);
        AnimationPlaysOnce(isLoopAnimation: false);
        return true;
    }

    private float CalculateRotationForMovement () {
        float target = targetRotation.y;
        float original = originalRotation.y;
        float degreeChange = 0;
        
        if(target > original + 180) {
            target -= 360;
        }
        else if(target < original - 179) {
            target += 360;
        }
        degreeChange = target - original;
        return degreeChange;
    }

    public void SetMovement(MCSConfigSimAgentMovement configSimAgentMovement) {
        this.agentMovement = configSimAgentMovement;
    }

    public void AnimationPlaysOnce(bool isLoopAnimation) {
        resetAnimationToIdleAfterPlayingOnce = !isLoopAnimation;
    }

    public void SetStepToEndAnimation(int step) {
        this.stepToEndAnimation = step;
    }

    public void SetBeard(int? beardIndex = -1) {
        if (this.beard == null || this.beard.gameObject == null || this.beard.materials == null) {
            return;
        }
        // Choose a random beard material now to ensure each part of the beard is the same.
        if (beardIndex == null || beardIndex >= this.beard.materials.Length || beardIndex < 0) {
            beardIndex = ChooseDefaultIndex(this.beard.materials.Length);
        }
        // The beard has four separate material elements to set in its renderer's materials array.
        for (int i = 0; i <= 3; ++i) {
            this.SetMaterialFromList(this.beard, this.beard.materials, (int)beardIndex, i);
        }
        this.beard.gameObject.SetActive(true);
    }

    public void SetChest(int? chestIndex = -1, int? chestMaterialIndex = -1) {
        if (chestIndex == null || chestIndex >= this.chestOptions.Length || chestIndex < 0) {
            chestIndex = ChooseDefaultIndex(this.chestOptions.Length);
        }
        this.DeactivateGameObjects(this.chestOptions);
        this.chest = this.chestOptions[(int)chestIndex];
        this.chest.gameObject.SetActive(true);
        this.SetMaterial(this.chest, chestMaterialIndex, this.chest.skinRendererMaterialIndex == 0 ? 1 : 0);
        // Update other game objects based on the active chest model.
        if (this.legs != null && this.legs.gameObject != null) {
            this.legs.gameObject.SetActive(!this.chest.deactivateLegs);
        }
        if (this.tie != null && this.tie.gameObject != null) {
            this.tie.gameObject.SetActive(this.chest.enableTies);
        }
    }

    public void SetElder(bool elder) {
        this.elder = elder;
        SkinnedMeshRenderer skinRenderer = this.head.gameObject.GetComponent<SkinnedMeshRenderer>();
        skinRenderer.SetBlendShapeWeight(MCSSimulationAgent.ELDER_BLEND_SHAPE, elder ? 100 : 0);
        // Ensure elders have elder skin, and visa-versa.
        this.SetSkin(this.skin);
    }

    public void SetEyes(int? eyesIndex = -1) {
        // The eyes are a material on the head game object.
        this.SetMaterial(this.head, eyesIndex, this.head.eyesRendererMaterialIndex);
    }

    public void SetFeet(int? feetIndex = -1, int? feetMaterialIndex = -1) {
        if (feetIndex == null || feetIndex >= this.feetOptions.Length || feetIndex < 0) {
            feetIndex = ChooseDefaultIndex(this.feetOptions.Length);
        }
        this.DeactivateGameObjects(this.feetOptions);
        this.feet = this.feetOptions[(int)feetIndex];
        this.feet.gameObject.SetActive(true);
        this.SetMaterial(this.feet, feetMaterialIndex, this.feet.skinRendererMaterialIndex == 0 ? 1 : 0);
    }

    public void SetGlasses(int? glassesIndex = -1) {
        this.SetMaterial(this.glasses, glassesIndex);
        this.glasses.gameObject.SetActive(true);
    }

    public void SetHair(int? hairIndex = -1, int? hairMaterialIndex = -1, int? hatMaterialIndex = -1) {
        if (hairIndex == null || hairIndex >= this.hairOptions.Length || hairIndex < 0) {
            hairIndex = ChooseDefaultIndex(this.hairOptions.Length);
        }
        this.DeactivateGameObjects(this.hairOptions);
        this.hair = this.hairOptions[(int)hairIndex];
        this.hair.gameObject.SetActive(true);
        this.SetMaterial(this.hair, hairMaterialIndex, this.hair.hatRendererMaterialIndex == 0 ? 1 : 0);
        // If the current hair model has a hat, set the hat's material.
        if (this.hair.hatRendererMaterialIndex >= 0 && this.hair.hatMaterials != null) {
            if (hatMaterialIndex == null || hatMaterialIndex >= this.hair.hatMaterials.Length || hatMaterialIndex < 0) {
                hatMaterialIndex = ChooseDefaultIndex(this.hair.hatMaterials.Length);
            }
            this.SetMaterialFromList(this.hair, this.hair.hatMaterials, (int)hatMaterialIndex,
                    this.hair.hatRendererMaterialIndex);
        }
    }

    public void SetJacket(int? jacketIndex = -1, int? jacketMaterialIndex = -1) {
        // TODO Fix issue with jackets clipping into other clothes.
        /*
        if (jacketIndex == null || jacketIndex >= this.jacketOptions.Length || jacketIndex < 0) {
            jacketIndex = ChooseDefaultIndex(this.jacketOptions.Length);
        }
        this.DeactivateGameObjects(this.jacketOptions);
        this.jacket = this.jacketOptions[(int)jacketIndex];
        this.jacket.gameObject.SetActive(true);
        this.SetMaterial(this.jacket, jacketMaterialIndex);
        */
    }

    public void SetLegs(int? legsIndex = -1, int? legsMaterialIndex = -1) {
        if (legsIndex == null || legsIndex >= this.legsOptions.Length || legsIndex < 0) {
            legsIndex = ChooseDefaultIndex(this.legsOptions.Length);
        }
        this.DeactivateGameObjects(this.legsOptions);
        this.legs = this.legsOptions[(int)legsIndex];
        this.legs.gameObject.SetActive(true);
        this.SetMaterial(this.legs, legsMaterialIndex, this.legs.skinRendererMaterialIndex == 0 ? 1 : 0);
        // If the current chest model already has legs, then deactivate the separate legs models.
        if (this.chest != null && this.chest.deactivateLegs) {
            this.legs.gameObject.SetActive(false);
        }
    }

    public void SetSkin(int? skinIndex = -1) {
        if (skinIndex == null || skinIndex >= this.skinOptions.Length || skinIndex < 0) {
            skinIndex = ChooseDefaultIndex(this.skinOptions.Length);
        }
        // There are 4 elder skins corresponding to the 4 male or 12 female skins.
        this.skin = this.elder ? ((int)skinIndex % 4) : (int)skinIndex;
        Material[] skinOptions = this.elder ? this.elderSkinOptions : this.skinOptions;
        if (facelessHead != null) {
            this.SetMaterial(facelessHead, skinIndex);
        }
        else {
            this.SetSkinMaterial(new SkinObjectMaterialOption[]{this.head}, skinOptions, this.skin);
        }
        this.SetSkinMaterial(this.chestOptions, skinOptions, this.skin);
        this.SetSkinMaterial(this.feetOptions, skinOptions, this.skin);
        this.SetSkinMaterial(this.legsOptions, skinOptions, this.skin);
    }

    public void SetTie(int? tieIndex = -1, int? tieMaterialIndex = -1) {
        if (tieIndex == null || tieIndex >= this.tieOptions.Length || tieIndex < 0) {
            tieIndex = ChooseDefaultIndex(this.tieOptions.Length);
        }
        this.DeactivateGameObjects(this.tieOptions);
        this.tie = this.tieOptions[(int)tieIndex];
        this.tie.gameObject.SetActive(true);
        this.SetMaterial(this.tie, tieMaterialIndex);
        // If the current chest model is not compatible with ties, then deactivate the tie.
        if (this.chest != null && !this.chest.enableTies) {
            this.tie.gameObject.SetActive(false);
        }
    }

    private void GetFacelessHead() {
        foreach (Transform child in GetComponentsInChildren<Transform>()){
            if (child.name == "FacelessHead"){
                facelessHead.gameObject = child.gameObject;
                facelessHead.materials = skinOptions;
                facelessHead.renderer = facelessHead.gameObject.GetComponent<Renderer>();
                break;
            }
        }
    }

    private int ChooseDefaultIndex(int listSize) {
        // Here in case we want to change this behavior in the future.
        return 0;
    }

    private void DeactivateGameObjects(ObjectMaterialOption[] options) {
        foreach (ObjectMaterialOption option in options) {
            option.gameObject.SetActive(false);
        }
    }

    private void SetMaterial(ObjectMaterialOption option, int? materialOptionIndex, int rendererMaterialIndex = 0) {
        if (materialOptionIndex == null || materialOptionIndex >= option.materials.Length || materialOptionIndex < 0) {
            materialOptionIndex = ChooseDefaultIndex(option.materials.Length);
        }
        this.SetMaterialFromList(option, option.materials, (int)materialOptionIndex, rendererMaterialIndex);
    }

    private void SetMaterialFromList(
        ObjectMaterialOption option,
        Material[] materialOptions,
        int materialOptionIndex,
        int rendererMaterialIndex = 0
    ) {
        // If we haven't cached the game object's renderer yet, do it now.
        if (option.renderer == null) {
            option.renderer = option.gameObject.GetComponent<Renderer>();
        }
        // Copy the renderer's existing materials array.
        Material[] materials = new Material[option.renderer.materials.Length];
        option.renderer.materials.CopyTo(materials, 0);
        // Change the specific material in the copied array.
        materials[rendererMaterialIndex] = materialOptions[materialOptionIndex];
        // Must completely reassign the renderer's materials array here (see the Unity docs).
        option.renderer.materials = materials;
    }

    private void SetSkinMaterial(ObjectMaterialOption[] options, Material[] skinOptions, int skinMaterialIndex) {
        foreach (SkinObjectMaterialOption option in options) {
            // If this body part shows skin...
            if (option.skinRendererMaterialIndex >= 0) {
                // Set the skin material for the body part using the chosen index.
                this.SetMaterialFromList(option, skinOptions, skinMaterialIndex, option.skinRendererMaterialIndex);
            }
        }
    }
}

[System.Serializable]
public class ObjectMaterialOption {
    public GameObject gameObject;
    public Material[] materials;
    [HideInInspector]
    public Renderer renderer;
}

[System.Serializable]
public class HairObjectMaterialOption : ObjectMaterialOption {
    // Some hair options may show hats as an additional material.
    public Material[] hatMaterials;
    public int hatRendererMaterialIndex = -1;
}

[System.Serializable]
public class SkinObjectMaterialOption : ObjectMaterialOption {
    // Some clothing options may show skin as an additional material.
    public int skinRendererMaterialIndex = -1;
}

[System.Serializable]
public class ChestObjectMaterialOption : SkinObjectMaterialOption {
    // Some chest options may deactivate legs.
    public bool deactivateLegs = false;
    // Some chest options may have ties (configured separately).
    public bool enableTies = false;
}

[System.Serializable]
public class HeadObjectMaterialOption : SkinObjectMaterialOption {
    public int eyesRendererMaterialIndex = 1;
}
