import os
import re
import argparse
import glob
import json
import pandas as pd
import numpy as np
import seaborn as sns
import matplotlib.pyplot as plt

# EACH SimObjPhysics MUST HAVE 1 Primary propert, no more no less
SimObjPrimaryProperty = {
    # NEVER LEAVE UNDEFINED
    'Undefined': 0,

    # PRIMARY PROPERTIES
    'Static': 1,
    'Moveable': 2,
    'CanPickup': 3,

    # these are to identify walls, floor, ceiling - these are not currently being used but might be in the future once
    # all scenes have their walls/floor/ceiling meshes split apart correctly (thanks Eli!)
    'Wall': 4,
    'Floor': 5,
    'Ceiling': 6
}

# EACH SimObjPhysics can have any number of Secondary Properties
SimObjSecondaryProperty = {
    # NEVER LEAVE UNDEFINED
    'Undefined': 0,

    # CLEANABLE PROPERTIES - this property defines what objects can clean certain objects - we might not use this, stay posted
    'CanBeCleanedFloor': 1,
    'CanBeCleanedDishware': 2,
    'CanBeCleanedGlass': 3,

    # OTHER SECONDARY PROPERTIES 
    'CanBeDirty': 4,
    'CanBeFilled': 5,
    'CanBeUsedUp': 6,
    'Receptacle': 7,
    'CanOpen': 8,
    'CanBeSliced': 9,
    'CanSlice': 10,
    'CanBreak': 11,
    'isHeatSource': 12,# this object can change temperature of other objects to hot
    'isColdSource': 13,# this object can change temperature of other objects to cold
    'CanBeHeatedCookware': 14,
    'CanHeatCookware': 15,
    'CanBeStoveTopCooked': 16,
    'CanStoveTopCook': 17,
    'CanBeMicrowaved': 18,
    'CanMicrowave': 19,
    'CanBeCooked': 20,
    'CanToast': 21,
    'CanBeFilledWithCoffee': 22,
    'CanFillWithCoffee': 23,
    'CanBeWatered': 24,
    'CanWater': 25,
    'CanBeFilledWithSoap': 26, # might not use this, instead categorize all as CanBeFullOrEmpty -see below 28
    'CanFillWithSoap': 27,
    'CanBeFullOrEmpty': 28, # for things that can be emptied like toilet paper, paper towel, tissue box
    'CanToggleOnOff': 29,
    'CanBeBedMade': 30,
    'CanBeMounted': 31,
    'CanMount': 32,
    'CanBeHungTowel': 33,
    'CanHangTowel': 34,
    'CanBeOnToiletPaperHolder': 35, # do not use, use object specific receptacle instead
    'CanHoldToiletPaper': 36, # do not use, use object specific receptacle instead
    'CanBeClogged': 37,
    'CanUnclog': 38,
    'CanBeOmelette': 39,
    'CanMakeOmelette': 40,
    'CanFlush': 41,
    'CanTurnOnTV': 42,

    # Might not use this, as picking up paintings is not really feasible if we have to lift and carry it. They are just way too big....
    # All Painting Mount Stuff Here
    'CanMountSmall': 43,
    'CanMountMedium': 44,
    'CanMountLarge': 45,
    'CanBeMountedSmall': 46,
    'CanBeMountedMedium': 47,
    'CanBeMountedLarge': 48,
    # End Painting Mount Stuff

    'CanBeLitOnFire': 49,
    'CanLightOnFire': 50,
    'CanSeeThrough': 51,
    'ObjectSpecificReceptacle': 52,
}

SimObjType = {
    # undefined is always the first value
    'Undefined': 0,
    # ADD NEW VALUES BELOW
    # DO NOT RE-ARRANGE OLDER VALUES
    'Apple': 1,
    'AppleSliced': 2,
    'Tomato': 3,
    'TomatoSliced': 4,
    'Bread': 5,
    'BreadSliced': 6,
    'Sink': 7,
    'Pot': 8,
    'Pan': 9,
    'Knife': 10,
    'Fork': 11,
    'Spoon': 12,
    'Bowl': 13,
    'Toaster': 14,
    'CoffeeMachine': 15,
    'Microwave': 16,
    'StoveBurner': 17,
    'Fridge': 18,
    'Cabinet': 19,
    'Egg': 20,
    'Chair': 21,
    'Lettuce': 22,
    'Potato': 23,
    'Mug': 24,
    'Plate': 25,
    'DiningTable': 26,
    'CounterTop': 27,
    'GarbageCan': 28,
    'Omelette': 29,
    'EggShell': 30,
    'EggCracked': 31,
    'StoveKnob': 32,
    'Container': 33, # for physics version - see Bottle
    'Cup': 34,
    'ButterKnife': 35,
    'PotatoSliced': 36,
    'MugFilled': 37, # not used in physics
    'BowlFilled': 38, # not used in physics
    'Statue': 39,
    'LettuceSliced': 40,
    'ContainerFull': 41, # not used in physics
    'BowlDirty': 42, # not used in physics
    'Sandwich': 43, # will need to make new prefab for physics
    'Television': 44,
    'HousePlant': 45,
    'TissueBox': 46,
    'VacuumCleaner': 47,
    'Painting': 48,# delineated sizes in physics
    'WateringCan': 49,
    'Laptop': 50,
    'RemoteControl': 51,
    'Box': 52,
    'Newspaper': 53,
    'TissueBoxEmpty': 54,# will be a state of TissuBox in physics
    'PaintingHanger': 55,# delineated sizes in physics
    'KeyChain': 56,
    'Dirt': 57, # physics will use a different cleaning system entirely
    'CellPhone': 58,
    'CreditCard': 59,
    'Cloth': 60,
    'Candle': 61,
    'Toilet': 62,
    'Plunger': 63,
    'Bathtub': 64,
    'ToiletPaper': 65,
    'ToiletPaperHanger': 66,
    'SoapBottle': 67,
    'SoapBottleFilled': 68,# DO NOT USE: Soap bottle now just has two states
    'SoapBar': 69,
    'ShowerDoor': 70,
    'SprayBottle': 71,
    'ScrubBrush': 72,
    'ToiletPaperRoll': 73,# DO NOT USE ANYMORE - ToiletPaper is now a single object that toggles states
    'Lamp': 74, # DO NOT USE: don't use this, use either FloorLamp or DeskLamp
    'LightSwitch': 75,
    'Bed': 76,
    'Book': 77,
    'AlarmClock': 78,
    'SportsEquipment': 79,# DO NOT USE: delineated into specific objects in physics - see Basketball etc
    'Pen': 80,
    'Pencil': 81,
    'Blinds': 82,
    'Mirror': 83,
    'TowelHolder': 84,
    'Towel': 85,
    'Watch': 86,
    'MiscTableObject': 87,# DO NOT USE: not sure what this is, not used for physics

    'ArmChair': 88,
    'BaseballBat': 89,
    'BasketBall': 90,
    'Faucet': 91,
    'Boots': 92,
    'Bottle': 93,
    'DishSponge': 94,
    'Drawer': 95,
    'FloorLamp': 96,
    'Kettle': 97,
    'LaundryHamper': 98,
    'LaundryHamperLid': 99,
    'Lighter': 100,
    'Ottoman': 101,
    'PaintingSmall': 102,
    'PaintingMedium': 103,
    'PaintingLarge': 104,
    'PaintingHangerSmall': 105,
    'PaintingHangerMedium': 106,
    'PaintingHangerLarge': 107,
    'PanLid': 108,
    'PaperTowelRoll': 109,
    'PepperShaker': 110,
    'PotLid': 111,
    'SaltShaker': 112,
    'Safe': 113,
    'SmallMirror': 114,# maybe don't use this, use just 'Mirror' instead
    'Sofa': 115,
    'SoapContainer': 116,
    'Spatula': 117,
    'TeddyBear': 118,
    'TennisRacket': 119,
    'Tissue': 120,
    'Vase': 121,
    'WallMirror': 122, # maybe don't use this, just use 'Mirror' instead?
    'MassObjectSpawner': 123,
    'MassScale': 124,
    'Footstool': 125,
    'Shelf': 126,
    'Dresser': 127,
    'Desk': 128,
    'SideTable': 129,
    'Pillow': 130,
    'Bench': 131,
    'Cart': 132, # bathroom cart on wheels
    'ShowerGlass': 133,
    'DeskLamp': 134,
    'Window': 135,
    'BathtubBasin': 136,
    'SinkBasin': 137,
    'CD': 138,
    'Curtains': 139,
    'Poster': 140,
    'HandTowel': 141,
    'HandTowelHolder': 142,
    'Ladle': 143,
    'WineBottle': 144,
    'ShowerCurtain': 145,
    'ShowerHead': 146,
    'TVStand': 147,
    'CoffeeTable': 148,
    'ShelvingUnit': 149,
    'AluminumFoil': 150,
    'DogBed': 151,
    'Dumbbell': 152,
    'TableTopDecor': 153, # for display pieces that are meant to be decorative and placed on tables, shelves, in cabinets etc.
    'RoomDecor': 154, # for display pieces that are mean to go on the floor of rooms, like the decorative sticks
    'Stool': 155,
    'GarbageBag': 156,
    'Desktop': 157,
    'TargetCircle': 158,
    'Floor': 159,
    'ScreenFrame': 160,
    'ScreenSheet': 161,
}

def extract_gt_objects(unique_objects):
    gt_objects = []
    with open(unique_objects) as fp:
        for line in fp.readlines():    
            asset = re.findall(r'base prefab name \(in assets\): (.*)', line)
            if len(asset) > 0:
                assert len(asset) == 1
                gt_objects.append(asset[0].split(' (UnityEngine.GameObject)')[0])
    return gt_objects

def objects_plots(df, output_dir):
    sns.set(rc = {'figure.figsize':(5, 20)})
    ax = sns.countplot(y='Type', data=df, orient='h', color='tab:blue',
        order = df['Type'].value_counts().index)
    plt.savefig(os.path.join(output_dir, 'object-types.png'), bbox_inches='tight')

def main(args):
    if args.unique_objects:
        gt_objects = extract_gt_objects(args.unique_objects)

    sim_obj_primary_property_map = dict((v,k) for k,v in SimObjPrimaryProperty.items())
    sim_obj_secondary_property_map = dict((v,k) for k,v in SimObjSecondaryProperty.items())
    sim_obj_type_map = dict((v,k) for k,v in SimObjType.items())

    gen_objects = []
    gen_object_paths = glob.glob(os.path.join(args.gen_objects, '*.json'))
    for path in gen_object_paths:
        with open(path) as fp:
            data = json.load(fp)
        has_physics = False
        is_root = True
        name = os.path.basename(path).split('.json')[0]
        for id, node in data.items():
            scipts = node.get('MonoBehaviour')
            if scipts:
                meta_src = scipts.get('SimObjPhysics')
                if not meta_src or meta_src['Type'] is None:
                    continue
                meta = {
                    'id': id,
                    'name': name,
                    'assetID': meta_src['assetID'],
                    'isRoot': is_root,
                    'Type': sim_obj_type_map[meta_src['Type']],
                    'PrimaryProperty': sim_obj_primary_property_map[meta_src['PrimaryProperty']],
                    'SecondaryProperties': [sim_obj_secondary_property_map[sec_prop] for sec_prop in meta_src['SecondaryProperties']],
                    'IsReceptacle': meta_src['IsReceptacle'],
                    'IsPickupable': meta_src['IsPickupable'],
                    'IsMoveable': meta_src['IsMoveable'],
                    'isStatic': meta_src['isStatic'],
                    'IsToggleable': meta_src['IsToggleable'],
                    'IsOpenable': meta_src['IsOpenable'],
                    'IsBreakable': meta_src['IsBreakable'],
                    'IsFillable': meta_src['IsFillable'],
                    'IsDirtyable': meta_src['IsDirtyable'],
                    'IsCookable': meta_src['IsCookable'],
                    'IsSliceable': meta_src['IsSliceable'],
                    'isHeatSource': meta_src['isHeatSource'],
                    'isColdSource': meta_src['isColdSource'],
                }
                asset = pd.DataFrame([list(meta.values())], columns=list(meta.keys()))
                gen_objects.append(asset)
                has_physics = True
                is_root = False
        if not has_physics:
            id, node = next(iter(data.items()))
            meta = {
                'id': id,
                'name': name,
                'assetID': node['name'],
                'isRoot': is_root,
                'Type': None,
                'PrimaryProperty': None,
                'SecondaryProperties': None,
                'IsReceptacle': None,
                'IsPickupable': None,
                'IsMoveable': None,
                'isStatic': None,
                'IsToggleable': None,
                'IsOpenable': None,
                'IsBreakable': None,
                'IsFillable': None,
                'IsDirtyable': None,
                'IsCookable': None,
                'IsSliceable': None,
                'isHeatSource': None,
                'isColdSource': None,
            }
            asset = pd.DataFrame([list(meta.values())], columns=list(meta.keys()))
            gen_objects.append(asset)
            
    gen_objects_df = pd.concat(gen_objects).sort_values(by=['name', 'Type'], ignore_index=True)
    print(gen_objects_df)
    gen_objects_df.to_csv(os.path.join(args.output_dir, 'ai2thor-assets.csv'), index=False)

    root_objects_df = gen_objects_df[gen_objects_df['isRoot']]
    root_objects_df.to_csv(os.path.join(args.output_dir, 'ai2thor-objects.csv'), index=False)
    objects_plots(root_objects_df, args.output_dir)


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--gen-objects', type=str, required=True)
    parser.add_argument('--unique-objects', type=str, required=False)
    parser.add_argument('--output-dir', type=str, default='./', required=False)
    args = parser.parse_args()

    main(args)
