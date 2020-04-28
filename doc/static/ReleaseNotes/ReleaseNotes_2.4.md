# AI2-THOR Version 2.4 Release Notes

## IMPORTANT NOTICE
Note that AI2-THOR 2.4 is not fully backwards compatible with previous versions due to updates and reworked architecture in the framework. All scenes and the majority of objects have been updated. Some actions have been deprecated and replaced with new actions with more features.

## More Sim Object Types
Additional Sim Object Types have been added to the framework
- Old Total Types: **115**
- New Total Types: **126**

Types Added:

- **ShelvingUnit**
- **AluminumFoil**
- **DogBed**
- **Dumbbell**
- **TableTopDecor**
- **RoomDecor**
- **Stool**
- **GarbageBag**
- **Desktop**
- **TargetCircle**
- **Floor**

## Moveable Objects
Previously, only objects with the `Pickupable` property were able to be moved around the environment with physics based actions. `Pickupable` objects are small enough to be picked up by the agent's hand, but larger objects that would have made sense to be moved around via `PushObject` or `PullObject` actions could not move, becuase they were not classified as `Pickupable.` Now, a new property called `Moveable` has been added. All large objects that are not explicitly attached to the structure of a scene can now be moved with physics. Some examples of these new `Moveable` objects are the `Chair`, `Table`, `Sofa`, `Microwave`, or `Toaster` categories. These `Moveable` objects cannot be picked up by the agent, as they are too large, but actions that can shove `Pickupable` objects like `PushObject` and `PullObject` will now also affect `Moveable` objects.

## Upgraded Actions
Various older actions have been upgraded to allow for more functionality.

- RotateLeft/RotateRight - These actions now allow the agent to freely rotate a specified number of degree increments, where previously only increments of 90 degrees was allowed.
- LookUp/LookDown - These actions now allow the agent to look up and down a specified number of degree increments rather than only in increments of 30 degrees.
- Push/Pull/Directional Push - These actions have been updated to allow manipulating both `Pickupable` and `Moveable` objects
- Initialize - New agent modes and agent controllers have been added. There is now the choice of a bot, drone, and default agent mode as well as a physics, stochastic, or drone controller that allows different actions.

## New Actions
New actions have been introduced.

- SetObjectStates - Change the states of all objects of a given category within a scene.
- MakeAllObjectsMoveable - Enable physics for all `Pickupable` and `Moveable` objects, allowing physics interactions in the scene.
- Fly Actions - New actions to allow movement if the agent is initialized as a flying drone with the drone agent mode and the drone controller.
- PlaceObjectAtPoint - Place an individual object flush with the surface of whatever structure is below the point specified by this action. This allows moving object around the scene without needing to pick them up in the agent's hand or using `InitialRandomSpawn` to randomly move all objects.
- TouchThenApplyForce - Apply force to objects targeted by using screen-space coordinates.
- SpawnTargetCircle - Spawn a new `TargetCircle` object that allows for a spawnable target zone that can report back if an object is on top of it or not.

### New Receptacle Objects
Various object types have been updated to also have the `Receptacle` property.
- Chair
- Stool
- Footstool
- Floor

## New Metadata Values
New metadata values have been added.

- axisAlignedBoundingBox - A world axis oriented box that encompesses the entirety of an object. All Sim Objects have an axis aligned box. Note that this box may change in volume if the object is rotated, as the dimensions are aligned to the world axes, not the object's local axes.
- objectOrientedBoundingBox - An object oriented bounding box that encompesses the entirety of an object. Only `Pickupable` objects have this box, as it is aligned to always be the same dimensions regardless of the rotation of the object.
- numStructureHits - A new metadata value included that works only with the drone agent and drone object launcher. Reports the number of structures an object launched has hit.
- numFloorHits - A new metadata value included that works only with the drone agent and drone object launcher. Reports the number of times an object launched has hit the floor object.
- numSimObjHits - A new metadata value included that works only with the drone agent and drone object launcher. Reports the number of sim objects an object launched has hit.
- isCaught - A new metadata value included that works only with the drone agent and drone object launcher. Reports if the launched object has been caught by the drone's box

## Updated Documentation
[Documentation on the AI2-THOR website](https://ai2thor.allenai.org/ithor/documentation/) has been expanded to detail all functionality of this update.


