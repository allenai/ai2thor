# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.action_hook

Hook runner with method names that are intercepted before running
controller.step to locally run some local code

"""
import logging
import os
import warnings
import pathlib

from typing import Dict, Any, List

from objathor.asset_conversion.util import (
    get_existing_thor_asset_file_path,
    create_runtime_asset_file,
    get_existing_thor_asset_file_path,
    change_asset_paths,
    add_default_annotations,
    load_existing_thor_asset_file,
)

from objathor.dataset import load_assets_path, DatasetSaveConfig

logger = logging.getLogger(__name__)

EXTENSIONS_LOADABLE_IN_UNITY = {
    ".json",
    ".json.gz",
    ".msgpack",
    ".msgpack.gz",
}


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


def create_asset(
    thor_controller,
    asset_id,
    asset_directory,
    copy_to_dir=None,
    verbose=False,
    load_file_in_unity=False,
    extension=None,
    raise_for_failure=True,
    fail_if_not_unity_loadable=False,
):
    return create_assets(
        thor_controller=thor_controller,
        asset_ids=[asset_id],
        assets_dir=asset_directory,
        copy_to_dir=copy_to_dir,
        verbose=verbose,
        load_file_in_unity=load_file_in_unity,
        extension=extension,
        fail_if_not_unity_loadable=fail_if_not_unity_loadable,
        raise_for_failure=raise_for_failure,
    )


def create_assets(
    thor_controller: Any,
    asset_ids: List[str],
    assets_dir: str,
    copy_to_dir=None,
    verbose=False,
    load_file_in_unity=False,
    extension=None,
    fail_if_not_unity_loadable=False,
    raise_for_failure=True,
):
    copy_to_dir = (
        os.path.join(thor_controller._build.base_dir) if copy_to_dir is None else copy_to_dir
    )

    multi_create_unity_loadable = dict(
        action="CreateRuntimeAssets",
        assets=[],
        dir=copy_to_dir,
        raise_for_failure=raise_for_failure,
    )

    create_with_data_actions = []
    errors = []

    for asset_id in asset_ids:
        asset_dir = os.path.join(assets_dir, asset_id)
        # Verifies the file exists
        asset_path = get_existing_thor_asset_file_path(
            out_dir=asset_dir, asset_id=asset_id, force_extension=extension
        )
        file_extension = (
            "".join(pathlib.Path(asset_path).suffixes) if extension is None else extension
        )
        load_asset_in_unity = load_file_in_unity

        if file_extension not in EXTENSIONS_LOADABLE_IN_UNITY:
            load_asset_in_unity = False
            if fail_if_not_unity_loadable:
                errors.append(asset_path)
                continue

        # save_dir = os.path.join(controller._build.base_dir, "processed_models")
        os.makedirs(copy_to_dir, exist_ok=True)

        if verbose:
            logger.info(f"Copying asset to THOR build dir: {copy_to_dir}.")

        asset = create_runtime_asset_file(
            asset_directory=asset_dir,
            save_dir=copy_to_dir,
            asset_id=asset_id,
            load_file_in_unity=load_asset_in_unity,
            verbose=verbose,
        )

        if not load_asset_in_unity:
            # TODO refactor to this when objathor changes
            # asset = load_existing_thor_asset_file(
            #     out_dir=asset_target_dir, object_name=asset_id, force_extension=file_extension
            # )
            asset = change_asset_paths(asset=asset, save_dir=copy_to_dir)
            asset = add_default_annotations(asset=asset, asset_directory=asset_dir, verbose=verbose)
            create_prefab_action = {
                "action": "CreateRuntimeAsset",
                "asset": asset,
                "raise_for_failure": raise_for_failure,
            }
            create_with_data_actions.append(create_prefab_action)
        else:
            asset_args = {
                "id": asset_id,
                "extension": file_extension,
            }
            asset_args = add_default_annotations(
                asset=asset_args, asset_directory=asset_dir, verbose=verbose
            )
            multi_create_unity_loadable["assets"].append(asset_args)

    if fail_if_not_unity_loadable:
        error_strs = ", ".join(errors)
        extensions = ", ".join(EXTENSIONS_LOADABLE_IN_UNITY)
        raise RuntimeError(
            f"Set to fail if files are not loadable in unity. Invalid extension of files `{error_strs}` must be of any of extensions {extensions}"
        )

    events = []
    # Slow pass asset data to pipe
    if len(create_with_data_actions):
        for create_asset_action in create_with_data_actions:
            evt = thor_controller.step(**create_asset_action)
            events.append(evt)
            if verbose:
                logger.info(f"Last Action: {thor_controller.last_action['action']}")

    if len(multi_create_unity_loadable):
        evt = thor_controller.step(**multi_create_unity_loadable)
        events.append(evt)
        if verbose:
            logger.debug(f"Last Action: {thor_controller.last_action['action']}")

    for evt in events:
        if not evt.metadata["lastActionSuccess"]:
            logger.error(
                f'Error: {evt.metadata["errorMessage"]}'
                f"\nLast Action: {thor_controller.last_action['action']}"
                f"\nAction success: {evt.metadata['lastActionSuccess']}"
            )
    return events


def create_assets_if_not_exist(
    controller,
    asset_ids,
    asset_directory,
    copy_to_dir,
    asset_symlink,  # TODO remove
    stop_if_fail,
    load_file_in_unity,
    extension=None,
    verbose=False,
    raise_for_failure=True,
    fail_if_not_unity_loadable=False,
):
    evt = controller.step(
        action="AssetsInDatabase", assetIds=asset_ids, updateProceduralLRUCache=True
    )

    asset_in_db = evt.metadata["actionReturn"]
    assets_not_created = [asset_id for (asset_id, in_db) in asset_in_db.items() if not in_db]

    events = create_assets(
        thor_controller=controller,
        asset_ids=assets_not_created,
        assets_dir=asset_directory,
        copy_to_dir=copy_to_dir,
        verbose=verbose,
        load_file_in_unity=load_file_in_unity,
        extension=extension,
        fail_if_not_unity_loadable=fail_if_not_unity_loadable,
    )
    for evt, i in zip(events, range(len(events))):
        if not evt.metadata["lastActionSuccess"]:
            # TODO do a better matching of asset_ids and event
            asset_id = assets_not_created[i] if i < len(events) else None
            asset_path = (
                get_existing_thor_asset_file_path(out_dir=asset_directory, asset_id=asset_id)
                if asset_id is not None
                else ""
            )
            warnings.warn(
                f"Could not create asset `{asset_path}`." f"\nError: {evt.metadata['errorMessage']}"
            )
    return events[-1]

    # slower
    # for asset_id in assets_not_created:
    #     asset_dir = os.path.abspath(os.path.join(asset_directory, asset_id))
    #     # print(f"Create {asset_id}")
    #     evt = create_asset(
    #         thor_controller=controller,
    #         asset_id=asset_id,
    #         asset_directory=asset_dir,
    #         copy_to_dir=copy_to_dir,
    #         verbose=verbose,
    #         load_file_in_unity=load_file_in_unity,
    #         extension=None,
    #         # raise_for_failure=raise_for_failure,
    #     )
    #     if not evt.metadata["lastActionSuccess"]:
    #         warnings.warn(
    #             f"Could not create asset `{get_existing_thor_asset_file_path(out_dir=asset_dir, asset_id=asset_id)}`."
    #             f"\nError: {evt.metadata['errorMessage']}"
    #         )
    #     if stop_if_fail:
    #         return evt


class ProceduralAssetActionCallback:
    def __init__(
        self,
        asset_directory,
        target_dir="processed_models",
        asset_symlink=True,
        load_file_in_unity=False,
        stop_if_fail=False,
        asset_limit=-1,
        extension=None,
        verbose=True,
    ):
        self.asset_directory = asset_directory
        self.asset_symlink = asset_symlink
        self.stop_if_fail = stop_if_fail
        self.asset_limit = asset_limit
        self.load_file_in_unity = load_file_in_unity
        self.target_dir = target_dir
        self.extension = extension
        self.verbose = verbose
        self.last_asset_id_set = set()

    def Initialize(self, action, controller):
        if self.asset_limit > 0:
            return controller.step(
                action="DeleteLRUFromProceduralCache", assetLimit=self.asset_limit
            )

    def CreateHouse(self, action, controller):
        house = action["house"]
        asset_ids = get_all_asset_ids_recursively(house["objects"], [])
        asset_ids_set = set(asset_ids)
        if not asset_ids_set.issubset(self.last_asset_id_set):
            controller.step(action="DeleteLRUFromProceduralCache", assetLimit=0)
            self.last_asset_id_set = set(asset_ids)
        return create_assets_if_not_exist(
            controller=controller,
            asset_ids=asset_ids,
            asset_directory=self.asset_directory,
            copy_to_dir=os.path.join(controller._build.base_dir, self.target_dir),
            asset_symlink=self.asset_symlink,
            stop_if_fail=self.stop_if_fail,
            load_file_in_unity=self.load_file_in_unity,
            extension=self.extension,
            verbose=self.verbose,
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
            extension=self.extension,
            verbose=self.verbose,
        )

    def GetHouseFromTemplate(self, action, controller):
        template = action["template"]
        asset_ids = get_all_asset_ids_recursively([v for (k, v) in template["objects"].items()], [])
        return create_assets_if_not_exist(
            controller=controller,
            asset_ids=asset_ids,
            asset_directory=self.asset_directory,
            copy_to_dir=os.path.join(controller._build.base_dir, self.target_dir),
            asset_symlink=self.asset_symlink,
            stop_if_fail=self.stop_if_fail,
            load_file_in_unity=self.load_file_in_unity,
            extension=self.extension,
            verbose=self.verbose,
        )


class DownloadObjaverseActionCallback(object):
    def __init__(
        self,
        asset_dataset_version,
        asset_download_path,
        target_dir="processed_models",
        asset_symlink=True,
        load_file_in_unity=False,
        stop_if_fail=False,
        asset_limit=-1,
        extension=None,
        verbose=True,
    ):
        self.asset_download_path = asset_download_path
        self.asset_symlink = asset_symlink
        self.stop_if_fail = stop_if_fail
        self.asset_limit = asset_limit
        self.load_file_in_unity = load_file_in_unity
        self.target_dir = target_dir
        self.extension = extension
        self.verbose = verbose
        self.last_asset_id_set = set()
        dsc = DatasetSaveConfig(
            VERSION=asset_dataset_version,
            BASE_PATH=asset_download_path,
        )
        self.asset_path = load_assets_path(dsc)

    def CreateHouse(self, action, controller):
        house = action["house"]
        asset_ids = get_all_asset_ids_recursively(house["objects"], [])
        asset_ids_set = set(asset_ids)
        if not asset_ids_set.issubset(self.last_asset_id_set):
            controller.step(action="DeleteLRUFromProceduralCache", assetLimit=0)
            self.last_asset_id_set = set(asset_ids)
        return create_assets_if_not_exist(
            controller=controller,
            asset_ids=asset_ids,
            asset_directory=self.asset_path,
            copy_to_dir=os.path.join(controller._build.base_dir, self.target_dir),
            asset_symlink=self.asset_symlink,
            stop_if_fail=self.stop_if_fail,
            load_file_in_unity=self.load_file_in_unity,
            extension=self.extension,
            verbose=self.verbose,
        )
