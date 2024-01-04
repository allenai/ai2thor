import json
import logging
import os
import shutil
import multiprocessing
import pathlib
from collections import OrderedDict

from filelock import FileLock

EXTENSIONS_LOADABLE_IN_UNITY = {
    ".json",
    ".msgpack.gz",
    ".msgpack",
    ".gz",
}

logger = logging.getLogger(__name__)

def get_msgpack_save_path(out_dir, object_name):
    return os.path.join(out_dir, f"{object_name}.msgpack")


def get_msgpackgz_save_path(out_dir, object_name):
    return os.path.join(out_dir, f"{object_name}.msgpack.gz")


def get_json_save_path(out_dir, object_name):
    return os.path.join(out_dir, f"{object_name}.json")


def get_picklegz_save_path(out_dir, object_name):
    return os.path.join(out_dir, f"{object_name}.pkl.gz")


def get_gz_save_path(out_dir, object_name):
    return os.path.join(out_dir, f"{object_name}.gz")


def get_extension_save_path(out_dir, asset_id, extension):
    comp_extension = ".{extension}" if not extension.startswith(".") else extension
    return os.path.join(out_dir, f"{asset_id}{comp_extension}")


def get_existing_thor_asset_file_path(out_dir, asset_id, force_extension=None):
    OrderedDict()
    possible_paths = OrderedDict(
        [
            (".json", get_json_save_path(out_dir, asset_id)),
            (".msgpack.gz", get_msgpackgz_save_path(out_dir, asset_id)),
            (".msgpack", get_msgpack_save_path(out_dir, asset_id)),
            (".pkl.gz", get_picklegz_save_path(out_dir, asset_id)),
            (".gz", get_gz_save_path(out_dir, asset_id)),
        ]
    )
    path = None
    if force_extension is not None:
        if force_extension in possible_paths.keys():
            path = possible_paths[force_extension]
            if os.path.exists(path):
                return path
        else:
            raise Exception(
                f"Invalid extension `{force_extension}` for {asset_id}. Supported: {possible_paths.keys()}"
            )
    else:
        for path in possible_paths.values():
            if os.path.exists(path):
                return path
    raise Exception(f"Could not find existing THOR object file for {asset_id}")


def load_existing_thor_asset_file(out_dir, object_name, force_extension=None):
    file_path = get_existing_thor_asset_file_path(
        out_dir, object_name, force_extension=force_extension
    )
    if file_path.endswith(".pkl.gz"):
        import compress_pickle

        return compress_pickle.load(file_path)
    elif file_path.endswith(".msgpack.gz"):
        import gzip

        with gzip.open(file_path, "rb") as f:
            unp = f.read()
            import msgpack

            unp = msgpack.unpackb(unp)
            return unp
            # return json.dumps(unp)
    elif file_path.endswith(".gz"):
        import gzip

        with gzip.open(file_path, "rb") as f:
            unp = f.read()
            return json.dumps(unp)
    elif file_path.endswith(".msgpack"):
        with open(file_path, "rb") as f:
            unp = f.read()
            import msgpack

            unp = msgpack.unpackb(unp)
            return unp
    elif file_path.endswith(".json"):
        with open(file_path, "r") as f:
            return json.load(f)
    else:
        raise NotImplementedError(f"Unsupported file extension for path: {file_path}")


def load_existing_thor_metadata_file(out_dir):
    path = os.path.join(out_dir, f"thor_metadata.json")
    if not os.path.exists(path):
        return None

    with open(path, "r") as f:
        return json.load(f)


def save_thor_asset_file(asset_json, save_path: str):
    extension = "".join(pathlib.Path(save_path).suffixes)
    if extension == ".msgpack.gz":
        import msgpack
        import gzip

        packed = msgpack.packb(asset_json)
        with gzip.open(save_path, "wb") as outfile:
            outfile.write(packed)
    elif extension == ".msgpack":
        import msgpack

        packed = msgpack.packb(asset_json)
        with open(save_path, "wb") as outfile:
            outfile.write(packed)
    elif extension == ".gz":
        import gzip

        with gzip.open(save_path, "wt") as outfile:
            json.dump(asset_json, outfile, indent=2)
    elif extension == ".pkl.gz":
        import compress_pickle

        compress_pickle.dump(
            obj=asset_json, path=save_path, pickler_kwargs={"protocol": 4}
        )
    elif extension.endswith(".json"):
        with open(save_path, "w") as f:
            json.dump(asset_json, f, indent=2)
    else:
        raise NotImplementedError(
            f"Unsupported file extension for save path: {save_path}"
        )
    
def get_runtime_asset_filelock(save_dir, asset_id):
    return os.path.join(save_dir, f"{asset_id}.lock")

# TODO  remove  load_file_in_unity param
def create_runtime_asset_file(
    asset_directory,
    save_dir,
    asset_id,
    asset_symlink=True,
    verbose=False,
    load_file_in_unity=False,
    use_extension=None,
):
    build_target_dir = os.path.join(save_dir, asset_id)
    asset = None
    from filelock import FileLock

    # TODO figure out plender error
    with FileLock(get_runtime_asset_filelock(save_dir=save_dir, asset_id=asset_id)):
        if asset_symlink:
            exists = os.path.exists(build_target_dir)
            is_link = os.path.islink(build_target_dir)
            if exists and not is_link:
                # If not a link, delete the full directory
                if verbose:
                    logger.info(f"Deleting old asset dir: {build_target_dir}")
                shutil.rmtree(build_target_dir)
            elif is_link:
                # If not a link, delete it only if its not pointing to the right place
                if os.path.realpath(build_target_dir) != os.path.realpath(
                    asset_directory
                ):
                    os.remove(build_target_dir)

            if (not os.path.exists(build_target_dir)) and (
                not os.path.islink(build_target_dir)
            ):
                # Add symlink if it doesn't already exist
                print(f"Symlink from {asset_directory} to {build_target_dir}")
                os.symlink(asset_directory, build_target_dir)
        else:
            if verbose:
                logger.info("Starting copy and reference modification...")

            if os.path.exists(build_target_dir):
                if verbose:
                    logger.info(f"Deleting old asset dir: {build_target_dir}")
                shutil.rmtree(build_target_dir)

            # Here?
            # save_thor_asset_file(
            #     asset_json=asset_json_actions,
            #     save_path=get_existing_thor_asset_file_path(
            #         out_dir=build_target_dir, object_name=uid
            #     ),
            # )

            shutil.copytree(
                asset_directory,
                build_target_dir,
                ignore=shutil.ignore_patterns("images", "*.obj", "thor_metadata.json"),
            )

            if verbose:
                logger.info("Copy finished.")
        if not load_file_in_unity:
            return load_existing_thor_asset_file(
                out_dir=build_target_dir, object_name=asset_id
            )
        return None


def change_asset_paths(asset, save_dir):
    asset["normalTexturePath"] = os.path.join(
        save_dir,
        asset["name"],
        os.path.basename(asset["normalTexturePath"]),
    )
    asset["albedoTexturePath"] = os.path.join(
        save_dir,
        asset["name"],
        os.path.basename(asset["albedoTexturePath"]),
    )
    if "emissionTexturePath" in asset:
        asset["emissionTexturePath"] = os.path.join(
            save_dir,
            asset["name"],
            os.path.basename(asset["emissionTexturePath"]),
        )
    return asset


def make_asset_pahts_relative(asset):
    return change_asset_paths(asset, ".")


def add_default_annotations(asset, asset_directory, verbose=False):
    thor_obj_md = load_existing_thor_metadata_file(out_dir=asset_directory)
    if thor_obj_md is None:
        if verbose:
            logger.info(
                f"Object metadata for {asset['name']} is missing annotations, assuming pickupable."
            )

        asset["annotations"] = {
            "objectType": "Undefined",
            "primaryProperty": "CanPickup",
            "secondaryProperties": []
            if asset.get("receptacleCandidate", False)
            else ["Receptacle"],
        }
    else:
        asset["annotations"] = {
            "objectType": "Undefined",
            "primaryProperty": thor_obj_md["assetMetadata"]["primaryProperty"],
            "secondaryProperties": thor_obj_md["assetMetadata"]["secondaryProperties"],
        }
    return asset


def create_asset(
    thor_controller,
    asset_id,
    asset_directory,
    copy_to_dir=None,
    asset_symlink=True,
    verbose=False,
    load_file_in_unity=False,
    extension=None,
):
    # Verifies the file exists
    create_prefab_action = {}

    asset_path = get_existing_thor_asset_file_path(
        out_dir=asset_directory, asset_id=asset_id, force_extension=extension
    )
    file_extension = (
        "".join(pathlib.Path(asset_path).suffixes) if extension is None else extension
    )
    if file_extension not in EXTENSIONS_LOADABLE_IN_UNITY:
        load_file_in_unity = False
    print("--------- extension " + extension)
    copy_to_dir = (
        os.path.join(thor_controller._build.base_dir)
        if copy_to_dir is None
        else copy_to_dir
    )

    # save_dir = os.path.join(controller._build.base_dir, "processed_models")
    os.makedirs(copy_to_dir, exist_ok=True)

    if verbose:
        logger.info(f"Copying asset to THOR build dir: {copy_to_dir}.")

    asset = create_runtime_asset_file(
        asset_directory=asset_directory,
        save_dir=copy_to_dir,
        asset_id=asset_id,
        asset_symlink=asset_symlink,
        verbose=verbose,
        load_file_in_unity=load_file_in_unity,
    )

    create_prefab_action = {}
    if not load_file_in_unity:
        asset = change_asset_paths(asset=asset, save_dir=copy_to_dir)
        asset = add_default_annotations(
            asset=asset, asset_directory=asset_directory, verbose=verbose
        )
        create_prefab_action = {"action": "CreateRuntimeAsset", "asset": asset}
    else:
        create_prefab_action = {
            "action": "CreateRuntimeAsset",
            "id": asset_id,
            "dir": copy_to_dir,
            "extension": file_extension,
        }
        create_prefab_action = add_default_annotations(
            asset=create_prefab_action, asset_directory=asset_directory, verbose=verbose
        )

    evt = thor_controller.step(**create_prefab_action)
    print(f"Last Action: {thor_controller.last_action['action']}")
    if not evt.metadata["lastActionSuccess"]:
        logger.info(f"Last Action: {thor_controller.last_action['action']}")
        logger.info(f"Action success: {evt.metadata['lastActionSuccess']}")
        logger.info(f'Error: {evt.metadata["errorMessage"]}')

        logger.info(
            {
                k: v
                for k, v in create_prefab_action.items()
                if k
                in [
                    "action",
                    "name",
                    "receptacleCandidate",
                    "albedoTexturePath",
                    "normalTexturePath",
                ]
            }
        )

    return evt

def download_objaverse_assets(controller, asset_ids):
    import objaverse

    process_count = multiprocessing.cpu_count()
    objects = objaverse.load_objects(uids=asset_ids, download_processes=process_count)
    return objects


def create_assets(
    controller,
    asset_ids,
    asset_directory,
    copy_to_dir,
    asset_symlink=True,
    return_events=False,
    verbose=False,
):
    events = []
    success = True
    for asset_id in asset_ids:
        evt = create_asset(
            controller=controller,
            asset_id=asset_id,
            asset_directory=asset_directory,
            copy_to_dir=copy_to_dir,
            asset_symlink=asset_symlink,
            verbose=verbose,
        )
        success = success and evt.metadata["lastActionSuccess"]
        if return_events:
            events.append(evt)
    return success, events


# def create_assets_from_paths(controller, asset_paths, asset_symlink=True, return_events=False, verbose=False):
