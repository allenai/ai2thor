using System;

[Serializable]
public enum SimObjManipTypePhysics : int //determines types of interactions objects can have. Some objects may have multiple manip types
{
    Undefined = 0,
    Static = 1, //for sim objects or environment elements that should not move
    Moveable = 2, //For large sim objects that can be shoved around but not picked up, things like chairs, trash cans, etc
    CanPickup = 3, //for small sim objects that can be picked up and placed by the agent's hand
    Receptacle = 4, //for sim objects that can store smaller CanPickup objects.
    Interactable = 5, //for sim objects that have actions like turn on/off, make bed messy/tidy - animation tied to this action?
    CanOpen = 6, //for sim objects that have some sort of lid or door that is attached to the sim object (cabinets but NOT pot with lid)
}

[Serializable]
public enum SimObjTypePhysics : int //used to determine the exact type of object and also used in object ID
{
    //undefined is always the first value
    Undefined = 0,
    //ADD NEW VALUES BELOW
    //DO NOT RE-ARRANGE OLDER VALUES

    Apple = 1,//CanPickup, Interactable
    AppleSlice = 2,//CanPickup
    Tomato = 3,//CanPickup, Interactable
    TomatoSliced = 4,//CanPickup
    Bread = 5,//CanPickup, Interactable
    BreadSliced = 6,//CanPickup
    Sink = 7,//Static, Receptacle, Interactable
    Pot = 8,//CanPickup, Receptacle
    Pan = 9,//CanPickup, Receptacle
    Knife = 10,//CanPickup
    Fork = 11,//CanPickup
    Spoon = 12,//CanPickup
    Bowl = 13,//CanPickup, Interactable, ***** Receptacle?,
    Toaster = 14,//Static, Interactable
    CoffeeMachine = 15,//static, Interactable
    Microwave = 16,//static, receptacle, Interactable, CanOpen
    StoveBurner = 17,//static, Interactable
    Fridge = 18,//static, Interactable, CanOpen
    Cabinet = 19,//static, Interactable, CanOpen
    Egg = 20,//CanPickup, Interactable
    Chair = 21,//Moveable
    Lettuce = 22,//CanPickup, Interactable
    Potato = 23,//CanPickup, Interactable
    Mug = 24,//CanPickup, Interactable ******* Receptacle? Action to fill water?
    Plate = 25,//CanPickup, ***** Receptacle?
    TableTop = 26,//Static, Receptacle
    CounterTop = 27,//Static, Receptacle
    GarbageCan = 28,//Moveable, Receptacle
    Omelette = 29,//CanPickup
    EggShell = 30,//CanPickup
    EggFried = 31,//CanPickup
    StoveKnob = 32,//static, Interactable
    Container = 33,//glass jar - CanPickup, Interactable ******** Receptacle? action to fill?
    Cup = 34,//CanPickup, Interactable ******** Receptacle? action to fill? - receptacle to put like, pen inside cup
    ButterKnife = 35,//CanPickup
    PotatoSliced = 36,//CanPickup
    MugFilled = 37,//CanPickup, ****** Interactable? Receptacle?
    BowlFilled = 38,//CanPickup, ***** Interactable? Receptacle?
    Statue = 39,//CanPickup
    LettuceSliced = 40,//CanPickup
    ContainerFull = 41,//CanPickup, ***** Interactable? Receptacle?
    BowlDirty = 42,//CanPickup, Interactable (clean dirty?)
    Sandwich = 43,//CanPickup
    Television = 44,//Static, Interactable?
    HousePlant = 45,//Moveable
    TissueBox = 46,//CanPickup, Interactable?
    VacuumCleaner = 47,//Moveable
    Painting = 48,//CanPickup - place on hanger?
    WateringCan = 49,//CanPickup, Interactable?
    Laptop = 50,//CanPickup, Interactable?
    RemoteControl = 51,//CanPickup
    Box = 52,//Receptacle, CanOpen/Interactable?
    Newspaper = 53,//CanPickup
    TissueBoxEmpty = 54,//CanPickup
    PaintingHanger = 55,//Static, Interactable
    KeyChain = 56,//CanPickup
    Dirt = 57,//Interactable? - where is dirt? do we clean with cloth? Static?
    CellPhone = 58,//CanPickup
    CreditCard = 59,//CanPickup
    Cloth = 60,//CanPickup
    Candle = 61,//CanPickup
    Toilet = 62,//Static, Interactable/CanOpen
    Plunger = 63,//CanPickup
    Bathtub = 64,//Static
    ToiletPaper = 65,//CanPickup
    ToiletPaperHanger = 66,//Static, Receptacle
    SoapBottle = 67,//CanPickup
    SoapBottleFilled = 68,//CanPickup
    SoapBar = 69,//CanPickup
    ShowerDoor = 70,//Static, CanOpen, Interactable
    SprayBottle = 71,//CanPickup
    ScrubBrush = 72,//CanPickup
    ToiletPaperRoll = 73,//CanPickup
    Lamp = 74,//Static, Interactable
    LightSwitch = 75,//Static, Interactable
    Bed = 76,//Static, Interactable
    Book = 77,//CanPickup
    AlarmClock = 78,//CanPickup
    SportsEquipment = 79,//CanPickup
    Pen = 80,//CanPickup
    Pencil = 81,//CanPickup
    Blinds = 82,//Static, Interactable
    Mirror = 83,//Interactable - clean?
    TowelHolder = 84,//Static, receptacle
    Towel = 85,//CanPickup
    Watch = 86,//CanPickup
    MiscTableObject = 87,//what? what is this?
    Drawer = 88,//Static, CanOpen, Interactable
    PotLid = 89,//CanPickup
}
