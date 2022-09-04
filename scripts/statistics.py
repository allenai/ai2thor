import os
import re
import argparse
import glob
import json
import pandas as pd
import numpy as np

def extract_gt_objects(unique_objects):
    gt_objects = []
    with open(unique_objects) as fp:
        for line in fp.readlines():    
            asset = re.findall(r'base prefab name \(in assets\): (.*)', line)
            if len(asset) > 0:
                assert len(asset) == 1
                gt_objects.append(asset[0].split(' (UnityEngine.GameObject)')[0])
    return gt_objects

def main(args):
    if args.unique_objects:
        gt_objects = extract_gt_objects(args.unique_objects)

    gen_objects = []
    gen_object_paths = glob.glob(os.path.join(args.gen_objects, '*.json'))
    for path in gen_object_paths:
        with open(path) as fp:
            data = json.load(fp)
        has_physics = False
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
                    'Type': meta_src['Type'],
                    'PrimaryProperty': meta_src['PrimaryProperty'],
                    'SecondaryProperties': meta_src['SecondaryProperties'],
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
        if not has_physics:
            id, node = next(iter(data.items()))
            meta = {
                'id': id,
                'name': name,
                'assetID': node['name'],
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
            
    gen_objects_df = pd.concat(gen_objects, ignore_index=True)
    print(gen_objects_df)
    gen_objects_df.to_csv(os.path.join(args.output_dir, 'ai2thor-assets.csv'))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--gen-objects', type=str, required=True)
    parser.add_argument('--unique-objects', type=str, required=False)
    parser.add_argument('--output-dir', type=str, default='./', required=False)
    args = parser.parse_args()

    main(args)
