import os
import glob
import argparse
import compress_pickle
import json


def normalize_texture_path(obj_dir, texture_path):
    return os.path.join(os.path.abspath(obj_dir), os.path.basename(texture_path))


def expand_objaverse(directory, ids):
    id_set = {}
    if ids != "":
        id_set = {id for id in ids.split(",")}
        print(f"selected ids: {id_set}")
    for obj_file in glob.glob(os.path.join(directory, "*", "*.pkl.gz")):
        obj_dir = os.path.dirname(obj_file)
        obj_id = os.path.basename(obj_dir)
        if not id_set or obj_id in id_set:
            obj = compress_pickle.load(obj_file)
            obj["albedoTexturePath"] = normalize_texture_path(obj_dir, obj["albedoTexturePath"])
            obj["normalTexturePath"] = normalize_texture_path(obj_dir, obj["normalTexturePath"])
            obj["emissionTexturePath"] = normalize_texture_path(obj_dir, obj["emissionTexturePath"])
            print(
                f'new paths: {obj["albedoTexturePath"]} {obj["normalTexturePath"]} {obj["emissionTexturePath"]}'
            )
            out_obj = os.path.join(obj_dir, f"{obj_id}.json")
            print(out_obj)
            with open(out_obj, "w") as f:
                json.dump(obj, f)


if __name__ == "__main__":
    # 0a0a8274693445a6b533dce7f97f747c
    parser = argparse.ArgumentParser()
    parser.add_argument("directory")
    parser.add_argument(
        "--ids",
        help="Comma separated list of ids to convert",
        type=str,
        default="",
    )
    args = parser.parse_args()
    expand_objaverse(args.directory, args.ids)
