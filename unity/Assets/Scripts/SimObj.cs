// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(Rigidbody))]
public class SimObj : MonoBehaviour, SimpleSimObj
{

	public string ObjectID 
	{
		get 
		{
			return objectID;
		} 

		set 
		{
			//TODO add an ID lock
			objectID = value;
		}
	}

	public bool IsVisible 
	{
		get 
		{
			return isVisible;
		} 

		set {
			isVisible = value;
		}
	}


	public SimObjType Type = SimObjType.Undefined;
	public SimObjManipType Manipulation = SimObjManipType.Inventory;
	public static SimObjType[] OpenableTypes = new SimObjType[] { SimObjType.Fridge, SimObjType.Cabinet, SimObjType.Microwave, SimObjType.LightSwitch, SimObjType.Blinds, SimObjType.Book, SimObjType.Toilet };
	public static SimObjType[] ImmobileTypes = new SimObjType[] { SimObjType.Chair, SimObjType.Toaster, SimObjType.CoffeeMachine, SimObjType.Television, SimObjType.StoveKnob };
	private static Dictionary<SimObjType, Dictionary<string, int>> OPEN_CLOSE_STATES = new Dictionary<SimObjType, Dictionary<string, int>>{
		{SimObjType.Microwave, new Dictionary<string, int>{{"open", 2}, {"close", 1}}},
		{SimObjType.Laptop, new Dictionary<string, int>{{"open", 2}, {"close", 1}}},
		{SimObjType.Book, new Dictionary<string, int>{{"open", 1}, {"close", 2}}},
		{SimObjType.Toilet, new Dictionary<string, int>{{"open", 2}, {"close", 3}}},
		{SimObjType.Sink, new Dictionary<string, int>{{"open", 2}, {"close", 1}}}
	};
	public bool UseCustomBounds = false;
	public bool isVisible = false;
	public bool UseWidthSearch = false;
	public bool hasCollision = false;
	public Transform BoundsTransform;
	//stores the location of the simObj on startup

	public SimObjType ObjType  {

		get {
			return Type;

		}
	}

	public List<string> ReceptacleObjectIds {

		get {
			List<string> objectIds = new List<string>();
			foreach (SimObj o in SimUtil.GetItemsFromReceptacle(this.Receptacle))
			{
				objectIds.Add(o.objectID);
			}
			return objectIds;
		}
	}

	public Transform StartupTransform 
    {
		get 
        {
			return startupTransform;
		}
	}

	public Animator Animator 
    {
		get 
        {
			return animator;
		}
	}
	public Receptacle Receptacle 
    {
		get 
        {
			return receptacle;
		}
	}
	public Rearrangeable Rearrangeable 
    {
		get 
        {
			return rearrangeable;
		}
	}
	public bool IsReceptacle 
    {
		get 
        {
			return receptacle != null;
		}
	}
	public bool IsAnimated 
    {
		get 
        {
			return animator != null;
		}
	}
	public bool IsAnimating 
    {
		get 
        {
			return isAnimating;
		}
		set 
        {
			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty (this);
			#endif
			isAnimating = value;
		}
	}

	private bool updateAnimState(Animator anim, int value)
	{
		AnimatorControllerParameter param = anim.parameters[0];

		if (anim.GetInteger(param.name) == value)
		{
			return false;
		}
		else
		{
			anim.SetInteger(param.name, value);
			return true;
		}
	}

	private bool updateAnimState(Animator anim, bool value)
	{
		AnimatorControllerParameter param = anim.parameters[0];

		if (anim.GetBool(param.name) == value)
		{
			return false;
		}
		else
		{
			anim.SetBool(param.name, value);
			return true;
		}
	}

	public bool Open() {
		bool res = false;
		if (OPEN_CLOSE_STATES.ContainsKey(this.Type))
		{
			res = updateAnimState(this.Animator, OPEN_CLOSE_STATES[this.Type]["open"]);

		}
		else if (this.IsAnimated)
		{
			res = updateAnimState(this.Animator, true);
		}

		return res;
	}

	public bool Close() {
		bool res = false;
		if (OPEN_CLOSE_STATES.ContainsKey(this.Type))
		{
			res = updateAnimState(this.Animator, OPEN_CLOSE_STATES[this.Type]["close"]);
		}
		else if (this.IsAnimated)
		{
			res = updateAnimState(this.Animator, false);
		}

		return res;
	}

	public bool VisibleToRaycasts 
    {
		get 
        {
			return visibleToRaycasts;
		}

        set 
        {
			if (colliders == null) 
            {
				#if UNITY_EDITOR
				Debug.LogWarning ("Warning: Tried to set colliders enabled before item was initialized in " + name);
				#endif
				visibleToRaycasts = value;
				return;
			}

			if (visibleToRaycasts != value) 
            {
				visibleToRaycasts = value;
				gameObject.layer = (visibleToRaycasts ? SimUtil.RaycastVisibleLayer : SimUtil.RaycastHiddenLayer);
				for (int i = 0; i < colliders.Length; i++) {
					colliders [i].gameObject.layer = (visibleToRaycasts ? SimUtil.RaycastVisibleLayer : SimUtil.RaycastHiddenLayer);
				}
			}
		}
	}
    public Vector3 CenterPoint 
    {
        get 
        {
            return centerPoint;
        }
    }

	public Vector3 TopPoint {
		get 
        {
			return topPoint;
		}
	}

	public Vector3 BottomPoint {
		get 
        {
			return bottomPoint;
		}
	}

	public Bounds Bounds {
		get {
			return bounds;
		}
	}

	public bool VisibleNow = false;

	#if UNITY_EDITOR
	//used for debugging object visibility
    public string Error 
    {
		get 
        {
			return error;
		}
	}
    string error = string.Empty;
	#endif

	private string objectID = string.Empty;
	private Receptacle receptacle;
	private Animator animator;
	private Rearrangeable rearrangeable;
	private Collider[] colliders = null;
	private bool visibleToRaycasts = true;
    private Vector3 centerPoint;
	private Vector3 startupScale;
	private Vector3 topPoint;
	private Vector3 bottomPoint;
	private Vector3 lastPosition;
	private bool startedInReceptacle;
	private bool isAnimating = false;
	private Transform startupTransform;
	private Bounds bounds;


    //this guy right here caused the giant groceries... should only be an issue with pivots
	public void ResetScale() 
    {
		Transform tempParent = transform.parent;
		transform.parent = null;
		transform.localScale = startupScale;
		transform.parent = tempParent;
	}

	public bool IsPickupable {
		get {
			return !this.IsOpenable && !this.IsReceptacle && !(Array.IndexOf(ImmobileTypes, this.Type) >= 0);
		}

	}

	public bool IsOpen {
		get {
			Animator anim = this.Animator;
			AnimatorControllerParameter param = anim.parameters[0];
			if (OPEN_CLOSE_STATES.ContainsKey(this.Type))
			{
				return anim.GetInteger(param.name) == OPEN_CLOSE_STATES[this.Type]["open"];
			}
			else
			{
				return anim.GetBool(param.name);
			}
		}
	}

	public bool IsOpenable {
		get {
			return Array.IndexOf(OpenableTypes, this.Type) >= 0 && this.IsAnimated;
		}

	}

    public void RecalculatePoints () 
    {

        //get first renderer in object, use that object's bounds to get center point
		Renderer r = null;
		if (!IsReceptacle) 
        {
			r = gameObject.GetComponentInChildren<MeshRenderer> ();
		}

		if (r != null) 
        {
			centerPoint = r.bounds.center;
			if (UseWidthSearch) 
            {
				topPoint = centerPoint + (Vector3.left * r.bounds.extents.x) + (Vector3.forward * r.bounds.extents.z);
				bottomPoint = centerPoint + (Vector3.right * r.bounds.extents.x) + (Vector3.back * r.bounds.extents.z);
			} 

            else 
            {
				topPoint = centerPoint + (Vector3.up * r.bounds.extents.y);
				bottomPoint = centerPoint + (Vector3.down * r.bounds.extents.y);
			}
			bounds = r.bounds;
		} 

        else 
        {
			//get the first collider
			Collider c = null;
			if (IsReceptacle) 
            {
				c = receptacle.VisibilityCollider;
			} 

            else 
            {
				c = gameObject.GetComponentInChildren<Collider> ();
			}

			if (c != null) 
            {
				centerPoint = c.bounds.center;
				if (UseWidthSearch) 
                {
					topPoint = centerPoint + (Vector3.left * c.bounds.extents.x) + (Vector3.forward * c.bounds.extents.z);
					bottomPoint = centerPoint + (Vector3.right * c.bounds.extents.x) + (Vector3.back * c.bounds.extents.z);
				} 

                else 
                {
					topPoint = centerPoint + (Vector3.up * c.bounds.extents.y);
					bottomPoint = centerPoint + (Vector3.down * c.bounds.extents.y);
				}
				bounds = c.bounds;
			}

            else 
            {
				Debug.Log ("Couldn't calculate center point in " + gameObject.name);
			}
		}

    }

	void OnCollisionEnter (Collision col)	
    {
		this.hasCollision = true;
	}
	// we do this to handle the case when an object is moved into by navigation into an object; since we reset the hasCollision flag to false prior
	// to the moveHand we check if we are leaving a collider and consider that to be a collision as well
	void OnCollisionExit (Collision col)	
    {
		this.hasCollision = true;
	}

	protected virtual void OnEnable () 
    {
		if (SceneManager.Current == null)
			return;

		//reset this in case one of our scripts was interrupted
		isAnimating = false;

		//store this beacause we'll be parenting / unparenting objects rapidly
		//and the floating point math can get wonky real quick
		startupScale = transform.lossyScale;
		
		//the receptacle script is guaranteed to run before sim obj
		//so it's safe to get our colliders here - we won't accidentally
		//grab colliders of nested objects
		receptacle = gameObject.GetComponent <Receptacle> ();
		animator = gameObject.GetComponent<Animator> ();
		rearrangeable = gameObject.GetComponent<Rearrangeable> ();
		colliders = gameObject.GetComponentsInChildren<Collider> ();

		//if the manip type isn't inventory, use the presence of the rearrangeable component to determine type
		switch (Manipulation) 
        {
		case SimObjManipType.Inventory:
			break;

		case SimObjManipType.Static:
		case SimObjManipType.StaticNoPlacement:
			Manipulation = (rearrangeable != null) ? SimObjManipType.Rearrangeable : Manipulation;
			break;
		}

		#if UNITY_EDITOR
		if (Type == SimObjType.Undefined) 
        {
			//check our prefab just in case the enum has gotten disconnected
			GameObject prefabParent = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject) as GameObject;
			if (prefabParent != null) 
            {
				SimObj ps = prefabParent.GetComponent<SimObj> ();
				if (ps != null) 
                {
					Type = ps.Type;
				}
			}
		}

		if (!Application.isPlaying) 
        {
			foreach (Collider c in colliders) 
            {
				c.gameObject.layer = SimUtil.RaycastVisibleLayer;
			}

			//if we're type static, set our renderers to static so navmeshes generate correctly
			MeshRenderer [] renderers = gameObject.GetComponentsInChildren <MeshRenderer> ();
			foreach (MeshRenderer mr in renderers) 
            {
				switch (Manipulation) 
                {
				case SimObjManipType.Static:
					mr.gameObject.isStatic = true;
					UnityEditor.GameObjectUtility.SetNavMeshArea (mr.gameObject, PlacementManager.NavmeshShelfArea);
					UnityEditor.GameObjectUtility.SetStaticEditorFlags (mr.gameObject, UnityEditor.StaticEditorFlags.NavigationStatic | UnityEditor.StaticEditorFlags.BatchingStatic);
					break;

				case SimObjManipType.StaticNoPlacement:
					mr.gameObject.isStatic = true;
					UnityEditor.GameObjectUtility.SetNavMeshArea (mr.gameObject, PlacementManager.NavemeshNoneArea);
					UnityEditor.GameObjectUtility.SetStaticEditorFlags (mr.gameObject, UnityEditor.StaticEditorFlags.NavigationStatic | UnityEditor.StaticEditorFlags.BatchingStatic);
					break;

				default:
					mr.gameObject.isStatic = false;
					UnityEditor.GameObjectUtility.SetNavMeshArea (mr.gameObject, PlacementManager.NavemeshNoneArea);
					UnityEditor.GameObjectUtility.SetStaticEditorFlags (mr.gameObject, 0);
					break;
				}
			}
		}
		#endif

		Rigidbody rb = GetComponent<Rigidbody> ();
		if (rb == null) 
        {
			rb = gameObject.AddComponent<Rigidbody> ();
		}

		if (SceneManager.Current.LocalPhysicsMode == ScenePhysicsMode.Dynamic) 
        {
			switch (Manipulation) 
            {
			case SimObjManipType.Static:
			case SimObjManipType.StaticNoPlacement:
				rb.isKinematic = true;
				break;

			default:
				rb.isKinematic = false;
				break;
			}
		} 

        else 
        {
			rb.isKinematic = true;
		}

        RecalculatePoints();

		if (Application.isPlaying) 
        {
			if (startupTransform == null) 
            {
				switch (Manipulation) {
				case SimObjManipType.Inventory:
				//if we can enter inventory
				//create a transform that stores our startup position
					startupTransform = new GameObject (name + "_Startup").transform;
					startupTransform.position = transform.position;
					startupTransform.rotation = transform.rotation;
					startupTransform.localScale = transform.localScale;
					startupTransform.parent = SceneManager.Current.ObjectsParent;
					startupTransform.SetAsLastSibling ();
					break;
				default:
					break;
				}
			}
			//make sure we're visible
			gameObject.layer = SimUtil.RaycastVisibleLayer;
			//force-update our colliders
			visibleToRaycasts = false;
			VisibleToRaycasts = true;
		}

		#if UNITY_EDITOR
		CheckForErrors ();
		#endif
	}

	#if UNITY_EDITOR
	public void RefreshColliders () 
    {
		colliders = gameObject.GetComponentsInChildren<Collider> ();
	}

	void CheckForErrors() 
    {
		error = string.Empty;
		colliders = gameObject.GetComponentsInChildren<Collider> ();
		//make sure all raycast targets are tagged correctly
		if (colliders.Length == 0) 
        {
			error = "No colliders attached!";
			return;
		}

		if (!gameObject.CompareTag (SimUtil.ReceptacleTag)) 
        {
			gameObject.tag = SimUtil.SimObjTag;
		}

		foreach (Collider c in colliders) 
        {
			//don't re-tag something that's tagged as a receptacle
			if (!c.CompareTag (SimUtil.ReceptacleTag)) 
            {
				c.gameObject.tag = SimUtil.SimObjTag;
			}
		}

		if (Type == SimObjType.Undefined) 
        {
			error = "Type is undefined!";
			return;
		}

		if (UseCustomBounds && BoundsTransform == null) {
			error = "Using custom bounds but no BoundsTransform supplied!";
		}
	}

	void Update() 
    {
		//TEMPORARY - we'll move this into receptacl
		if (transform.position != lastPosition) 
        {
			lastPosition = transform.position;
			RecalculatePoints ();
		}
	}

	void OnDrawGizmos () 
    {
		Gizmos.color = Color.white;

		if (!SimUtil.ShowObjectVisibility)
			VisibleNow = false;

		if (!string.IsNullOrEmpty (error)) 
        {
			Gizmos.color = Color.Lerp (Color.red, Color.clear, 0.5f);
			Gizmos.DrawSphere (transform.position, 0.25f);
			CheckForErrors ();
		} 

        else 
        {
			if (UseCustomBounds && SimUtil.ShowCustomBounds) 
            {
				if (BoundsTransform != null) 
                {
					//draw aligned bounding box
					Gizmos.matrix = transform.localToWorldMatrix;
					Gizmos.color = VisibleNow ? Color.yellow : Color.cyan;
					Gizmos.DrawWireCube (BoundsTransform.localPosition, BoundsTransform.localScale);
					Gizmos.color = Color.Lerp (VisibleNow ? Color.yellow : Color.cyan, Color.clear, 0.45f);
					Gizmos.DrawCube (BoundsTransform.localPosition, BoundsTransform.localScale);
					//reset matrix
					Gizmos.matrix = Matrix4x4.identity;
				}
			}

			if (UnityEditor.Selection.activeGameObject == gameObject) {
				Gizmos.color = Color.white;
				Gizmos.DrawWireSphere (CenterPoint, 0.02f);
				Gizmos.color = Color.Lerp (Color.cyan, Color.clear, 0.5f);
				Gizmos.DrawWireSphere (TopPoint, 0.02f);
				Gizmos.DrawWireSphere (BottomPoint, 0.02f);
			}

			if (VisibleNow) 
            {
				//draw an outline around our biggest renderer
				MeshFilter mf = gameObject.GetComponentInChildren <MeshFilter> (false);
				if (mf != null) 
                {
					Gizmos.color = Color.yellow;
					Gizmos.DrawWireMesh (mf.sharedMesh, -1, mf.transform.position, mf.transform.rotation, mf.transform.lossyScale);
				} 

                else 
                {
					//probably a visibility collider only sim obj
					if (IsReceptacle) {
						Gizmos.color = Color.yellow;
						//Gizmos.matrix = receptacle.VisibilityCollider.transform.worldToLocalMatrix;
						Gizmos.DrawSphere (centerPoint, 0.25f);
						Gizmos.DrawWireSphere (centerPoint, 0.25f);
					}
				}
			}
		}
	}
	#endif
}

public interface SimpleSimObj {
	SimObjType ObjType { get; }
	string ObjectID {get; set; }
	List<string> ReceptacleObjectIds {get;}
	bool IsReceptacle {get; }
	bool IsOpen {get; }
	bool IsPickupable {get; }
	bool IsOpenable {get; }
	bool Open(); 
	bool Close();
	GameObject gameObject {get; }
}