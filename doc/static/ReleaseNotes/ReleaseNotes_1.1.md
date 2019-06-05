# AI2-THOR Version 1.1 Release Notes

## IMPORTANT NOTICE
Note that AI2-THOR 1.1 is not fully backwards compatible with previous versions due to some reworked architecture in the framework. For example, some object types have been deprecated (example: ToiletPaperRoll), new object types have been introduced (example: Faucet). Several scenes have also had their layout re-arranged, so objects are not guaranteed to be found in their same position, and as such the entire room itself might be different.

New Object States and Interactions added
Old interactions: openable, pickupable, on/off, receptacle
New interactions: fillable, sliceable, cookable, breakable, dirty, used up
Fillable pic
Sliceable pic
Cookable pic
Breakable pic
Dirty 
Used up pic

New Physics and Material properties added to objects
-temperature - abstracted temperature (Cold, Hot, Room Temp) is reported by all objects
-mass - all pickup able objects have a mass value in kilograms
-salient materials - return a list of observable materials an object is composed of

Contextual Interactions that change object states
Numerous objects can contextually change states and properties of other objects or themselves. Examples:

Get pictures for all

Breakable objects will break if dropped with enough force
Dirty dishwater will become clean if moved under running water
Potatoes are cooked if moved over an active stove burner
Fillable objects are filled with water if moved under a running water source
Lit candles will be put out if placed in water


New actions that can change object states
All state changes have an accompanying Action that can be used to change the state. Note that some states can also be changed automatically via contextual interactions:
SliceObject
BreakObject
DirtyObject
CleanObject
FillObjectWithLiquid
EmptyLiquidFromObject
UseUpObject


Object State Randomization 
New actions have been added to allow random initialization of new object states
RandomToggleStateOfAllObjects
RandomToggleSpecificState

Temperature Manipulation
New actions have been added to manipulate Temperature properties
SetRoomTempDecayTimeForType
SetGlobalRoomTempDecayTime
SetDecayTemperatureBool

State changes added to object metadata
New metadata values have been added to represent new state changes
pickupable, isPickedUp
receptacleCount
Openable, isOpen
toggleable, isToggled
breakable, isBroken
canFillWithLiquid, isFilledWithLiquid
dirtyable, isDirty
cookable, isCooked
sliceable, isSliced
canBeUsedUp, isUsedUp
objectTemperature
canChangeTempToHot
canChangeTempToCold
mass
salientMaterials


More Sim Object Types
Additional Sim Object Types have been added to the framework
Old 105, new 113

More agent Actions
Additional Agent Navigation actions have been added
-stand, crouch

Improved Documentation



