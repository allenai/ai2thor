using System;
using System.Linq;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class FpinMovableContinuous: MovableContinuous {
        public CollisionListener collisionListener {
            get;
            private set;
        }
        public FpinMovableContinuous(CollisionListener collisionListener) {
            this.collisionListener = collisionListener;
        }
        public string GetHaltMessage() {
            var staticCollisions = collisionListener?.StaticCollisions().ToList();

            if (staticCollisions.Count > 0) {
                var sc = staticCollisions[0];

                // if we hit a sim object
                if (sc.isSimObj) {
                    return "Collided with static/kinematic sim object: '" + sc.simObjPhysics.name + "', could not reach target.";
                }

                // if we hit a structural object that isn't a sim object but still has static collision
                if (!sc.isSimObj) {
                    return "Collided with static structure in scene: '" + sc.gameObject.name + "', could not reach target.";
                }
            }
            return "";
        }

        public virtual bool ShouldHalt() {
            return collisionListener.ShouldHalt();
        }

         public virtual void ContinuousUpdate(float fixedDeltaTime) {
            // Here would go the arm manipulation 
         }

        public virtual ActionFinished FinishContinuousMove(BaseFPSAgentController controller) {
            bool actionSuccess = !this.ShouldHalt();
            string errorMessage = this.GetHaltMessage();
            
            return new ActionFinished() {
                success = actionSuccess,
                errorMessage = errorMessage
            };
        }
    }