# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.action_hook

Hook runner with method names that are intercepted before running
controller.step to locally run some local code

"""
import logging
import os
from typing import Dict, Any, List

from ai2thor.util.runtime_assets import (
    create_asset,
    get_existing_thor_asset_file_path
)

logger = logging.getLogger(__name__)

def get_all_asset_ids_recursively(objects: List[Dict[str, Any]], asset_ids: List[str]) -> List[str]:
    """
    Get all asset IDs in a house.
    """
    for obj in objects:
        asset_ids.append(obj["assetId"])
        if "children" in obj:
            get_all_asset_ids_recursively(obj["children"], asset_ids)
    assets_set = set(asset_ids)
    if "" in assets_set:
        assets_set.remove("")
    return list(assets_set)



class ProceduralAssetHookRunner(object):

    def __init__(self, asset_directory, asset_symlink=True, stop_if_fail=False, verbose=False):
        self.asset_directory = asset_directory
        self.asset_symlink = asset_symlink
        self.stop_if_fail = stop_if_fail
        

    def CreateHouse(self, action, controller):
        house = action["house"]
        asset_ids = get_all_asset_ids_recursively(house["objects"], [])
        evt = controller.step(action="AssetsInDatabase", assetIds=asset_ids)
        asset_in_db = evt.metadata["actionReturn"]
        assets_not_created = [asset_id for (asset_id, in_db) in asset_in_db.items() if not in_db]
        for asset_id in assets_not_created:
            asset_dir = os.path.abspath(os.path.join(self.asset_directory, asset_id))
            evt = create_asset(
                controller=controller,
                asset_id=asset_id,
                asset_directory=asset_dir,
                asset_symlink=self.asset_symlink,
                verbose=True
            )
            if not evt.metadata["lastActionSuccess"]:
                logger.info(f"Could not create asset `{get_existing_thor_asset_file_path(out_dir=asset_dir, asset_id=asset_id)}`.")
                logger.info(f"Error: {evt.metadata['errorMessage']}")
            if self.stop_if_fail:
                return evt
        return evt

    def SpawnAsset(self, action, controller):
        asset_ids = [action["assetId"]]
        evt = controller.step(action="AssetsInDatabase", assetIds=asset_ids)
        asset_in_db = evt.metadata["actionReturn"]
        assets_not_created = [asset_id for (asset_id, in_db) in asset_in_db.items() if not in_db]
        for asset_id in assets_not_created:
            asset_dir = os.path.abspath(os.path.join(self.asset_directory, asset_id))
            evt = create_asset(
                controller=controller,
                asset_id=asset_id,
                asset_directory=asset_dir,
                asset_symlink=self.asset_symlink,
                verbose=True
            )
            if not evt.metadata["lastActionSuccess"]:
                logger.info(
                    f"Could not create asset `{get_existing_thor_asset_file_path(out_dir=asset_dir, asset_id=asset_id)}`.")
                logger.info(f"Error: {evt.metadata['errorMessage']}")
            if self.stop_if_fail:
                return evt
        return evt


class ObjaverseAssetHookRunner(object):
    def __init__(self):
        import objaverse
        self.objaverse_uid_set = set(objaverse.load_uids())

    def CreateHouse(self, action, controller):
        raise NotImplemented("Not yet implemented.")
    
        house = action["house"]
        asset_ids = list(set(obj["assetId"] for obj in house["objects"]))
        evt = controller.step(action="AssetsInDatabase", assetIds=asset_ids)
        asset_in_db = evt.metadata["actionReturn"]
        assets_not_created = [asset_id for (asset_id, in_db) in asset_in_db.items() if in_db]
        not_created_set = set(assets_not_created)
        not_objeverse_not_created = not_created_set.difference(self.objaverse_uid_set)
        if len(not_created_set):
            raise ValueError(f"Invalid asset ids are not in THOR AssetDatabase or part of objeverse: {not_objeverse_not_created}")
        
        # TODO when transformed assets are in objaverse download them and create them
        # objaverse.load_thor_objects
        # create_assets()