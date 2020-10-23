using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityStandardAssets.Characters.FirstPerson;

public class CollisionListener : MonoBehaviour
{
    //references to the joints of the mid level arm
    [SerializeField]
    private bool CascadeCollisionEventsToParent = true;
    //track what was hit while arm was moving
    public class StaticCollision
    {
        public GameObject gameObject;

        public SimObjPhysics simObjPhysics;

         //indicates if gameObject a simObject
        public bool isSimObj {
            get { return simObjPhysics != null; }
        }
    }
    private StaticCollision staticCollided;

    private HashSet<Collider> activeColliders = new HashSet<Collider>();

    public void RegisterCollision(Collider col, bool notifyParent = true) {
        activeColliders.Add(col);
        if (notifyParent && this.transform.parent != null) {
            foreach (var listener in this.transform.parent.GetComponentsInParent<CollisionListener>()) {
                listener.RegisterCollision(col, notifyParent);
            }
        }     
    }

    public void DeregisterCollision(Collider col, bool notifyParent = true) {
        activeColliders.Remove(col);
        if (notifyParent && this.transform.parent != null) {
            foreach (var listener in this.transform.parent.GetComponentsInParent<CollisionListener>()) {
                listener.DeregisterCollision(col, notifyParent);
            }
        } 
    }

    public void Reset(bool? notifyParent = null) {
        activeColliders.Clear();
        if (notifyParent.GetValueOrDefault(this.CascadeCollisionEventsToParent) && this.transform.parent != null) {
            foreach (var listener in this.transform.parent.GetComponentsInParent<CollisionListener>()) {
                listener.Reset(notifyParent);
            }
        } 
    }

    public void OnTriggerExit(Collider col)
    {
        DeregisterCollision(col, CascadeCollisionEventsToParent);
    }

    public void OnTriggerStay(Collider col)
    {
        this.
        RegisterCollision(col, CascadeCollisionEventsToParent);
    }

    public void OnTriggerEnter(Collider col)
    {
        RegisterCollision(col, CascadeCollisionEventsToParent);
    }

    private StaticCollision ColliderToStaticCollision(Collider col) {
        StaticCollision sc = null;
        if(col.GetComponentInParent<SimObjPhysics>())
            {
                //how does this handle nested sim objects? maybe it's fine?
                SimObjPhysics sop = col.GetComponentInParent<SimObjPhysics>();
                if(sop.PrimaryProperty == SimObjPrimaryProperty.Static)
                {

                    if(!col.isTrigger)
                    {
                        // #if UNITY_EDITOR
                        // Debug.Log("Collided with static sim obj " + sop.name);
                        // #endif
                        sc = new StaticCollision();
                        sc.simObjPhysics = sop;
                        sc.gameObject = col.gameObject;
                    }
                }
            }

            //also check if the collider hit was a structure?
            else if(col.gameObject.tag == "Structure")
            {                
                if(!col.isTrigger)
                {
                    sc = new StaticCollision();
                    sc.gameObject = col.gameObject;
                }
            }
        return sc;
    }

   public List<StaticCollision> StaticCollisions() {
        var staticCols = new List<StaticCollision>();
        foreach(var col in activeColliders) {

            var staticCollision = ColliderToStaticCollision(col);
            if (staticCollision != null) {
                staticCols.Add(staticCollision);
            }
        }
        return staticCols;
    }

    public bool ShouldHalt() {
        return StaticCollisions().Count > 0;
    }

}
