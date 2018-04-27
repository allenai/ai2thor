using System;

[Serializable]
public enum SimObjManipTypePhysics : int //determines types of interactions objects can have. Some objects may have multiple manip types
{
    Undefined = 0, //default, please change it from this

    Static = 1, //for sim objects or environment elements that should not move

    Moveable = 2, //For large sim objects that can be shoved around but not picked up, things like chairs, trash cans, etc

    Receptacle = 3, //sim objects that are containers, can have smaller CanPickup objects inside them if they will fit

    CanOpen = 4, //has a lid/door/cover that is attached to the sim object in some way (cabinet, book, Drawer but NOT a pot's lid)

    CanPickup = 5, //small sim objects that can be picked up by the Agent's hand

    CanBeSliced = 6, //can be cut up into pieces (apple, bread, etc)
    CanSlice = 7, //tool that can be used to slice up sim objects that CanBeSliced

    CanBeFilledWithWater = 8, //can be filled with water (mug, container)
    CanFillWithWater = 9, //sim objects that can fill other CanBeFilledWithWater objects with water (sink faucet)

    CanBeCleaned = 10, //object can be cleaned up by an Agent with something like a cloth (Dirt, dirty mug, etc)
    CanClean = 11, //can be used TO clean up dirty objects

    CanBeCooked = 12, //food object that can be cooked in some way (apple, potato, egg)
    CanCook = 13, //can be used to cook certain CanBeCooked objects (stove top, microwave, toaster, coffee machine?)

    CanBeWatered = 14, //object can be watered by another sim object filled with water -CanBeFilled- (houseplant)
    CanWater = 15, //if object is filled (CanBeFilled), can water CanBeWatered object

    CanBeFilledWithSoap = 16, //things that can be filled with soap (soap bottle)
    CanFillWithSoap = 17, //things that can fill CanBeFilledWithSoap objects - currently no big bottle of detergent exists, however

    CanBeFilledWithTissues = 18, //for tissuebox that can be empty or full of tissues
    CanFillWithTissues = 19, //this doesn't exist yet, a stack of tissues to refill a box of tissues

    CanBeUnrolled = 20, //for things like toilet paper rolls or paper towel rolls, when used up become empty 

    CanToggleOnOff = 21, //objects that have an on and off state (light switch, TV, stove top, etc)

    CanBeMade = 22, //specifically bedding objects, "Make the Bed"

    CanBoil = 23, //can be used to heat up and boil objects filled with water (stove top, microwave, coffee maker)
    CanBeBoiled = 24, //for objects filled with water that can be boiled (pot, mug )

    CanBeCracked = 25, //for cracking eggs

}

[Serializable]
public enum SimObjTypePhysics : int //used to determine the exact type of object and also used in object ID
{
    //undefined is always the first value
    Undefined = 0,
    //ADD NEW VALUES BELOW
    //DO NOT RE-ARRANGE OLDER VALUES

    Apple = 1,//CanPickup, CanBeSliced, CanBeCooked
    AppleSlice = 2,//CanPickup, CanBeCooked
    Tomato = 3,//CanPickup, CanBeSliced, CanBeCooked
    TomatoSliced = 4,//CanPickup, CanBeCooked
    Bread = 5,//CanPickup, CanBeSliced, CanBeCooked
    BreadSliced = 6,//CanPickup, CanBeCooked
    Sink = 7,//Static, Receptacle, CanFillWithWater, CanToggleOnOff
    Pot = 8,//CanPickup, Receptacle, CanCook, CanBeFilledWithWater
    Pan = 9,//CanPickup, Receptacle, CanCook, CanBeFilledWithWater
    Knife = 10,//CanPickup, CanSlice
    Fork = 11,//CanPickup
    Spoon = 12,//CanPickup
    Bowl = 13,//CanPickup, CanClean, CanBeFilledWithWater, Receptacle, CanBeCleaned
    Toaster = 14,//Static, CanToggleOnOff, CanCook
    CoffeeMachine = 15,//static, CanBoil, Receptacle, CanToggleOnOff
    Microwave = 16,//static, receptacle, CanToggleOnOff, CanOpen, CanCook, CanBoil
    StoveBurner = 17,//static, Receptacle?, CanToggleOnOff, CanCook, CanBoil
    Fridge = 18,//static, CanOpen
    Cabinet = 19,//static, CanOpen
    Egg = 20,//CanPickup, CanBeCracked, CanBeCooked
    Chair = 21,//Moveable
    Lettuce = 22,//CanPickup, CanBeSliced, CanBeCooked
    Potato = 23,//CanPickup, CanBeSliced, CanBeCooked
    Mug = 24,//CanPickup, CanWater, CanBeFilledWithWater, Receptacle
    Plate = 25,//CanPickup, Receptacle
    TableTop = 26,//Static, Receptacle //marked for removal***
    CounterTop = 27,//Static, Receptacle
    GarbageCan = 28,//Moveable, Receptacle
    Omelette = 29,//CanPickup
    EggShell = 30,//CanPickup
    EggFried = 31,//CanPickup
    StoveKnob = 32,//static, Actionable
    Container = 33,//glass jar - CanPickup, Actionable ******** Receptacle? action to fill?
    Cup = 34,//CanPickup, Actionable ******** Receptacle? action to fill? - receptacle to put like, pen inside cup
    ButterKnife = 35,//CanPickup
    PotatoSliced = 36,//CanPickup
    MugFilled = 37,//CanPickup, ****** Actionable? Receptacle?
    BowlFilled = 38,//CanPickup, ***** Actionable? Receptacle?
    Statue = 39,//CanPickup
    LettuceSliced = 40,//CanPickup
    ContainerFull = 41,//CanPickup, ***** Actionable? Receptacle?
    BowlDirty = 42,//CanPickup, Actionable (clean dirty?)
    Sandwich = 43,//CanPickup
    Television = 44,//Static, Actionable?
    HousePlant = 45,//Moveable
    TissueBox = 46,//CanPickup, Actionable?
    VacuumCleaner = 47,//Moveable
    Painting = 48,//CanPickup - place on hanger?
    WateringCan = 49,//CanPickup, Actionable?
    Laptop = 50,//CanPickup, Actionable?
    RemoteControl = 51,//CanPickup
    Box = 52,//Receptacle, CanOpen/Actionable?
    Newspaper = 53,//CanPickup
    TissueBoxEmpty = 54,//CanPickup
    PaintingHanger = 55,//Static, Actionable
    KeyChain = 56,//CanPickup
    Dirt = 57,//Actionable? - where is dirt? do we clean with cloth? Static?
    CellPhone = 58,//CanPickup
    CreditCard = 59,//CanPickup
    Cloth = 60,//CanPickup
    Candle = 61,//CanPickup
    Toilet = 62,//Static, Actionable/CanOpen
    Plunger = 63,//CanPickup
    Bathtub = 64,//Static
    ToiletPaper = 65,//CanPickup
    ToiletPaperHanger = 66,//Static, Receptacle
    SoapBottle = 67,//CanPickup
    SoapBottleFilled = 68,//CanPickup
    SoapBar = 69,//CanPickup
    ShowerDoor = 70,//Static, CanOpen, Actionable
    SprayBottle = 71,//CanPickup
    ScrubBrush = 72,//CanPickup
    ToiletPaperRoll = 73,//CanPickup
    Lamp = 74,//Static, Actionable
    LightSwitch = 75,//Static, Actionable
    Bed = 76,//Static, Actionable
    Book = 77,//CanPickup
    AlarmClock = 78,//CanPickup
    SportsEquipment = 79,//CanPickup
    Pen = 80,//CanPickup
    Pencil = 81,//CanPickup
    Blinds = 82,//Static, Actionable
    Mirror = 83,//Actionable - clean?
    TowelHolder = 84,//Static, receptacle
    Towel = 85,//CanPickup
    Watch = 86,//CanPickup
    MiscTableObject = 87,//what? what is this?
    Drawer = 88,//Static, CanOpen, Actionable
    PotLid = 89,//CanPickup
}
