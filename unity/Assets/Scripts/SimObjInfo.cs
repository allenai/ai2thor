// DO NOT USE THIS ANYMORE - WAS USED FOR DEBUG TESTING - Please see SimObjType.cs
using System;

[Serializable]
public enum SimObjProperty : int //Properties of a SimObjPhysics that determine how they can be interacted with
{
    Undefined, //NEVER LEAVE ON UNDEFINED

    //PRIMARY PROPERTIES - EACH SimObjPhysics must have ONE Primary Property
    Static,
    Moveable,
    CanPickup,

    //CLEANABLE PROPERATIES - this property defines what objects can clean certain objects
    CanBeCleanedFloor,
    CanBeCleanedDishware,
    CanBeCleanedGlass,

    //SECONDARY PROPERTIES - EACH SimObjPhysics can have any number of Secondary Properties
    CanCleanFloor,
    CanCleanDishware,
    CanCleanGlass,
    Receptacle,
    CanOpen,
    CanBeSliced,
    CanSlice,
    CanBeCracked,
    CanBeFilledWithWater,
    CanFillWithWater,
    CanBeHeatedCookware,
    CanHeatCookware,
    CanBeStoveTopCooked,
    CanStoveTopCook,
    CanBeMicrowaved,
    CanMicrowave,
    CanBeToasted,
    CanToast,
    CanBeFilledWithCoffee,
    CanFillWithCoffee,
    CanBeWatered,
    CanWater,
    CanBeFilledWithSoap,
    CanFillWithSoap,
    CanBeUnrolled,
    CanToggleOnOff,
    CanBeBedMade,
    CanBeMounted,
    CanMount,
    CanBeHungTowel,
    CanHangTowel,
    CanBeOnToiletPaperHolder,
    CanHoldToiletPaper,
    CanBeClogged,
    CanUnclog,
    CanBeOmelette,
    CanMakeOmelette,
    CanFlush,
    CanTurnOnTV,
    CanMountSmall,
    CanMountMedium,
    CanMountLarge,
    CanBeMountedSmall,
    CanBeMountedMedium,
    CanBeMountedLarge,
    CanBeLitOnFire,
    CanLightOnFire   
}


