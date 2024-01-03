# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.action_hook

Hook runner with method names that are intercepted before running
controller.step to locally run some local code

"""
import logging
import os
from typing import Dict, Any, List

from ai2thor.util.runtime_assets import create_asset, get_existing_thor_asset_file_path

logger = logging.getLogger(__name__)


def get_all_asset_ids_recursively(
    objects: List[Dict[str, Any]], asset_ids: List[str]
) -> List[str]:
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


def create_assets_if_not_exist(
    controller, asset_ids, asset_directory, copy_to_dir, asset_symlink, stop_if_fail, load_file_in_unity, verbose=False
):
    evt = controller.step(
        action="AssetsInDatabase", assetIds=asset_ids, updateProceduralLRUCache=True
    )

    asset_in_db = evt.metadata["actionReturn"]
    assets_not_created = [
        asset_id for (asset_id, in_db) in asset_in_db.items() if not in_db
    ]
    for asset_id in assets_not_created:
        asset_dir = os.path.abspath(os.path.join(asset_directory, asset_id))
        # print(f"Create {asset_id}")
        evt = create_asset(
            controller=controller,
            asset_id=asset_id,
            asset_directory=asset_dir,
            copy_to_dir=copy_to_dir,
            asset_symlink=asset_symlink,
            verbose=verbose,
            load_file_in_unity=load_file_in_unity
        )
        if not evt.metadata["lastActionSuccess"]:
            logger.info(
                f"Could not create asset `{get_existing_thor_asset_file_path(out_dir=asset_dir, asset_id=asset_id)}`."
            )
            logger.info(f"Error: {evt.metadata['errorMessage']}")
        if stop_if_fail:
            return evt
    return evt


class ProceduralAssetHookRunner:
    def __init__(
        self,
        asset_directory,
        target_dir="processed_models",
        asset_symlink=True,
        load_file_in_unity=False,
        stop_if_fail=False,
        asset_limit=-1,
        verbose=True,
    ):
        self.asset_directory = asset_directory
        self.asset_symlink = asset_symlink
        self.stop_if_fail = stop_if_fail
        self.asset_limit = asset_limit
        self.load_file_in_unity = load_file_in_unity
        self.target_dir = target_dir
        self.verbose = verbose

    def Initialize(self, action, controller):
        if self.asset_limit > 0:
            return controller.step(
                action="DeleteLRUFromProceduralCache", assetLimit=self.asset_limit
            )

    def CreateHouse(self, action, controller):
        house = action["house"]
        asset_ids = get_all_asset_ids_recursively(house["objects"], [])
        return create_assets_if_not_exist(
            controller=controller,
            asset_ids=asset_ids,
            asset_directory=self.asset_directory,
            copy_to_dir=os.path.join(controller._build.base_dir, self.target_dir),
            asset_symlink=self.asset_symlink,
            stop_if_fail=self.stop_if_fail,
            load_file_in_unity=self.load_file_in_unity
            verbose=self.verbose
        )

    def SpawnAsset(self, action, controller):
        asset_ids = [action["assetId"]]
        return create_assets_if_not_exist(
            controller=controller,
            asset_ids=asset_ids,
            asset_directory=self.asset_directory,
            copy_to_dir=os.path.join(controller._build.base_dir, self.target_dir),
            asset_symlink=self.asset_symlink,
            stop_if_fail=self.stop_if_fail,
            load_file_in_unity=self.load_file_in_unity,
            verbose=self.verbose
        )

    def GetHouseFromTemplate(self, action, controller):
        template = action["template"]
        asset_ids = get_all_asset_ids_recursively(
            [v for (k, v) in template["objects"].items()], []
        )
        return create_assets_if_not_exist(
            controller=controller,
            asset_ids=asset_ids,
            asset_directory=self.asset_directory,
            copy_to_dir=os.path.join(controller._build.base_dir, self.target_dir),
            asset_symlink=self.asset_symlink,
            stop_if_fail=self.stop_if_fail,
            load_file_in_unity=self.load_file_in_unity,
            verbose=self.verbose
        )


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
        assets_not_created = [
            asset_id for (asset_id, in_db) in asset_in_db.items() if in_db
        ]
        not_created_set = set(assets_not_created)
        not_objeverse_not_created = not_created_set.difference(self.objaverse_uid_set)
        if len(not_created_set):
            raise ValueError(
                f"Invalid asset ids are not in THOR AssetDatabase or part of objeverse: {not_objeverse_not_created}"
            )

        # TODO when transformed assets are in objaverse download them and create them
        # objaverse.load_thor_objects
        # create_assets()
