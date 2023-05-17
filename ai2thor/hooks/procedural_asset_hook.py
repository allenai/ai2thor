# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.action_hook

Hook runner with method names that are intercepted before running
controller.step to locally run some local code

"""
import logging
import os

from ai2thor.util.runtime_assets import (
    create_asset,
    get_existing_thor_asset_file_path
)

logger = logging.getLogger(__name__)

class ProceduralAssetHookRunner(object):

    def __init__(self, asset_directory, asset_symlink=True, stop_if_fail=False, verbose=False):
        self.asset_directory = asset_directory
        self.asset_symlink = asset_symlink
        self.stop_if_fail = stop_if_fail
        

    def CreateHouse(self, action, controller):
        house = action["house"]
        asset_ids = [obj["assetId"] for obj in house["objects"]]
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
                logger.info(f"Could not create asset `{get_existing_thor_asset_file_path(out_dir=self.asset_directory, asset_id=asset_id)}`.")
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
        asset_ids = [obj["assetId"] for obj in house["objects"]]
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