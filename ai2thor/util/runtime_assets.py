import json
import logging
import os
import shutil
import multiprocessing

logger = logging.getLogger(__name__)

def get_json_save_path(out_dir, asset_id):
    return os.path.join(out_dir, f"{asset_id}.json")

def get_picklegz_save_path(out_dir, asset_id):
    return os.path.join(out_dir, f"{asset_id}.pkl.gz")

def get_existing_thor_asset_file_path(out_dir, asset_id):
    possible_paths = [
        get_json_save_path(out_dir, asset_id),
        get_picklegz_save_path(out_dir, asset_id),
    ]
    for path in possible_paths:
        if os.path.exists(path):
            return path
    raise Exception(f"Could not find existing THOR object file for {asset_id} in dir {out_dir}.")

def load_existing_thor_asset_file(out_dir, object_name):
    path = get_existing_thor_asset_file_path(out_dir, object_name)
    if path.endswith(".pkl.gz"):
        import compress_pickle

        return compress_pickle.load(path)
    elif path.endswith(".json"):
        with open(path, "r") as f:
            return json.load(f)
    else:
        raise NotImplementedError(f"Unsupported file extension for path: {path}")

def save_thor_asset_file(data, save_path: str):
    if save_path.endswith(".pkl.gz"):
        import compress_pickle

        compress_pickle.dump(obj=data, path=save_path, pickler_kwargs={"protocol": 4})
    elif save_path.endswith(".json"):
        file_dir = os.path.dirname(save_path)
        if not os.path.exists(file_dir):
            os.makedirs(file_dir, exist_ok=True)
        with open(save_path, "w") as f:
            json.dump(data, f, indent=2)
    else:
        raise NotImplementedError(f"Unsupported file extension for save path: {save_path}")

def create_asset(controller, asset_id, asset_directory, asset_symlink=True, verbose=False):
    # Verifies the file exists
    
    get_existing_thor_asset_file_path(out_dir=asset_directory, asset_id=asset_id)

    save_dir = os.path.join(controller._build.base_dir, "procedural_assets")
    os.makedirs(save_dir, exist_ok=True)

    if verbose:
        logger.info(
            f"Copying asset to THOR build dir: {save_dir}."
        )

    if asset_symlink:
        build_target_dir = os.path.join(save_dir, asset_id)
        if os.path.exists(build_target_dir):
            if not os.path.islink(build_target_dir):
                if verbose:
                    logger.info(f"Deleting old asset dir: {build_target_dir}")
                shutil.rmtree(build_target_dir)
            else:
                tmp_symlink = os.path.join(save_dir, "tmp")
                os.symlink(asset_directory, tmp_symlink)
                os.replace(tmp_symlink, build_target_dir)
        else:
            os.symlink(asset_directory, build_target_dir)
    else:
        build_target_dir = os.path.join(save_dir, asset_id)

        if verbose:
            logger.info("Starting copy and reference modification...")
        if os.path.exists(build_target_dir):
            if verbose:
                logger.info(f"Deleting old asset dir: {build_target_dir}")
            shutil.rmtree(build_target_dir)

        shutil.copytree(
            asset_directory,
            build_target_dir,
            ignore=shutil.ignore_patterns("images", "*.obj", "thor_metadata.json"),
        )
        if os.path.isabs(asset_directory):

            json_asset = load_existing_thor_asset_file(
                out_dir=build_target_dir, object_name=asset_id
            )
            # Changes reference to textures
            json_asset["albedoTexturePath"] = os.path.join(
                asset_id, os.path.basename(json_asset["albedoTexturePath"])
            )
            json_asset["normalTexturePath"] = os.path.join(
                asset_id, os.path.basename(json_asset["normalTexturePath"])
            )
            save_thor_asset_file(
                json_asset,
                get_existing_thor_asset_file_path(
                    out_dir=build_target_dir,
                    asset_id=asset_id
                )
            )
            if verbose:
                logger.info("Reference modification finished.")
        
        # shutil.copytree(
        #     asset_directory,
        #     build_target_dir,
        #     ignore=shutil.ignore_patterns("images", "*.obj", "thor_metadata.json"),
        # )

        if verbose:
            logger.info("Copy finished.")

    create_prefab_action = load_existing_thor_asset_file(out_dir=asset_directory, object_name=asset_id)
    create_prefab_action['normalTexturePath'] = create_prefab_action['normalTexturePath'].replace("/root/", "")
    create_prefab_action['albedoTexturePath'] = create_prefab_action['albedoTexturePath'].replace("/root/", "")
    evt = controller.step(**create_prefab_action)

    if not evt.metadata["lastActionSuccess"]:
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

def create_assets(controller, asset_ids, asset_directory, asset_symlink=True, return_events=False, verbose=False):
    events = []
    success = True
    for asset_id in asset_ids:
        evt = create_asset(
            controller=controller,
            asset_id=asset_id,
            asset_directory=asset_directory,
            asset_symlink=asset_symlink,
            verbose=verbose
        )
        success = success and evt.metadata["lastActionSuccess"]
        if return_events:
            events.append(evt)
    return success, events

# def create_assets_from_paths(controller, asset_paths, asset_symlink=True, return_events=False, verbose=False):

