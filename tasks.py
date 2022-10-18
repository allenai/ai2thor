import os
import sys
import fcntl
import datetime
import json
import re
import time
import zipfile
import threading
import hashlib
import shutil
import subprocess
import pprint
import random
from invoke import task
import boto3
import botocore.exceptions
import multiprocessing
import io
import ai2thor.build
import logging
import glob

from ai2thor.build import TEST_OUTPUT_DIRECTORY

logger = logging.getLogger()
logger.setLevel(logging.INFO)
handler = logging.StreamHandler(sys.stdout)
handler.setLevel(logging.INFO)

formatter = logging.Formatter(
    "%(asctime)s [%(process)d] %(funcName)s - %(levelname)s - %(message)s"
)
handler.setFormatter(formatter)
logger.addHandler(handler)

content_types = {
    ".js": "application/javascript; charset=utf-8",
    ".html": "text/html; charset=utf-8",
    ".ico": "image/x-icon",
    ".svg": "image/svg+xml; charset=utf-8",
    ".css": "text/css; charset=utf-8",
    ".png": "image/png",
    ".txt": "text/plain",
    ".jpg": "image/jpeg",
    ".wasm": "application/wasm",
    ".data": "application/octet-stream",
    ".unityweb": "application/octet-stream",
    ".json": "application/json",
}

def add_files(zipf, start_dir, exclude_ext=()):
    for root, dirs, files in os.walk(start_dir):
        for f in files:
            fn = os.path.join(root, f)
            if any(map(lambda ext: fn.endswith(ext), exclude_ext)):
                #print("skipping file %s" % fn)
                continue

            arcname = os.path.relpath(fn, start_dir)
            if arcname.split("/")[0].endswith("_BackUpThisFolder_ButDontShipItWithYourGame"):
                # print("skipping %s" % arcname)
                continue
            # print("adding %s" % arcname)
            zipf.write(fn, arcname)


def push_build(build_archive_name, zip_data, include_private_scenes):
    logger.info("start of push_build")
    import boto3
    from base64 import b64encode

    # subprocess.run("ls %s" % build_archive_name, shell=True)
    # subprocess.run("gsha256sum %s" % build_archive_name)
    logger.info("boto3 resource")
    s3 = boto3.resource("s3", region_name="us-west-2")
    acl = "public-read"
    bucket = ai2thor.build.PUBLIC_S3_BUCKET
    if include_private_scenes:
        bucket = ai2thor.build.PRIVATE_S3_BUCKET
        acl = "private"

    logger.info("archive base")
    archive_base = os.path.basename(build_archive_name)
    key = "builds/%s" % (archive_base,)
    sha256_key = "builds/%s.sha256" % (os.path.splitext(archive_base)[0],)

    logger.info("hashlib sha256")
    sha = hashlib.sha256(zip_data)
    try:
        logger.info("pushing build %s" % (key,))
        s3.Object(bucket, key).put(Body=zip_data, ACL=acl, ChecksumSHA256=b64encode(sha.digest()).decode('ascii'))
        logger.info("pushing sha256 %s" % (sha256_key,))
        s3.Object(bucket, sha256_key).put(
            Body=sha.hexdigest(), ACL=acl, ContentType="text/plain"
        )
    except botocore.exceptions.ClientError as e:
        logger.error("caught error uploading archive %s: %s" % (build_archive_name, e))

    logger.info("pushed build %s to %s" % (bucket, build_archive_name))


def _webgl_local_build_path(prefix, source_dir="builds"):
    return os.path.join(
        os.getcwd(), "unity/{}/thor-{}-WebGL/".format(source_dir, prefix)
    )


def _unity_version():
    import yaml

    with open("unity/ProjectSettings/ProjectVersion.txt") as pf:
        project_version = yaml.load(pf.read(), Loader=yaml.FullLoader)

    return project_version["m_EditorVersion"]


def _unity_path():
    unity_version = _unity_version()
    standalone_path = None

    if sys.platform.startswith("darwin"):
        unity_hub_path = (
            "/Applications/Unity/Hub/Editor/{}/Unity.app/Contents/MacOS/Unity".format(
                unity_version
            )
        )
        # /Applications/Unity/2019.4.20f1/Unity.app/Contents/MacOS

        standalone_path = (
            "/Applications/Unity/{}/Unity.app/Contents/MacOS/Unity".format(
                unity_version
            )
        )
        # standalone_path = (
        #     "/Applications/Unity-{}/Unity.app/Contents/MacOS/Unity".format(
        #         unity_version
        #     )
        # )
    elif "win" in sys.platform:
        unity_hub_path = "C:/PROGRA~1/Unity/Hub/Editor/{}/Editor/Unity.exe".format(
            unity_version
        )
        # TODO: Verify windows unity standalone path
        standalone_path = "C:/PROGRA~1/{}/Editor/Unity.exe".format(unity_version)
    elif sys.platform.startswith("linux"):
        unity_hub_path = "{}/Unity/Hub/Editor/{}/Editor/Unity".format(
            os.environ["HOME"], unity_version
        )

    if standalone_path and os.path.exists(standalone_path):
        unity_path = standalone_path
    else:
        unity_path = unity_hub_path

    return unity_path


def _build(unity_path, arch, build_dir, build_name, env={}):
    import yaml

    project_path = os.path.join(os.getcwd(), unity_path)

    # osxintel64 is not a BuildTarget
    build_target_map = dict(OSXIntel64="OSXUniversal")

    # -buildTarget must be passed as an option for the CloudRendering target otherwise a clang error
    # will get thrown complaining about missing features.h
    command = (
        "%s -quit -batchmode -logFile %s/%s.log -projectpath %s -buildTarget %s -executeMethod Build.%s"
        % (_unity_path(), os.getcwd(), build_name, project_path, build_target_map.get(arch, arch), arch)
    )

    target_path = os.path.join(build_dir, build_name)

    full_env = os.environ.copy()
    full_env.update(env)
    full_env["UNITY_BUILD_NAME"] = target_path
    result_code = subprocess.check_call(command, shell=True, env=full_env)
    print("Exited with code {}".format(result_code))
    success = result_code == 0
    if success:
        generate_build_metadata(os.path.join(project_path, build_dir, "metadata.json"))
    return success


def generate_build_metadata(metadata_path):

    # this server_types metadata is maintained
    # to allow future versions of the Python API
    # to launch older versions of the Unity build
    # and know whether the Fifo server is available
    server_types = ["WSGI"]
    try:
        import ai2thor.fifo_server

        server_types.append("FIFO")
    except Exception as e:
        pass

    with open(os.path.join(metadata_path), "w") as f:
        f.write(json.dumps(dict(server_types=server_types)))


def class_dataset_images_for_scene(scene_name):
    import ai2thor.controller
    from itertools import product
    from collections import defaultdict
    import numpy as np
    import cv2

    env = ai2thor.controller.Controller(quality="Low")
    player_size = 300
    zoom_size = 1000
    target_size = 256
    rotations = [0, 90, 180, 270]
    horizons = [330, 0, 30]
    buffer = 15
    # object must be at least 40% in view
    min_size = ((target_size * 0.4) / zoom_size) * player_size

    env.start(width=player_size, height=player_size)
    env.reset(scene_name)
    event = env.step(
        dict(
            action="Initialize",
            gridSize=0.25,
            renderInstanceSegmentation=True,
            renderSemanticSegmentation=False,
            renderImage=False,
        )
    )

    for o in event.metadata["objects"]:
        if o["receptacle"] and o["receptacleObjectIds"] and o["openable"]:
            print("opening %s" % o["objectId"])
            env.step(
                dict(action="OpenObject", objectId=o["objectId"], forceAction=True)
            )

    event = env.step(dict(action="GetReachablePositions", gridSize=0.25))

    visible_object_locations = []
    for point in event.metadata["actionReturn"]:
        for rot, hor in product(rotations, horizons):
            exclude_colors = set(
                map(tuple, np.unique(event.instance_segmentation_frame[0], axis=0))
            )
            exclude_colors.update(
                set(
                    map(
                        tuple,
                        np.unique(event.instance_segmentation_frame[:, -1, :], axis=0),
                    )
                )
            )
            exclude_colors.update(
                set(
                    map(tuple, np.unique(event.instance_segmentation_frame[-1], axis=0))
                )
            )
            exclude_colors.update(
                set(
                    map(
                        tuple,
                        np.unique(event.instance_segmentation_frame[:, 0, :], axis=0),
                    )
                )
            )

            event = env.step(
                dict(
                    action="TeleportFull",
                    x=point["x"],
                    y=point["y"],
                    z=point["z"],
                    rotation=rot,
                    horizon=hor,
                    forceAction=True,
                ),
                raise_for_failure=True,
            )

            visible_objects = []

            for o in event.metadata["objects"]:

                if o["visible"] and o["objectId"] and o["pickupable"]:
                    color = event.object_id_to_color[o["objectId"]]
                    mask = (
                        (event.instance_segmentation_frame[:, :, 0] == color[0])
                        & (event.instance_segmentation_frame[:, :, 1] == color[1])
                        & (event.instance_segmentation_frame[:, :, 2] == color[2])
                    )
                    points = np.argwhere(mask)

                    if len(points) > 0:
                        min_y = int(np.min(points[:, 0]))
                        max_y = int(np.max(points[:, 0]))
                        min_x = int(np.min(points[:, 1]))
                        max_x = int(np.max(points[:, 1]))
                        max_dim = max((max_y - min_y), (max_x - min_x))
                        if (
                            max_dim > min_size
                            and min_y > buffer
                            and min_x > buffer
                            and max_x < (player_size - buffer)
                            and max_y < (player_size - buffer)
                        ):
                            visible_objects.append(
                                dict(
                                    objectId=o["objectId"],
                                    min_x=min_x,
                                    min_y=min_y,
                                    max_x=max_x,
                                    max_y=max_y,
                                )
                            )
                            print(
                                "[%s] including object id %s %s"
                                % (scene_name, o["objectId"], max_dim)
                            )

            if visible_objects:
                visible_object_locations.append(
                    dict(point=point, rot=rot, hor=hor, visible_objects=visible_objects)
                )

    env.stop()
    env = ai2thor.controller.Controller()
    env.start(width=zoom_size, height=zoom_size)
    env.reset(scene_name)
    event = env.step(dict(action="Initialize", gridSize=0.25))

    for o in event.metadata["objects"]:
        if o["receptacle"] and o["receptacleObjectIds"] and o["openable"]:
            print("opening %s" % o["objectId"])
            env.step(
                dict(action="OpenObject", objectId=o["objectId"], forceAction=True)
            )

    for vol in visible_object_locations:
        point = vol["point"]

        event = env.step(
            dict(
                action="TeleportFull",
                x=point["x"],
                y=point["y"],
                z=point["z"],
                rotation=vol["rot"],
                horizon=vol["hor"],
                forceAction=True,
            ),
            raise_for_failure=True,
        )
        for v in vol["visible_objects"]:
            object_id = v["objectId"]
            min_y = int(round(v["min_y"] * (zoom_size / player_size)))
            max_y = int(round(v["max_y"] * (zoom_size / player_size)))
            max_x = int(round(v["max_x"] * (zoom_size / player_size)))
            min_x = int(round(v["min_x"] * (zoom_size / player_size)))
            delta_y = max_y - min_y
            delta_x = max_x - min_x
            scaled_target_size = max(delta_x, delta_y, target_size) + buffer * 2
            if min_x > (zoom_size - max_x):
                start_x = min_x - (scaled_target_size - delta_x)
                end_x = max_x + buffer
            else:
                end_x = max_x + (scaled_target_size - delta_x)
                start_x = min_x - buffer

            if min_y > (zoom_size - max_y):
                start_y = min_y - (scaled_target_size - delta_y)
                end_y = max_y + buffer
            else:
                end_y = max_y + (scaled_target_size - delta_y)
                start_y = min_y - buffer

            # print("max x %s max y %s min x %s  min y %s" % (max_x, max_y, min_x, min_y))
            # print("start x %s start_y %s end_x %s end y %s" % (start_x, start_y, end_x, end_y))
            print("storing %s " % object_id)
            img = event.cv2img[start_y:end_y, start_x:end_x, :]
            dst = cv2.resize(
                img, (target_size, target_size), interpolation=cv2.INTER_LANCZOS4
            )

            object_type = object_id.split("|")[0].lower()
            target_dir = os.path.join("images", scene_name, object_type)
            h = hashlib.md5()
            h.update(json.dumps(point, sort_keys=True).encode("utf8"))
            h.update(json.dumps(v, sort_keys=True).encode("utf8"))

            os.makedirs(target_dir, exist_ok=True)

            cv2.imwrite(os.path.join(target_dir, h.hexdigest() + ".png"), dst)

    env.stop()

    return scene_name


@task
def build_class_dataset(context):
    import concurrent.futures
    import ai2thor.controller

    multiprocessing.set_start_method("spawn")

    controller = ai2thor.controller.Controller()
    executor = concurrent.futures.ProcessPoolExecutor(max_workers=4)
    futures = []

    for scene in controller.scene_names():
        print("processing scene %s" % scene)
        futures.append(executor.submit(class_dataset_images_for_scene, scene))

    for f in concurrent.futures.as_completed(futures):
        scene = f.result()
        print("scene name complete: %s" % scene)


def local_build_name(prefix, arch):
    return "thor-%s-%s" % (prefix, arch)


@task
def local_build_test(context, prefix="local", arch="OSXIntel64"):
    from ai2thor.tests.constants import TEST_SCENE

    local_build(context, prefix, arch, [TEST_SCENE])


@task(iterable=["scenes"])
def local_build(
    context, prefix="local", arch="OSXIntel64", scenes=None, scripts_only=False
):
    import ai2thor.controller

    build = ai2thor.build.Build(arch, prefix, False)
    env = dict()
    if os.path.isdir("unity/Assets/Private/Scenes"):
        env["INCLUDE_PRIVATE_SCENES"] = "true"

    build_dir = os.path.join("builds", build.name)
    if scripts_only:
        env["BUILD_SCRIPTS_ONLY"] = "true"

    if scenes:
        env["BUILD_SCENES"] = ",".join(
            map(ai2thor.controller.Controller.normalize_scene, scenes)
        )

    if _build("unity", arch, build_dir, build.name, env=env):
        print("Build Successful")
    else:
        print("Build Failure")
    generate_quality_settings(context)


@task
def webgl_build(
    context,
    scenes="",
    room_ranges=None,
    directory="builds",
    prefix="local",
    verbose=False,
    content_addressable=False,
    crowdsource_build=False,
):
    """
    Creates a WebGL build
    :param context:
    :param scenes: String of scenes to include in the build as a comma separated list
    :param prefix: Prefix name for the build
    :param content_addressable: Whether to change the unityweb build files to be content-addressable
                                have their content hashes as part of their names.
    :return:
    """
    from functools import reduce

    def file_to_content_addressable(file_path):
        # name_split = os.path.splitext(file_path)
        path_split = os.path.split(file_path)
        directory = path_split[0]
        file_name = path_split[1]

        print("File name {} ".format(file_name))
        with open(file_path, "rb") as f:
            h = hashlib.md5()
            h.update(f.read())
            md5_id = h.hexdigest()
        new_file_name = "{}_{}".format(md5_id, file_name)
        os.rename(file_path, os.path.join(directory, new_file_name))

    arch = "WebGL"
    build_name = local_build_name(prefix, arch)

    if room_ranges is not None:
        floor_plans = [
            "FloorPlan{}_physics".format(i)
            for i in reduce(
                lambda x, y: x + y,
                map(
                    lambda x: x + [x[-1] + 1],
                    [
                        list(range(*tuple(int(y) for y in x.split("-"))))
                        for x in room_ranges.split(",")
                    ],
                ),
            )
        ]
        scenes = ",".join(floor_plans)

    if verbose:
        print(scenes)

    env = dict(BUILD_SCENES=scenes)

    # https://forum.unity.com/threads/cannot-build-for-webgl-in-unity-system-dllnotfoundexception.1254429/
    # without setting this environment variable the error mentioned in the thread will get thrown
    os.environ["EMSDK_PYTHON"] = "/usr/bin/python3"

    if crowdsource_build:
        env["DEFINES"] = "CROWDSOURCE_TASK"
    if _build("unity", arch, directory, build_name, env=env):
        print("Build Successful")
    else:
        print("Build Failure")

    build_path = _webgl_local_build_path(prefix, directory)
    generate_quality_settings(context)

    # the remainder of this is only used to generate scene metadata, but it
    # is not part of building webgl player
    rooms = {
        "kitchens": {"name": "Kitchens", "roomRanges": range(1, 31)},
        "livingRooms": {"name": "Living Rooms", "roomRanges": range(201, 231)},
        "bedrooms": {"name": "Bedrooms", "roomRanges": range(301, 331)},
        "bathrooms": {"name": "Bathrooms", "roomRanges": range(401, 431)},
        "foyers": {"name": "Foyers", "roomRanges": range(501, 531)},
    }

    room_type_by_id = {}
    for room_type, room_data in rooms.items():
        for room_num in room_data["roomRanges"]:
            room_id = "FloorPlan{}_physics".format(room_num)
            room_type_by_id[room_id] = {"type": room_type, "name": room_data["name"]}

    scene_metadata = {}
    for scene_name in scenes.split(","):
        if scene_name not in room_type_by_id:
            # allows for arbitrary scenes to be included dynamically
            room_type = {"type": "Other", "name": None}
        else:
            room_type = room_type_by_id[scene_name]

        if room_type["type"] not in scene_metadata:
            scene_metadata[room_type["type"]] = {
                "scenes": [],
                "name": room_type["name"],
            }

        scene_metadata[room_type["type"]]["scenes"].append(scene_name)

    if verbose:
        print(scene_metadata)

    to_content_addressable = [
        ("{}.data".format(build_name), "dataUrl"),
        ("{}.loader.js".format(build_name), "loaderUrl"),
        ("{}.wasm".format(build_name), "wasmCodeUrl"),
        ("{}.framework.js".format(build_name), "wasmFrameworkUrl"),
    ]
    if content_addressable:
        for file_name, key in to_content_addressable:
            file_to_content_addressable(
                os.path.join(build_path, "Build/{}".format(file_name)),
            )

    with open(os.path.join(build_path, "scenes.json"), "w") as f:
        f.write(json.dumps(scene_metadata, sort_keys=False, indent=4))


@task
def generate_quality_settings(ctx):
    import yaml

    class YamlUnity3dTag(yaml.SafeLoader):
        def let_through(self, node):
            return self.construct_mapping(node)

    YamlUnity3dTag.add_constructor(
        "tag:unity3d.com,2011:47", YamlUnity3dTag.let_through
    )

    qs = yaml.load(
        open("unity/ProjectSettings/QualitySettings.asset").read(),
        Loader=YamlUnity3dTag,
    )

    quality_settings = {}
    default = "Ultra"
    for i, q in enumerate(qs["QualitySettings"]["m_QualitySettings"]):
        quality_settings[q["name"]] = i

    assert default in quality_settings

    with open("ai2thor/_quality_settings.py", "w") as f:
        f.write("# GENERATED FILE - DO NOT EDIT\n")
        f.write("DEFAULT_QUALITY = '%s'\n" % default)
        f.write("QUALITY_SETTINGS = " + pprint.pformat(quality_settings))


def git_commit_comment():
    comment = (
        subprocess.check_output("git log -n 1 --format=%B", shell=True)
        .decode("utf8")
        .strip()
    )

    return comment


def git_commit_id():
    commit_id = (
        subprocess.check_output("git log -n 1 --format=%H", shell=True)
        .decode("ascii")
        .strip()
    )

    return commit_id


@task
def deploy_pip(context):
    if "TWINE_PASSWORD" not in os.environ:
        raise Exception("Twine token not specified in environment")
    subprocess.check_call("twine upload -u __token__ dist/*", shell=True)


@task
def push_pip_commit(context):
    import glob

    commit_id = git_commit_id()
    s3 = boto3.resource("s3")
    for g in glob.glob("dist/ai2thor-0+%s*" % commit_id):
        acl = "public-read"
        pip_name = os.path.basename(g)
        logger.info("pushing pip file %s" % g)
        with open(g, "rb") as f:
            s3.Object(
                ai2thor.build.PYPI_S3_BUCKET, os.path.join("ai2thor", pip_name)
            ).put(Body=f, ACL=acl)


@task
def build_pip_commit(context):

    commit_id = git_commit_id()

    if os.path.isdir("dist"):
        shutil.rmtree("dist")

    generate_quality_settings(context)

    # must use this form to create valid PEP440 version specifier
    version = "0+" + commit_id

    with open("ai2thor/_builds.py", "w") as fi:
        fi.write("# GENERATED FILE - DO NOT EDIT\n")
        fi.write("COMMIT_ID = '%s'\n" % commit_id)

    with open("ai2thor/_version.py", "w") as fi:
        fi.write("# Copyright Allen Institute for Artificial Intelligence 2021\n")
        fi.write("# GENERATED FILE - DO NOT EDIT\n")
        fi.write("__version__ = '%s'\n" % (version))

    subprocess.check_call("python setup.py clean --all", shell=True)
    subprocess.check_call("python setup.py sdist bdist_wheel --universal", shell=True)


@task
def build_pip(context, version):
    import xml.etree.ElementTree as ET
    import requests

    res = requests.get("https://pypi.org/rss/project/ai2thor/releases.xml")

    res.raise_for_status()

    root = ET.fromstring(res.content)
    latest_version = None

    for title in root.findall("./channel/item/title"):
        latest_version = title.text
        break

    # make sure that the tag is on this commit
    commit_tags = (
        subprocess.check_output("git tag --points-at", shell=True)
        .decode("ascii")
        .strip()
        .split("\n")
    )

    if version not in commit_tags:
        raise Exception("tag %s is not on current commit" % version)

    commit_id = git_commit_id()
    res = requests.get("https://api.github.com/repos/allenai/ai2thor/commits?sha=main")
    res.raise_for_status()

    if commit_id not in map(lambda c: c["sha"], res.json()):
        raise Exception("tag %s is not off the main branch" % version)

    if not re.match(r"^[0-9]{1,3}\.+[0-9]{1,3}\.[0-9]{1,3}$", version):
        raise Exception("invalid version: %s" % version)

    for plat in ai2thor.build.AUTO_BUILD_PLATFORMS:
        commit_build = ai2thor.build.Build(plat, commit_id, False)
        if not commit_build.exists():
            raise Exception("Build does not exist for %s/%s" % (commit_id, plat.name()))

    current_maj, current_min, current_sub = list(map(int, latest_version.split(".")))
    next_maj, next_min, next_sub = list(map(int, version.split(".")))

    if (
        (next_maj == current_maj + 1)
        or (next_maj == current_maj and next_min == current_min + 1)
        or (
            next_maj == current_maj
            and next_min == current_min
            and next_sub >= current_sub + 1
        )
    ):

        if os.path.isdir("dist"):
            shutil.rmtree("dist")

        generate_quality_settings(context)

        with open("ai2thor/_builds.py", "w") as fi:
            fi.write("# GENERATED FILE - DO NOT EDIT\n")
            fi.write("COMMIT_ID = '%s'\n" % commit_id)

        with open("ai2thor/_version.py", "w") as fi:
            fi.write("# Copyright Allen Institute for Artificial Intelligence 2021\n")
            fi.write("# GENERATED FILE - DO NOT EDIT\n")
            fi.write("__version__ = '%s'\n" % (version))

        subprocess.check_call("python setup.py clean --all", shell=True)
        subprocess.check_call(
            "python setup.py sdist bdist_wheel --universal", shell=True
        )

    else:
        raise Exception(
            "Invalid version increment: new version=%s,current version=%s; must increment the major, minor or patch by only 1"
            % (version, latest_version)
        )


@task
def fetch_source_textures(context):
    import ai2thor.downloader

    zip_data = ai2thor.downloader.download(
        "http://s3-us-west-2.amazonaws.com/ai2-thor/assets/source-textures.zip",
        "source-textures",
        "75476d60a05747873f1173ba2e1dbe3686500f63bcde3fc3b010eea45fa58de7",
    )

    z = zipfile.ZipFile(io.BytesIO(zip_data))
    z.extractall(os.getcwd())


def build_log_push(build_info, include_private_scenes):
    with open(build_info["log"]) as f:
        build_log = f.read() + "\n" + build_info.get("build_exception", "")

    build_log_key = "builds/" + build_info["log"]
    s3 = boto3.resource("s3")

    bucket = ai2thor.build.PUBLIC_S3_BUCKET
    acl = "public-read"

    if include_private_scenes:
        bucket = ai2thor.build.PRIVATE_S3_BUCKET
        acl = "private"

    s3.Object(bucket, build_log_key).put(
        Body=build_log, ACL=acl, ContentType="text/plain"
    )


def archive_push(unity_path, build_path, build_dir, build_info, include_private_scenes):
    threading.current_thread().success = False
    archive_name = os.path.join(unity_path, build_path)
    zip_buf = io.BytesIO()
    # Unity build is done with CompressWithLz4. Zip with compresslevel=1
    # results in smaller builds than Uncompressed Unity + zip comprseslevel=6 (default)
    logger.info("building zip archive  %s %s" % (archive_name, os.path.join(unity_path, build_dir)))
    zipf = zipfile.ZipFile(zip_buf, "w", zipfile.ZIP_DEFLATED, compresslevel=1)
    add_files(zipf, os.path.join(unity_path, build_dir), exclude_ext=('.debug',))
    zipf.close()
    zip_buf.seek(0)
    zip_data = zip_buf.read()

    logger.info("generated zip archive %s %s" % (archive_name, len(zip_data)))
    push_build(archive_name, zip_data, include_private_scenes)
    build_log_push(build_info, include_private_scenes)
    print("Build successful")
    threading.current_thread().success = True


@task
def pre_test(context):
    import ai2thor.controller

    c = ai2thor.controller.Controller()
    os.makedirs("unity/builds/%s" % c.build_name())
    shutil.move(
        os.path.join("unity", "builds", c.build_name() + ".app"),
        "unity/builds/%s" % c.build_name(),
    )


def clean():
    import scripts.update_private

    # a deploy key is used on the build server and an .ssh/config entry has been added
    # to point to the deploy key caclled ai2thor-private-github
    scripts.update_private.private_repo_url = (
        "git@ai2thor-private-github:allenai/ai2thor-private.git"
    )
    subprocess.check_call("git reset --hard", shell=True)
    subprocess.check_call("git clean -f -d -x", shell=True)
    shutil.rmtree("unity/builds", ignore_errors=True)
    shutil.rmtree(scripts.update_private.private_dir, ignore_errors=True)
    scripts.update_private.checkout_branch()


def ci_prune_cache(cache_dir):
    entries = {}
    for e in os.scandir(cache_dir):
        if os.path.isdir(e.path):
            mtime = os.stat(e.path).st_mtime
            entries[e.path] = mtime

    # keeping the most recent 60 entries (this keeps the cache around 300GB-500GB)
    sorted_paths = sorted(entries.keys(), key=lambda x: entries[x])[:-60]
    for path in sorted_paths:
        if os.path.basename(path) != "main":
            logger.info("pruning cache directory: %s" % path)
            shutil.rmtree(path)


def link_build_cache(root_dir, arch, branch):
    library_path = os.path.join(root_dir, "unity", "Library")
    logger.info("linking build cache for %s" % branch)

    if os.path.lexists(library_path):
        os.unlink(library_path)

    # this takes takes care of branches with '/' in it
    # to avoid implicitly creating directories under the cache dir
    encoded_branch = re.sub(r"[^a-zA-Z0-9_\-.]", "_", re.sub("_", "__", branch))

    cache_base_dir = os.path.join(os.environ["HOME"], "cache")
    os.makedirs(cache_base_dir, exist_ok=True)

    ci_prune_cache(cache_base_dir)

    main_cache_dir = os.path.join(cache_base_dir, "main", arch)
    branch_cache_dir = os.path.join(cache_base_dir, encoded_branch, arch)
    # use the main cache as a starting point to avoid
    # having to re-import all assets, which can take up to 1 hour
    if not os.path.exists(branch_cache_dir) and os.path.exists(main_cache_dir):
        logger.info("copying main cache for %s" % encoded_branch)

        os.makedirs(os.path.dirname(branch_cache_dir), exist_ok=True)

        # -c uses MacOS clonefile
        subprocess.check_call(
            "cp -a -c %s %s" % (main_cache_dir, branch_cache_dir), shell=True
        )
        logger.info("copying main cache complete for %s" % encoded_branch)

    branch_library_cache_dir = os.path.join(branch_cache_dir, "Library")
    os.makedirs(branch_library_cache_dir, exist_ok=True)
    os.symlink(branch_library_cache_dir, library_path)
    # update atime/mtime to simplify cache pruning
    os.utime(os.path.join(cache_base_dir, encoded_branch))


def travis_build(build_id):
    import requests

    res = requests.get(
        "https://api.travis-ci.com/build/%s" % build_id,
        headers={
            "Accept": "application/json",
            "Content-Type": "application/json",
            "Travis-API-Version": "3",
        },
    )

    res.raise_for_status()

    return res.json()


def pending_travis_build():
    import requests

    res = requests.get(
        "https://api.travis-ci.com/repo/3459357/builds?include=build.id%2Cbuild.commit%2Cbuild.branch%2Cbuild.request%2Cbuild.created_by%2Cbuild.repository&build.state=started&sort_by=started_at:desc",
        headers={
            "Accept": "application/json",
            "Content-Type": "application/json",
            "Travis-API-Version": "3",
        },
        timeout=10,
    )
    for b in res.json()["builds"]:
        tag = None
        if b["tag"]:
            tag = b["tag"]["name"]

        return {
            "branch": b["branch"]["name"],
            "commit_id": b["commit"]["sha"],
            "tag": tag,
            "id": b["id"],
        }


def pytest_s3_object(commit_id):
    s3 = boto3.resource("s3")
    pytest_key = "builds/pytest-%s.json" % commit_id

    return s3.Object(ai2thor.build.PUBLIC_S3_BUCKET, pytest_key)

def pytest_s3_general_object(commit_id, filename):
    s3 = boto3.resource("s3")
    # TODO: Create a new bucket directory for test artifacts
    pytest_key = "builds/%s-%s" % (commit_id, filename)
    return s3.Object(ai2thor.build.PUBLIC_S3_BUCKET, pytest_key)

# def pytest_s3_data_urls(commit_id):
#     test_outputfiles = sorted(
#         glob.glob("{}/*".format(TEST_OUTPUT_DIRECTORY))
#     )
#     logger.info("Getting test data in directory {}".format(os.path.join(os.getcwd(), TEST_OUTPUT_DIRECTORY)))
#     logger.info("Test output files: {}".format(", ".join(test_outputfiles)))
#     test_data_urls = []
#     for filename in test_outputfiles:
#         s3_test_out_obj = pytest_s3_general_object(commit_id, filename)
#
#         s3_pytest_url = "http://s3-us-west-2.amazonaws.com/%s/%s" % (
#             s3_test_out_obj.bucket_name,
#             s3_test_out_obj.key,
#         )
#
#         _, ext = os.path.splitext(filename)
#
#         if ext in content_types:
#             s3_test_out_obj.put(
#                 Body=s3_test_out_obj, ACL="public-read", ContentType=content_types[ext]
#             )
#             logger.info(s3_pytest_url)
#             # merged_result["stdout"] += "--- test output url: {}".format(s3_pytest_url)
#             test_data_urls.append(s3_pytest_url)
#     return test_data_urls

@task
def ci_merge_push_pytest_results(context, commit_id):

    s3_obj = pytest_s3_object(commit_id)

    s3_pytest_url = "http://s3-us-west-2.amazonaws.com/%s/%s" % (
        s3_obj.bucket_name,
        s3_obj.key,
    )
    logger.info("ci_merge_push_pytest_results pytest before url check code change logging works")
    logger.info("pytest url %s" % s3_pytest_url)
    logger.info("s3 obj is valid: {}".format(s3_obj))

    merged_result = dict(success=True, stdout="", stderr="", test_data=[])
    result_files = ["tmp/pytest_results.json", "tmp/test_utf_results.json"]

    for rf in result_files:
        with open(rf) as f:
             result = json.loads(f.read())

        merged_result["success"] &= result["success"]
        merged_result["stdout"] += result["stdout"] + "\n"
        merged_result["stderr"] += result["stderr"] + "\n"

    # merged_result["test_data"] = pytest_s3_data_urls(commit_id)

    s3_obj.put(
        Body=json.dumps(merged_result), ACL="public-read", ContentType="application/json"
    )


def ci_pytest(branch, commit_id):
    import requests
    logger.info("running pytest for %s %s" % (branch, commit_id))

    proc = subprocess.run(
        "pytest", shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE
    )

    result = dict(
        success=proc.returncode == 0,
        stdout=proc.stdout.decode("ascii"),
        stderr=proc.stderr.decode("ascii"),
    )

    with open("tmp/pytest_results.json", "w") as f:
        f.write(json.dumps(result))

    logger.info("finished pytest for %s %s" % (branch, commit_id))


@task
def ci_build(context):

    lock_f = open(os.path.join(os.environ["HOME"], ".ci-build.lock"), "w")
    arch_temp_dirs = dict()
    try:
        fcntl.flock(lock_f, fcntl.LOCK_EX | fcntl.LOCK_NB)
        build = pending_travis_build()
        skip_branches = ["vids", "video", "erick/cloudrendering", "it_vr"]
        if build and build["branch"] not in skip_branches:
            # disabling delete temporarily since it interferes with pip releases
            # pytest_s3_object(build["commit_id"]).delete()
            logger.info(
                "pending build for %s %s" % (build["branch"], build["commit_id"])
            )
            clean()
            subprocess.check_call("git fetch", shell=True)
            subprocess.check_call("git checkout %s --" % build["branch"], shell=True)
            subprocess.check_call("git checkout -qf %s" % build["commit_id"], shell=True)

            private_scene_options = [False]

            procs = []
            build_archs = ["OSXIntel64", "Linux64"]

            # CloudRendering only supported with 2020.3.25
            # should change this in the future to automatically install
            # cloudrendering engine if available
            if _unity_version() == "2020.3.25f1":
                build_archs.append("CloudRendering")

            for include_private_scenes in private_scene_options:
                for arch in build_archs:
                    logger.info(
                        "starting build for %s %s %s"
                        % (arch, build["branch"], build["commit_id"])
                    )
                    temp_dir = arch_temp_dirs[arch] = os.path.join(os.environ["HOME"], "tmp/unity-%s-%s-%s-%s" % (arch, build["commit_id"], os.getpid(), random.randint(0, 2**32 - 1)))
                    os.makedirs(temp_dir)
                    logger.info( "copying unity data to %s" % (temp_dir,))
                    # -c uses MacOS clonefile
                    subprocess.check_call("cp -a -c unity %s" % temp_dir, shell=True)
                    logger.info( "completed unity data copy to %s" % (temp_dir,))
                    rdir = os.path.join(temp_dir, "unity/builds")
                    commit_build = ai2thor.build.Build(
                        arch,
                        build["commit_id"],
                        include_private_scenes=include_private_scenes,
                        releases_dir=rdir,
                    )
                    if commit_build.exists():
                        logger.info(
                            "found build for commit %s %s" % (build["commit_id"], arch)
                        )
                        # download the build so that we can run the tests
                        if arch == "OSXIntel64":
                            commit_build.download()
                    else:
                        # this is done here so that when a tag build request arrives and the commit_id has already
                        # been built, we avoid bootstrapping the cache since we short circuited on the line above
                        link_build_cache(temp_dir, arch, build["branch"])

                        # ci_build_arch(temp_dir, arch, build["commit_id"], include_private_scenes)
                        p = multiprocessing.Process(target=ci_build_arch, args=(temp_dir, arch, build["commit_id"], include_private_scenes,))
                        active_procs = lambda x: sum([p.is_alive() for p in x])
                        started = False
                        for _ in range(200):
                            if active_procs(procs) > 0:
                                logger.info("too many active procs - waiting before start %s " % arch)
                                time.sleep(15)
                                continue
                            else:
                                logger.info("starting build process for %s " % arch)
                                started = True
                                p.start()
                                # wait for Unity to start so that it can pick up the GICache config
                                # changes
                                time.sleep(30)
                                procs.append(p)
                                break
                        if not started:
                            logger.error("could not start build for %s" % arch)

            # the UnityLockfile is used as a trigger to indicate that Unity has closed
            # the project and we can run the unit tests
            # waiting for all builds to complete before starting tests
            for arch in build_archs:
                lock_file_path = os.path.join(arch_temp_dirs[arch], "unity/Temp/UnityLockfile")
                if os.path.isfile(lock_file_path):
                    logger.info("attempting to lock %s" % lock_file_path)
                    lock_file = os.open(lock_file_path, os.O_RDWR)
                    fcntl.lockf(lock_file, fcntl.LOCK_EX)
                    fcntl.lockf(lock_file, fcntl.LOCK_UN)
                    os.close(lock_file)
                    logger.info("obtained lock on %s" % lock_file_path)

            # don't run tests for a tag since results should exist
            # for the branch commit
            if build["tag"] is None:

                # its possible that the cache doesn't get linked if the builds
                # succeeded during an earlier run
                link_build_cache(arch_temp_dirs["OSXIntel64"], "OSXIntel64", build["branch"])

                # link builds directory so pytest can run
                logger.info("current directory pre-symlink %s" % os.getcwd())
                os.symlink(os.path.join(arch_temp_dirs["OSXIntel64"], "unity/builds"), "unity/builds")
                os.makedirs('tmp', exist_ok=True)
                # using threading here instead of multiprocessing since we must use the start_method of spawn, which 
                # causes the tasks.py to get reloaded, which may be different on a branch from main
                utf_proc = threading.Thread(target=ci_test_utf, args=(build["branch"], build["commit_id"], arch_temp_dirs["OSXIntel64"]))
                utf_proc.start()
                procs.append(utf_proc)
                pytest_proc = threading.Thread(target=ci_pytest, args=(build["branch"], build["commit_id"]))
                pytest_proc.start()
                procs.append(pytest_proc)

            ## allow webgl to be force deployed with #webgl-deploy in the commit comment

            if (
                build["branch"] in ["main", "demo-updates"]
                and "#webgl-deploy" in git_commit_comment()
            ):
                ci_build_webgl(context, build["commit_id"])


            for p in procs:
                if p:
                    logger.info(
                        "joining proc %s for %s %s"
                        % (p, build["branch"], build["commit_id"])
                    )
                    p.join()

            if build["tag"] is None:
                ci_merge_push_pytest_results(context, build["commit_id"])

            # must have this after all the procs are joined
            # to avoid generating a _builds.py file that would affect pytest execution
            build_pip_commit(context)
            push_pip_commit(context)
            generate_pypi_index(context)

            # give the travis poller time to see the result
            for i in range(12):
                b = travis_build(build["id"])
                logger.info("build state for %s: %s" % (build["id"], b["state"]))

                if b["state"] != "started":
                    break
                time.sleep(10)

            logger.info("build complete %s %s" % (build["branch"], build["commit_id"]))


        fcntl.flock(lock_f, fcntl.LOCK_UN)

    except io.BlockingIOError as e:
        pass

    finally:
        for arch, temp_dir in arch_temp_dirs.items():
            logger.info("deleting temp dir %s" % temp_dir)
            shutil.rmtree(temp_dir)


    lock_f.close()


@task
def install_cloudrendering_engine(context, force=False):
    if not sys.platform.startswith("darwin"):
        raise Exception("CloudRendering Engine can only be installed on Mac")
    s3 = boto3.resource("s3")
    target_base_dir = "/Applications/Unity/Hub/Editor/{}/PlaybackEngines".format(_unity_version())
    full_dir = os.path.join(target_base_dir, "CloudRendering")
    if os.path.isdir(full_dir):
        if force:
            shutil.rmtree(full_dir)
            logger.info("CloudRendering engine already installed - removing due to force")
        else:
            logger.info("skipping installation - CloudRendering engine already installed")
            return

    print("packages/CloudRendering-%s.zip" % _unity_version())
    res = s3.Object(ai2thor.build.PRIVATE_S3_BUCKET, "packages/CloudRendering-%s.zip" % _unity_version()).get()
    data = res["Body"].read()
    z = zipfile.ZipFile(io.BytesIO(data))
    z.extractall(target_base_dir)


@task
def ci_build_webgl(context, commit_id):
    branch = "main"
    logger.info("starting auto-build webgl build deploy %s %s" % (branch, commit_id))
    # linking here in the event we didn't link above since the builds had
    # already completed. Omitting this will cause the webgl build
    # to import all assets from scratch into a new unity/Library
    arch = "WebGL"
    set_gi_cache_folder(arch)
    link_build_cache(os.getcwd(), arch, branch)
    webgl_build_deploy_demo(context, verbose=True, content_addressable=False, force=True)
    logger.info("finished webgl build deploy %s %s" % (branch, commit_id))
    update_webgl_autodeploy_commit_id(commit_id)


def set_gi_cache_folder(arch):
    gi_cache_folder = os.path.join(os.environ["HOME"], "GICache/%s" % arch)
    plist_path = os.path.join(os.environ["HOME"], "Library/Preferences/com.unity3d.UnityEditor5.x.plist")
    # done to avoid race conditions when modifying GICache from more than one build
    subprocess.check_call("plutil -replace GICacheEnableCustomPath -bool TRUE %s" % plist_path, shell=True)
    subprocess.check_call("plutil -replace GICacheFolder -string '%s' %s" % (gi_cache_folder, plist_path), shell=True)
    subprocess.check_call("plutil -replace GICacheMaximumSizeGB -integer 100 %s" % (plist_path,), shell=True)

def ci_build_arch(root_dir, arch, commit_id, include_private_scenes=False):

    os.chdir(root_dir)
    unity_path = "unity"
    build_name = ai2thor.build.build_name(arch, commit_id, include_private_scenes)
    build_dir = os.path.join("builds", build_name)
    build_path = build_dir + ".zip"
    build_info = {}

    proc = None
    try:
        build_info["log"] = "%s.log" % (build_name,)
        env = {}
        if include_private_scenes:
            env["INCLUDE_PRIVATE_SCENES"] = "true"
        set_gi_cache_folder(arch)
        _build(unity_path, arch, build_dir, build_name, env)
        logger.info("finished build for %s %s" % (arch, commit_id))

        archive_push(unity_path, build_path, build_dir, build_info, include_private_scenes)

    except Exception as e:
        print("Caught exception %s" % e)
        build_info["build_exception"] = "Exception building: %s" % e
        build_log_push(build_info, include_private_scenes)



@task
def poll_ci_build(context):
    import requests.exceptions
    import requests
    import datetime

    commit_id = git_commit_id()
    start_datetime = datetime.datetime.utcnow()

    last_emit_time = 0
    for i in range(360):
        log_exist_count = 0
        # must emit something at least once every 10 minutes
        # otherwise travis will time out the build
        if (time.time() - last_emit_time) > 120:
            print(".", end="")
            last_emit_time = time.time()

        check_platforms = ai2thor.build.AUTO_BUILD_PLATFORMS

        for plat in check_platforms:
            commit_build = ai2thor.build.Build(plat, commit_id, False)
            try:
                res = requests.head(commit_build.log_url)    
                if res.status_code == 200:
                    last_modified = datetime.datetime.strptime(res.headers['Last-Modified'], '%a, %d %b %Y %H:%M:%S GMT')
                    # if a build is restarted, a log from a previous build will exist
                    # but its last-modified date will precede the start datetime
                    if last_modified > start_datetime or commit_build.exists():
                        log_exist_count += 1

            # we observe errors when polling AWS periodically - we don't want these to stop
            # the build
            except requests.exceptions.ConnectionError as e:
                print("Caught exception %s" % e)

        if log_exist_count == len(check_platforms):
            break
        sys.stdout.flush()
        time.sleep(10)

    for plat in ai2thor.build.AUTO_BUILD_PLATFORMS:
        commit_build = ai2thor.build.Build(plat, commit_id, False)
        if not commit_build.exists():
            print("Build log url: %s" % commit_build.log_url)
            raise Exception("Failed to build %s for commit: %s " % (plat.name(), commit_id))

    pytest_missing = True
    for i in range(30):
        if (time.time() - last_emit_time) > 120:
            print(".", end="")
            last_emit_time = time.time()

        s3_obj = pytest_s3_object(commit_id)
        s3_pytest_url = "http://s3-us-west-2.amazonaws.com/%s/%s" % (
            s3_obj.bucket_name,
            s3_obj.key,
        )
        res = requests.get(s3_pytest_url)
        if res.status_code == 200:
            pytest_missing = False
            pytest_result = res.json()

            print(pytest_result["stdout"])  # print so that it appears in travis log
            print(pytest_result["stderr"])

            if "test_data" in pytest_result:
                print("Pytest url: %s" % s3_pytest_url)
                print("Data urls: ")
                print(", ".join(pytest_result["test_data"]))
            else:
                print("No test data url's in json '{}'.".format(s3_pytest_url))

            if not pytest_result["success"]:
                raise Exception("pytest failure")
            break
        time.sleep(10)

    if pytest_missing:
        raise Exception("Missing pytest output")


@task
def build(context, local=False):

    version = datetime.datetime.now().strftime("%Y%m%d%H%M")

    builds = {"Docker": {"tag": version}}
    threads = []

    for include_private_scenes in (True, False):
        for plat in ai2thor.build.AUTO_BUILD_PLATFORMS:
            env = {}
            if include_private_scenes:
                env["INCLUDE_PRIVATE_SCENES"] = "true"
            unity_path = "unity"
            build_name = ai2thor.build.build_name(plat.name(), version, include_private_scenes)
            build_dir = os.path.join("builds", build_name)
            build_path = build_dir + ".zip"
            build_info = builds[plat.name()] = {}
            build_info["log"] = "%s.log" % (build_name,)

            _build(unity_path, plat.name(), build_dir, build_name, env=env)
            t = threading.Thread(
                target=archive_push,
                args=(
                    unity_path,
                    build_path,
                    build_dir,
                    build_info,
                    include_private_scenes,
                ),
            )
            t.start()
            threads.append(t)

    # dp.join()

    # if dp.exitcode != 0:
    #    raise Exception("Exception with docker build")

    for t in threads:
        t.join()
        if not t.success:
            raise Exception("Error with thread")

    generate_quality_settings(context)


@task
def interact(
    ctx,
    scene,
    editor_mode=False,
    local_build=False,
    image=False,
    depth_image=False,
    class_image=False,
    object_image=False,
    metadata=False,
    robot=False,
    port=8200,
    host="127.0.0.1",
    image_directory=".",
    width=300,
    height=300,
    include_private_scenes=False,
    noise=False,
):
    import ai2thor.controller
    import ai2thor.robot_controller

    if image_directory != ".":
        if os.path.exists(image_directory):
            shutil.rmtree(image_directory)
        os.makedirs(image_directory)

    if not robot:
        env = ai2thor.controller.Controller(
            host=host,
            port=port,
            width=width,
            height=height,
            local_build=local_build,
            image_dir=image_directory,
            start_unity=False if editor_mode else True,
            save_image_per_frame=True,
            include_private_scenes=include_private_scenes,
            add_depth_noise=noise,
            scene=scene,
        )
    else:
        env = ai2thor.robot_controller.Controller(
            host=host,
            port=port,
            width=width,
            height=height,
            image_dir=image_directory,
            save_image_per_frame=True,
        )

    env.reset(scene)
    initialize_event = env.step(
        dict(
            action="Initialize",
            gridSize=0.25,
            renderInstanceSegmentation=object_image,
            renderSemanticSegmentation=class_image,
            renderDepthImage=depth_image,
        )
    )

    from ai2thor.interact import InteractiveControllerPrompt

    InteractiveControllerPrompt.write_image(
        initialize_event,
        image_directory,
        "_init",
        image_per_frame=True,
        semantic_segmentation_frame=class_image,
        instance_segmentation_frame=object_image,
        color_frame=image,
        depth_frame=depth_image,
        metadata=metadata,
    )

    env.interact(
        semantic_segmentation_frame=class_image,
        instance_segmentation_frame=object_image,
        depth_frame=depth_image,
        color_frame=image,
        metadata=metadata,
    )
    env.stop()


@task
def get_depth(
    ctx,
    scene=None,
    image=False,
    depth_image=False,
    class_image=False,
    object_image=False,
    metadata=False,
    port=8200,
    host="127.0.0.1",
    image_directory=".",
    number=1,
    local_build=False,
    teleport=None,
    rotation=0,
):
    import ai2thor.controller
    import ai2thor.robot_controller

    if image_directory != ".":
        if os.path.exists(image_directory):
            shutil.rmtree(image_directory)
        os.makedirs(image_directory)

    if scene is None:

        env = ai2thor.robot_controller.Controller(
            host=host,
            port=port,
            width=600,
            height=600,
            image_dir=image_directory,
            save_image_per_frame=True,
        )
    else:
        env = ai2thor.controller.Controller(
            width=600, height=600, local_build=local_build
        )

    if scene is not None:
        env.reset(scene)

    initialize_event = env.step(
        dict(
            action="Initialize",
            gridSize=0.25,
            renderInstanceSegmentation=object_image,
            renderSemanticSegmentation=class_image,
            renderDepthImage=depth_image,
            agentMode="locobot",
            fieldOfView=59,
            continuous=True,
            snapToGrid=False,
        )
    )

    from ai2thor.interact import InteractiveControllerPrompt

    if scene is not None:
        teleport_arg = dict(
            action="TeleportFull", y=0.9010001, rotation=dict(x=0, y=rotation, z=0)
        )
        if teleport is not None:
            teleport = [float(pos) for pos in teleport.split(",")]

            t_size = len(teleport)
            if 1 <= t_size:
                teleport_arg["x"] = teleport[0]
            if 2 <= t_size:
                teleport_arg["z"] = teleport[1]
            if 3 <= t_size:
                teleport_arg["y"] = teleport[2]

        evt = env.step(teleport_arg)

        InteractiveControllerPrompt.write_image(
            evt,
            image_directory,
            "_{}".format("teleport"),
            image_per_frame=True,
            semantic_segmentation_frame=class_image,
            instance_segmentation_frame=object_image,
            color_frame=image,
            depth_frame=depth_image,
            metadata=metadata,
        )

    InteractiveControllerPrompt.write_image(
        initialize_event,
        image_directory,
        "_init",
        image_per_frame=True,
        semantic_segmentation_frame=class_image,
        instance_segmentation_frame=object_image,
        color_frame=image,
        depth_frame=depth_image,
        metadata=metadata,
    )

    for i in range(number):
        event = env.step(action="MoveAhead", moveMagnitude=0.0)

        InteractiveControllerPrompt.write_image(
            event,
            image_directory,
            "_{}".format(i),
            image_per_frame=True,
            semantic_segmentation_frame=class_image,
            instance_segmentation_frame=object_image,
            color_frame=image,
            depth_frame=depth_image,
            metadata=metadata,
        )
    env.stop()


@task
def inspect_depth(
    ctx, directory, all=False, indices=None, jet=False, under_score=False
):
    import numpy as np
    import cv2
    import glob

    under_prefix = "_" if under_score else ""
    regex_str = "depth{}(.*)\.png".format(under_prefix)

    def sort_key_function(name):
        split_name = name.split("/")
        x = re.search(regex_str, split_name[len(split_name) - 1]).group(1)
        try:
            val = int(x)
            return val
        except ValueError:
            return -1

    if indices is None or all:
        images = sorted(
            glob.glob("{}/depth{}*.png".format(directory, under_prefix)),
            key=sort_key_function,
        )
        print(images)
    else:
        images = ["depth{}{}.png".format(under_prefix, i) for i in indices.split(",")]

    for depth_filename in images:
        # depth_filename = os.path.join(directory, "depth_{}.png".format(index))

        split_fn = depth_filename.split("/")
        index = re.search(regex_str, split_fn[len(split_fn) - 1]).group(1)
        print("index {}".format(index))
        print("Inspecting: '{}'".format(depth_filename))
        depth_raw_filename = os.path.join(
            directory, "depth_raw{}{}.npy".format("_" if under_score else "", index)
        )
        raw_depth = np.load(depth_raw_filename)

        if jet:
            mn = np.min(raw_depth)
            mx = np.max(raw_depth)
            print("min depth value: {}, max depth: {}".format(mn, mx))
            norm = (((raw_depth - mn).astype(np.float32) / (mx - mn)) * 255.0).astype(
                np.uint8
            )

            img = cv2.applyColorMap(norm, cv2.COLORMAP_JET)
        else:
            grayscale = (
                255.0 / raw_depth.max() * (raw_depth - raw_depth.min())
            ).astype(np.uint8)
            print("max {} min {}".format(raw_depth.max(), raw_depth.min()))
            img = grayscale

        print(raw_depth.shape)

        def inspect_pixel(event, x, y, flags, param):
            if event == cv2.EVENT_LBUTTONDOWN:
                print("Pixel at x: {}, y: {} ".format(y, x))
                print(raw_depth[y][x])

        cv2.namedWindow("image")
        cv2.setMouseCallback("image", inspect_pixel)

        cv2.imshow("image", img)
        cv2.waitKey(0)


@task
def real_2_sim(
    ctx, source_dir, index, scene, output_dir, rotation=0, local_build=False, jet=False
):
    import numpy as np
    import cv2
    from ai2thor.util.transforms import transform_real_2_sim

    depth_metadata_fn = os.path.join(source_dir, "metadata_{}.json".format(index))
    color_real_fn = os.path.join(source_dir, "color_{}.png".format(index))
    color_sim_fn = os.path.join(output_dir, "color_teleport.png".format(index))
    with open(depth_metadata_fn, "r") as f:
        metadata = json.load(f)

        pos = metadata["agent"]["position"]

        sim_pos = transform_real_2_sim(pos)

        teleport_arg = "{},{},{}".format(sim_pos["x"], sim_pos["z"], sim_pos["y"])

        print(sim_pos)
        print(teleport_arg)

        inspect_depth(ctx, source_dir, indices=index, under_score=True, jet=jet)

        get_depth(
            ctx,
            scene=scene,
            image=True,
            depth_image=True,
            class_image=False,
            object_image=False,
            metadata=True,
            image_directory=output_dir,
            number=1,
            local_build=local_build,
            teleport=teleport_arg,
            rotation=rotation,
        )

        im = cv2.imread(color_real_fn)
        cv2.imshow("color_real.png", im)

        im2 = cv2.imread(color_sim_fn)
        cv2.imshow("color_sim.png", im2)

        inspect_depth(ctx, output_dir, indices="teleport", under_score=True, jet=jet)


@task
def noise_depth(ctx, directory, show=False):
    import glob
    import cv2
    import numpy as np

    def imshow_components(labels):
        # Map component labels to hue val
        label_hue = np.uint8(179 * labels / np.max(labels))
        blank_ch = 255 * np.ones_like(label_hue)
        labeled_img = cv2.merge([label_hue, blank_ch, blank_ch])

        # cvt to BGR for display
        labeled_img = cv2.cvtColor(labeled_img, cv2.COLOR_HSV2BGR)

        # set bg label to black
        labeled_img[label_hue == 0] = 0

        if show:
            cv2.imshow("labeled.png", labeled_img)
            cv2.waitKey()

    images = glob.glob("{}/depth_*.png".format(directory))

    indices = []
    for image_file in images:
        print(image_file)

        grayscale_img = cv2.imread(image_file, 0)
        img = grayscale_img

        img_size = img.shape

        img = cv2.threshold(img, 30, 255, cv2.THRESH_BINARY_INV)[1]

        ret, labels = cv2.connectedComponents(img)
        print("Components: {}".format(ret))
        imshow_components(labels)
        print(img_size[0])

        indices_top_left = np.where(labels == labels[0][0])
        indices_top_right = np.where(labels == labels[0][img_size[1] - 1])
        indices_bottom_left = np.where(labels == labels[img_size[0] - 1][0])
        indices_bottom_right = np.where(
            labels == labels[img_size[0] - 1][img_size[1] - 1]
        )

        indices = [
            indices_top_left,
            indices_top_right,
            indices_bottom_left,
            indices_bottom_right,
        ]

        blank_image = np.zeros((300, 300, 1), np.uint8)

        blank_image.fill(255)
        blank_image[indices_top_left] = 0
        blank_image[indices_top_right] = 0
        blank_image[indices_bottom_left] = 0
        blank_image[indices_bottom_right] = 0

        if show:
            cv2.imshow("labeled.png", blank_image)
            cv2.waitKey()
        break

    compressed = []
    for indices_arr in indices:
        unique_e, counts = np.unique(indices_arr[0], return_counts=True)
        compressed.append(counts)

    np.save("depth_noise", compressed)


@task
def release(ctx):
    x = subprocess.check_output("git status --porcelain", shell=True).decode("ASCII")
    for line in x.split("\n"):
        if line.strip().startswith("??") or len(line.strip()) == 0:
            continue
        raise Exception(
            "Found locally modified changes from 'git status' - please commit and push or revert"
        )

    import ai2thor._version

    tag = "v" + ai2thor._version.__version__
    subprocess.check_call('git tag -a %s -m "release  %s"' % (tag, tag), shell=True)
    subprocess.check_call("git push origin main --tags", shell=True)
    subprocess.check_call(
        "twine upload -u ai2thor dist/ai2thor-{ver}-* dist/ai2thor-{ver}.*".format(
            ver=ai2thor._version.__version__
        ),
        shell=True,
    )


@task
def check_visible_objects_closed_receptacles(ctx, start_scene, end_scene):
    from itertools import product

    import ai2thor.controller

    controller = ai2thor.controller.BFSController()
    controller.start()
    for i in range(int(start_scene), int(end_scene)):
        print("working on floorplan %s" % i)
        controller.search_all_closed("FloorPlan%s" % i)

        visibility_object_id = None
        visibility_object_types = ["Mug", "CellPhone", "SoapBar"]
        for obj in controller.last_event.metadata["objects"]:
            if obj["pickupable"]:
                controller.step(
                    action=dict(
                        action="PickupObject",
                        objectId=obj["objectId"],
                        forceVisible=True,
                    )
                )

            if (
                visibility_object_id is None
                and obj["objectType"] in visibility_object_types
            ):
                visibility_object_id = obj["objectId"]

        if visibility_object_id is None:
            raise Exception("Couldn't get a visibility_object")

        bad_receptacles = set()
        for point in controller.grid_points:
            controller.step(
                dict(action="Teleport", x=point["x"], y=point["y"], z=point["z"]),
                raise_for_failure=True,
            )

            for rot, hor in product(controller.rotations, controller.horizons):
                event = controller.step(
                    dict(action="RotateLook", rotation=rot, horizon=hor),
                    raise_for_failure=True,
                )
                for j in event.metadata["objects"]:
                    if j["receptacle"] and j["visible"] and j["openable"]:

                        controller.step(
                            action=dict(
                                action="Replace",
                                forceVisible=True,
                                pivot=0,
                                receptacleObjectId=j["objectId"],
                                objectId=visibility_object_id,
                            )
                        )

                        replace_success = controller.last_event.metadata[
                            "lastActionSuccess"
                        ]

                        if replace_success:
                            if (
                                controller.is_object_visible(visibility_object_id)
                                and j["objectId"] not in bad_receptacles
                            ):
                                bad_receptacles.add(j["objectId"])
                                print("Got bad receptacle: %s" % j["objectId"])
                                # import cv2
                                # cv2.imshow('aoeu', controller.last_event.cv2image())
                                # cv2.waitKey(0)

                            controller.step(
                                action=dict(
                                    action="PickupObject",
                                    objectId=visibility_object_id,
                                    forceVisible=True,
                                )
                            )


@task
def benchmark(
    ctx,
    width=600,
    height=600,
    editor_mode=False,
    out="benchmark.json",
    verbose=False,
    local_build=False,
    number_samples=100,
    gridSize=0.25,
    scenes=None,
    house_json_path=None,
    filter_object_types="",
    teleport_random_before_actions=False,
    commit_id=ai2thor.build.COMMIT_ID,
    distance_visibility_scheme=False,
    title=""
):
    import ai2thor.controller
    import random
    import platform
    import time
    from functools import reduce
    from pprint import pprint

    import os
    curr = os.path.dirname(os.path.abspath(__file__))

    move_actions = ["MoveAhead", "MoveBack", "MoveLeft", "MoveRight"]
    rotate_actions = ["RotateRight", "RotateLeft"]
    look_actions = ["LookUp", "LookDown"]
    all_actions = move_actions + rotate_actions + look_actions

    def test_routine(env, test_actions, n=100):
        average_frame_time = 0
        for i in range(n):
            action = random.choice(test_actions)
            start = time.time()
            env.step(dict(action=action))
            end = time.time()
            frame_time = end - start
            average_frame_time += frame_time

        average_frame_time = average_frame_time / float(n)
        return average_frame_time

    def benchmark_actions(env, action_name, actions, n=100):
        if verbose:
            print("--- Actions {}".format(actions))
        frame_time = test_routine(env, actions)
        if verbose:
            print("{} average: {}".format(action_name, 1 / frame_time))
        return 1 / frame_time

    procedural = False
    if house_json_path:
        procedural = True

    def create_procedural_house(procedural_house_path):
        house = None
        if procedural_house_path:
            if verbose:
                print("Loading house from path: '{}'. cwd: '{}'".format(procedural_house_path, curr))
            with open(procedural_house_path, "r") as f:
                house = json.load(f)
                env.step(
                    action="CreateHouse",
                    house=house
                )

        if filter_object_types != "":
            if filter_object_types == "*":
                if verbose:
                    print("-- Filter All Objects From Metadata")
                env.step(action="SetObjectFilter", objectIds=[])
            else:
                types = filter_object_types.split(",")
                evt = env.step(action="SetObjectFilterForType", objectTypes=types)
                if verbose:
                    print("Filter action, Success: {}, error: {}".format(evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]))
        return house

    def telerport_to_random_reachable(env, house=None):

        # teleport within scene for reachable positions to work
        def centroid(poly):
            n = len(poly)
            total = reduce(lambda acc, e: {'x':acc['x']+e['x'], 'y': acc['y']+e['y'], 'z': acc['z']+e['z']}, poly, {'x':0, 'y': 2, 'z': 0})
            return {'x':total['x']/n, 'y': total['y']/n, 'z': total['z']/n}

        if procedural:
            pos = {'x':0, 'y': 2, 'z': 0}

            if house['rooms'] and len(house['rooms']) > 0 :
                poly = house['rooms'][0]['floorPolygon']
                pos = centroid(poly)

                print("poly center: {0}".format(pos))
            evt = env.step(
                dict(
                    action="TeleportFull",
                    x=pos['x'],
                    y=pos['y'],
                    z=pos['z'],
                    rotation=dict(x=0, y=0, z=0),
                    horizon=0.0,
                    standing=True,
                    forceAction=True
                )
            )
            if verbose:
                print("--Teleport, " +  " err: " + evt.metadata["errorMessage"])

        evt = env.step(action="GetReachablePositions")

        # print("After GetReachable AgentPos: {}".format(evt.metadata["agent"]["position"]))
        if verbose:
            print("-- GetReachablePositions success: {}, message: {}".format(evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]))

        reachable_pos = evt.metadata["actionReturn"]

        # print(evt.metadata["actionReturn"])
        pos = random.choice(reachable_pos)
        rot = random.choice([0, 90, 180, 270])

        evt = env.step(
            dict(
                action="TeleportFull",
                x=pos['x'],
                y=pos['y'],
                z=pos['z'],
                rotation=dict(x=0, y=rot, z=0),
                horizon=0.0,
                standing=True
            )
        )

    args = {}
    if editor_mode:
        args["port"] = 8200
        args["start_unity"] = False
    elif local_build:
        args["local_build"] = local_build
    else:
        args["commit_id"] = commit_id

    args['width'] = width
    args['height'] = height
    args['gridSize'] = gridSize
    args['snapToGrid'] = True
    args['visibilityScheme'] = 'Distance' if distance_visibility_scheme else 'Collider'

    env = ai2thor.controller.Controller(
        **args
    )

    # Kitchens:       FloorPlan1 - FloorPlan30
    # Living rooms:   FloorPlan201 - FloorPlan230
    # Bedrooms:       FloorPlan301 - FloorPlan330
    # Bathrooms:      FloorPLan401 - FloorPlan430

    room_ranges = [(1, 30), (201, 230), (301, 330), (401, 430)]
    if scenes:
        scene_list = scenes.split(",")
    else:
        scene_list = [["FloorPlan{}_physics".format(i) for i in range(room_range[0], room_range[1])] for room_range in room_ranges]

    procedural_json_filenames = None
    if house_json_path:
        scene_list = house_json_path.split(",")

    # inv_args = locals()
    # del inv_args['ctx']
    # inv_args['platform'] =platform.system()

    benchmark_map = {"scenes": {}, "controller_params": {**args}, "benchmark_params": { "platform": platform.system(), "filter_object_types": filter_object_types, "action_sample_number": number_samples}}
    if title != '':
        benchmark_map['title'] = title
    total_average_ft = 0
    scene_count = 0

    for scene in scene_list:
            scene_benchmark = {}
            if verbose:
                print("Loading scene {}".format(scene))
            if not procedural:
                 env.reset(scene)
            else:
                if verbose:
                    print("------ RESET")
                env.reset("procedural")

            # env.step(dict(action="Initialize", gridSize=0.25))

            if verbose:
                print("------ {}".format(scene))

            # initial_teleport(env)
            sample_number = number_samples
            action_tuples = [
                ("move", move_actions, sample_number),
                ("rotate", rotate_actions, sample_number),
                ("look", look_actions, sample_number),
                ("all", all_actions, sample_number),
            ]
            scene_average_fr = 0
            procedural_house_path = scene if procedural else None

            house = create_procedural_house(procedural_house_path) if procedural else None

            for action_name, actions, n in action_tuples:

                telerport_to_random_reachable(env, house)
                ft = benchmark_actions(env, action_name, actions, n)
                scene_benchmark[action_name] = ft
                scene_average_fr += ft

            scene_average_fr = scene_average_fr / float(len(action_tuples))
            total_average_ft += scene_average_fr

            if verbose:
                print("Total average frametime: {}".format(scene_average_fr))

            benchmark_map["scenes"][scene] = scene_benchmark
            scene_count += 1

    benchmark_map["average_framerate_seconds"] = total_average_ft / scene_count
    with open(out, "w") as f:
        f.write(json.dumps(benchmark_map, indent=4, sort_keys=True))

    env.stop()


def list_objects_with_metadata(bucket):
    keys = {}
    s3c = boto3.client("s3")
    continuation_token = None
    while True:
        if continuation_token:
            objects = s3c.list_objects_v2(
                Bucket=bucket, ContinuationToken=continuation_token
            )
        else:
            objects = s3c.list_objects_v2(Bucket=bucket)

        for i in objects.get("Contents", []):
            keys[i["Key"]] = i

        if "NextContinuationToken" in objects:
            continuation_token = objects["NextContinuationToken"]
        else:
            break

    return keys


def s3_etag_data(data):
    h = hashlib.md5()
    h.update(data)
    return '"' + h.hexdigest() + '"'


cache_seconds = 31536000


@task
def webgl_deploy(
    ctx,
    bucket=ai2thor.build.PUBLIC_WEBGL_S3_BUCKET,
    prefix="local",
    source_dir="builds",
    target_dir="",
    verbose=False,
    force=False,
    extensions_no_cache="",
):
    from pathlib import Path
    from os.path import isfile, join, isdir

    content_encoding = {".unityweb": "gzip"}

    bucket_name = bucket
    s3 = boto3.resource("s3")

    current_objects = list_objects_with_metadata(bucket_name)

    no_cache_extensions = {".txt", ".html", ".json", ".js"}

    no_cache_extensions.union(set(extensions_no_cache.split(",")))

    def walk_recursive(path, func, parent_dir=""):
        for file_name in os.listdir(path):
            f_path = join(path, file_name)
            relative_path = join(parent_dir, file_name)
            if isfile(f_path):
                key = Path(join(target_dir, relative_path))
                func(f_path, key.as_posix())
            elif isdir(f_path):
                walk_recursive(f_path, func, relative_path)

    def upload_file(f_path, key):
        _, ext = os.path.splitext(f_path)
        if verbose:
            print("'{}'".format(key))

        with open(f_path, "rb") as f:
            file_data = f.read()
            etag = s3_etag_data(file_data)
            kwargs = {}
            if ext in content_encoding:
                kwargs["ContentEncoding"] = content_encoding[ext]

            if (
                not force
                and key in current_objects
                and etag == current_objects[key]["ETag"]
            ):
                if verbose:
                    print("ETag match - skipping %s" % key)
                return

            if ext in content_types:
                cache = (
                    "no-cache, no-store, must-revalidate"
                    if ext in no_cache_extensions
                    else "public, max-age={}".format(cache_seconds)
                )
                now = datetime.datetime.utcnow()
                expires = (
                    now
                    if ext == ".html" or ext == ".txt"
                    else now + datetime.timedelta(seconds=cache_seconds)
                )
                s3.Object(bucket_name, key).put(
                    Body=file_data,
                    ACL="public-read",
                    ContentType=content_types[ext],
                    CacheControl=cache,
                    Expires=expires,
                    **kwargs,
                )
            else:
                if verbose:
                    print(
                        "Warning: Content type for extension '{}' not defined,"
                        " uploading with no content type".format(ext)
                    )
                s3.Object(bucket_name, key).put(Body=f.read(), ACL="public-read")

    if prefix is not None:
        build_path = _webgl_local_build_path(prefix, source_dir)
    else:
        build_path = source_dir
    if verbose:
        print("Build path: '{}'".format(build_path))
        print("Uploading...")
    walk_recursive(build_path, upload_file)


@task
def webgl_build_deploy_demo(ctx, verbose=False, force=False, content_addressable=False):
    # Main demo
    demo_selected_scene_indices = [
        1,
        3,
        7,
        29,
        30,
        204,
        209,
        221,
        224,
        227,
        301,
        302,
        308,
        326,
        330,
        401,
        403,
        411,
        422,
        430,
    ]
    scenes = ["FloorPlan{}_physics".format(x) for x in demo_selected_scene_indices]
    webgl_build(
        ctx,
        scenes=",".join(scenes),
        directory="builds/demo",
        content_addressable=content_addressable,
    )

    webgl_deploy(
        ctx, source_dir="builds/demo", target_dir="demo", verbose=verbose, force=force
    )

    if verbose:
        print("Deployed selected scenes to bucket's 'demo' directory")

    # Full framework demo
    kitchens = [f"FloorPlan{i}_physics" for i in range(1, 31)]
    living_rooms = [f"FloorPlan{200 + i}_physics" for i in range(1, 31)]
    bedrooms = [f"FloorPlan{300 + i}_physics" for i in range(1, 31)]
    bathrooms = [f"FloorPlan{400 + i}_physics" for i in range(1, 31)]
    robothor_train = [
        f"FloorPlan_Train{i}_{j}" for i in range(1, 13) for j in range(1, 6)
    ]
    robothor_val = [f"FloorPlan_Val{i}_{j}" for i in range(1, 4) for j in range(1, 6)]
    scenes = (
        kitchens + living_rooms + bedrooms + bathrooms + robothor_train + robothor_val
    )

    webgl_build(
        ctx,
        scenes=",".join(scenes),
        content_addressable=content_addressable,
    )
    webgl_deploy(ctx, verbose=verbose, force=force, target_dir="full")

    if verbose:
        print("Deployed all scenes to bucket's root.")


def current_webgl_autodeploy_commit_id():
    s3 = boto3.resource("s3")
    try:
        res = s3.Object(ai2thor.build.PUBLIC_WEBGL_S3_BUCKET, "autodeploy.json").get()
        return json.loads(res["Body"].read())["commit_id"]
    except botocore.exceptions.ClientError as e:
        if e.response["Error"]["Code"] == "NoSuchKey":
            return None
        else:
            raise e


def update_webgl_autodeploy_commit_id(commit_id):
    s3 = boto3.resource("s3")
    s3.Object(ai2thor.build.PUBLIC_WEBGL_S3_BUCKET, "autodeploy.json").put(
        Body=json.dumps(dict(timestamp=time.time(), commit_id=commit_id)),
        ContentType="application/json",
    )


@task
def webgl_deploy_all(ctx, verbose=False, individual_rooms=False):
    rooms = {
        "kitchens": (1, 30),
        "livingRooms": (201, 230),
        "bedrooms": (301, 330),
        "bathrooms": (401, 430),
        "foyers": (501, 530),
    }

    for key, room_range in rooms.items():
        range_str = "{}-{}".format(room_range[0], room_range[1])
        if verbose:
            print("Building for rooms: {}".format(range_str))

        build_dir = "builds/{}".format(key)
        if individual_rooms:
            for i in range(room_range[0], room_range[1]):
                floorPlanName = "FloorPlan{}_physics".format(i)
                target_s3_dir = "{}/{}".format(key, floorPlanName)
                build_dir = "builds/{}".format(target_s3_dir)

                webgl_build(ctx, scenes=floorPlanName, directory=build_dir)
                webgl_deploy(
                    ctx, source_dir=build_dir, target_dir=target_s3_dir, verbose=verbose
                )

        else:
            webgl_build(ctx, room_ranges=range_str, directory=build_dir)
            webgl_deploy(ctx, source_dir=build_dir, target_dir=key, verbose=verbose)


@task
def webgl_s3_deploy(
    ctx, bucket, target_dir, scenes="", verbose=False, all=False, deploy_skip=False
):
    """
    Builds and deploys a WebGL unity site
    :param context:
    :param target_dir: Target s3 bucket
    :param target_dir: Target directory in bucket
    :param scenes: String of scene numbers to include in the build as a comma separated list e.g. "4,6,230"
    :param verbose: verbose build
    :param all: overrides 'scenes' parameter and builds and deploys all separate rooms
    :param deploy_skip: Whether to skip deployment and do build only.
    :return:
    """
    rooms = {
        "kitchens": (1, 30),
        "livingRooms": (201, 230),
        "bedrooms": (301, 330),
        "bathrooms": (401, 430),
    }

    if all:
        flatten = lambda l: [item for sublist in l for item in sublist]
        room_numbers = flatten(
            [
                [i for i in range(room_range[0], room_range[1])]
                for key, room_range in rooms.items()
            ]
        )
    else:
        room_numbers = [s.strip() for s in scenes.split(",")]

    if verbose:
        print("Rooms in build: '{}'".format(room_numbers))

    for i in room_numbers:
        floor_plan_name = "FloorPlan{}_physics".format(i)
        if verbose:
            print("Building room '{}'...".format(floor_plan_name))
        target_s3_dir = "{}/{}".format(target_dir, floor_plan_name)
        build_dir = "builds/{}".format(target_s3_dir)

        webgl_build(
            ctx, scenes=floor_plan_name, directory=build_dir, crowdsource_build=True
        )
        if verbose:
            print("Deploying room '{}'...".format(floor_plan_name))
        if not deploy_skip:
            webgl_deploy(
                ctx,
                bucket=bucket,
                source_dir=build_dir,
                target_dir=target_s3_dir,
                verbose=verbose,
                extensions_no_cache=".css",
            )


@task
def webgl_site_deploy(
    context,
    template_name,
    output_dir,
    bucket,
    unity_build_dir="",
    s3_target_dir="",
    force=False,
    verbose=False,
):
    from pathlib import Path
    from os.path import isfile, join, isdir

    template_dir = Path("unity/Assets/WebGLTemplates/{}".format(template_name))

    if os.path.exists(output_dir):
        shutil.rmtree(output_dir)
    # os.mkdir(output_dir)

    ignore_func = lambda d, files: [
        f for f in files if isfile(join(d, f)) and f.endswith(".meta")
    ]

    if unity_build_dir != "":
        shutil.copytree(unity_build_dir, output_dir, ignore=ignore_func)
        # shutil.copytree(os.path.join(unity_build_dir, "Build"), os.path.join(output_dir, "Build"), ignore=ignore_func)
    else:
        shutil.copytree(template_dir, output_dir, ignore=ignore_func)

    webgl_deploy(
        context,
        bucket=bucket,
        prefix=None,
        source_dir=output_dir,
        target_dir=s3_target_dir,
        verbose=verbose,
        force=force,
        extensions_no_cache=".css",
    )


@task
def mock_client_request(context):
    import msgpack
    import numpy as np
    import requests
    import cv2

    r = requests.post(
        "http://127.0.0.1:9200/step", json=dict(action="MoveAhead", sequenceId=1)
    )
    payload = msgpack.unpackb(r.content, raw=False)
    metadata = payload["metadata"]["agents"][0]
    image = np.frombuffer(payload["frames"][0], dtype=np.uint8).reshape(
        metadata["screenHeight"], metadata["screenWidth"], 3
    )
    pprint.pprint(metadata)
    cv2.imshow("aoeu", image)
    cv2.waitKey(1000)


@task
def start_mock_real_server(context):
    import ai2thor.mock_real_server

    m = ai2thor.mock_real_server.MockServer(height=300, width=300)
    print("Started mock server on port: http://" + m.host + ":" + str(m.port))
    m.start()


@task
def create_robothor_dataset(
    context,
    local_build=False,
    editor_mode=False,
    width=300,
    height=300,
    output="robothor-dataset.json",
    intermediate_directory=".",
    visibility_distance=1.0,
    objects_filter=None,
    scene_filter=None,
    filter_file=None,
):
    """
    Creates a dataset for the robothor challenge in `intermediate_directory`
    named `robothor-dataset.json`
    """
    import ai2thor.controller
    import ai2thor.util.metrics as metrics

    scene = "FloorPlan_Train1_1"
    angle = 45
    gridSize = 0.25
    # Restrict points visibility_multiplier_filter * visibility_distance away from the target object
    visibility_multiplier_filter = 2

    scene_object_filter = {}
    if filter_file is not None:
        with open(filter_file, "r") as f:
            scene_object_filter = json.load(f)
            print("Filter:")
            pprint.pprint(scene_object_filter)

    print("Visibility distance: {}".format(visibility_distance))
    controller = ai2thor.controller.Controller(
        width=width,
        height=height,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        scene=scene,
        port=8200,
        host="127.0.0.1",
        # Unity params
        gridSize=gridSize,
        fieldOfView=60,
        rotateStepDegrees=angle,
        agentMode="bot",
        visibilityDistance=visibility_distance,
    )

    targets = [
        "Apple",
        "Baseball Bat",
        "BasketBall",
        "Bowl",
        "Garbage Can",
        "House Plant",
        "Laptop",
        "Mug",
        "Remote",
        "Spray Bottle",
        "Vase",
        "Alarm Clock",
        "Television",
        "Pillow",
    ]
    failed_points = []

    if objects_filter is not None:
        obj_filter = set([o for o in objects_filter.split(",")])
        targets = [o for o in targets if o.replace(" ", "") in obj_filter]

    desired_points = 30
    event = controller.step(
        dict(
            action="GetScenesInBuild",
        )
    )
    scenes_in_build = event.metadata["actionReturn"]
    objects_types_in_scene = set()

    def sqr_dist(a, b):
        x = a[0] - b[0]
        z = a[2] - b[2]
        return x * x + z * z

    def sqr_dist_dict(a, b):
        x = a["x"] - b["x"]
        z = a["z"] - b["z"]
        return x * x + z * z

    def get_points(contoller, object_type, scene):
        print("Getting points in scene: '{}'...: ".format(scene))
        controller.reset(scene)
        event = controller.step(
            dict(
                action="ObjectTypeToObjectIds", objectType=object_type.replace(" ", "")
            )
        )
        object_ids = event.metadata["actionReturn"]

        if object_ids is None or len(object_ids) > 1 or len(object_ids) == 0:
            print("Object type '{}' not available in scene.".format(object_type))
            return None

        objects_types_in_scene.add(object_type)
        object_id = object_ids[0]

        event_reachable = controller.step(
            dict(action="GetReachablePositions", gridSize=0.25)
        )

        target_position = controller.step(
            action="GetObjectPosition", objectId=object_id
        ).metadata["actionReturn"]

        reachable_positions = event_reachable.metadata["actionReturn"]

        reachable_pos_set = set(
            [
                (pos["x"], pos["y"], pos["z"])
                for pos in reachable_positions
                # if sqr_dist_dict(pos, target_position) >= visibility_distance * visibility_multiplier_filter
            ]
        )

        def filter_points(selected_points, point_set, minimum_distance):
            result = set()
            for selected in selected_points:
                if selected in point_set:
                    result.add(selected)
                    remove_set = set(
                        [
                            p
                            for p in point_set
                            if sqr_dist(p, selected)
                            <= minimum_distance * minimum_distance
                        ]
                    )
                    point_set = point_set.difference(remove_set)
            return result

        import random

        points = random.sample(reachable_pos_set, desired_points * 4)

        final_point_set = filter_points(points, reachable_pos_set, gridSize * 2)

        print("Total number of points: {}".format(len(final_point_set)))

        print("Id {}".format(event.metadata["actionReturn"]))

        point_objects = []

        eps = 0.0001
        counter = 0
        for (x, y, z) in final_point_set:
            possible_orientations = [0, 90, 180, 270]
            pos_unity = dict(x=x, y=y, z=z)
            try:
                path = metrics.get_shortest_path_to_object(
                    controller, object_id, pos_unity, {"x": 0, "y": 0, "z": 0}
                )
                minimum_path_length = metrics.path_distance(path)

                rotation_allowed = False
                while not rotation_allowed:
                    if len(possible_orientations) == 0:
                        break
                    roatation_y = random.choice(possible_orientations)
                    possible_orientations.remove(roatation_y)
                    evt = controller.step(
                        action="TeleportFull",
                        x=pos_unity["x"],
                        y=pos_unity["y"],
                        z=pos_unity["z"],
                        rotation=dict(x=0, y=roatation_y, z=0),
                    )
                    rotation_allowed = evt.metadata["lastActionSuccess"]
                    if not evt.metadata["lastActionSuccess"]:
                        print(evt.metadata["errorMessage"])
                        print(
                            "--------- Rotation not allowed! for pos {} rot {} ".format(
                                pos_unity, roatation_y
                            )
                        )

                if minimum_path_length > eps and rotation_allowed:
                    m = re.search("FloorPlan_([a-zA-Z\-]*)([0-9]+)_([0-9]+)", scene)
                    point_id = "{}_{}_{}_{}_{}".format(
                        m.group(1), m.group(2), m.group(3), object_type, counter
                    )
                    point_objects.append(
                        {
                            "id": point_id,
                            "scene": scene,
                            "object_type": object_type,
                            "object_id": object_id,
                            "target_position": target_position,
                            "initial_position": pos_unity,
                            "initial_orientation": roatation_y,
                            "shortest_path": path,
                            "shortest_path_length": minimum_path_length,
                        }
                    )
                    counter += 1

            except ValueError:
                print("-----Invalid path discarding point...")
                failed_points.append(
                    {
                        "scene": scene,
                        "object_type": object_type,
                        "object_id": object_id,
                        "target_position": target_position,
                        "initial_position": pos_unity,
                    }
                )

        sorted_objs = sorted(point_objects, key=lambda m: m["shortest_path_length"])
        third = int(len(sorted_objs) / 3.0)

        for i, obj in enumerate(sorted_objs):
            if i < third:
                level = "easy"
            elif i < 2 * third:
                level = "medium"
            else:
                level = "hard"

            sorted_objs[i]["difficulty"] = level

        return sorted_objs

    dataset = {}
    dataset_flat = []

    if intermediate_directory is not None:
        if intermediate_directory != ".":
            if os.path.exists(intermediate_directory):
                shutil.rmtree(intermediate_directory)
            os.makedirs(intermediate_directory)

    def key_sort_func(scene_name):
        m = re.search("FloorPlan_([a-zA-Z\-]*)([0-9]+)_([0-9]+)", scene_name)
        return m.group(1), int(m.group(2)), int(m.group(3))

    scenes = sorted(
        [scene for scene in scenes_in_build if "physics" not in scene],
        key=key_sort_func,
    )

    if scene_filter is not None:
        scene_filter_set = set(scene_filter.split(","))
        scenes = [s for s in scenes if s in scene_filter_set]

    print("Sorted scenes: {}".format(scenes))
    for scene in scenes:
        dataset[scene] = {}
        dataset["object_types"] = targets
        objects = []
        for objectType in targets:

            if filter_file is None or (
                objectType in scene_object_filter
                and scene in scene_object_filter[objectType]
            ):
                dataset[scene][objectType] = []
                obj = get_points(controller, objectType, scene)
                if obj is not None:

                    objects = objects + obj

        dataset_flat = dataset_flat + objects
        if intermediate_directory != ".":
            with open(
                os.path.join(intermediate_directory, "{}.json".format(scene)), "w"
            ) as f:
                json.dump(objects, f, indent=4)

    with open(os.path.join(intermediate_directory, output), "w") as f:
        json.dump(dataset_flat, f, indent=4)
    print("Object types in scene union: {}".format(objects_types_in_scene))
    print("Total unique objects: {}".format(len(objects_types_in_scene)))
    print("Total scenes: {}".format(len(scenes)))
    print("Total datapoints: {}".format(len(dataset_flat)))

    print(failed_points)
    with open(os.path.join(intermediate_directory, "failed.json"), "w") as f:
        json.dump(failed_points, f, indent=4)


@task
def shortest_path_to_object(
    context,
    scene,
    object,
    x,
    z,
    y=0.9103442,
    rotation=0,
    editor_mode=False,
    local_build=False,
    visibility_distance=1.0,
    grid_size=0.25,
):
    p = dict(x=x, y=y, z=z)

    import ai2thor.controller
    import ai2thor.util.metrics as metrics

    angle = 45
    gridSize = grid_size
    controller = ai2thor.controller.Controller(
        width=300,
        height=300,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        scene=scene,
        port=8200,
        host="127.0.0.1",
        # Unity params
        gridSize=gridSize,
        fieldOfView=60,
        rotateStepDegrees=angle,
        agentMode="bot",
        visibilityDistance=visibility_distance,
    )
    path = metrics.get_shortest_path_to_object_type(
        controller, object, p, {"x": 0, "y": 0, "z": 0}
    )
    minimum_path_length = metrics.path_distance(path)

    print("Path: {}".format(path))
    print("Path lenght: {}".format(minimum_path_length))


@task
def filter_dataset(ctx, filename, output_filename, ids=False):
    """
    Filters objects in dataset that are not reachable in at least one of the scenes (have
    zero occurrences in the dataset)
    """

    with open(filename, "r") as f:
        obj = json.load(f)

    targets = [
        "Apple",
        "Baseball Bat",
        "BasketBall",
        "Bowl",
        "Garbage Can",
        "House Plant",
        "Laptop",
        "Mug",
        "Spray Bottle",
        "Vase",
        "Alarm Clock",
        "Television",
        "Pillow",
    ]

    counter = {}
    for f in obj:
        obj_type = f["object_type"]

        if f["scene"] not in counter:
            counter[f["scene"]] = {target: 0 for target in targets}
        scene_counter = counter[f["scene"]]
        if obj_type not in scene_counter:
            scene_counter[obj_type] = 1
        else:
            scene_counter[obj_type] += 1

    objects_with_zero = set()
    objects_with_zero_by_obj = {}
    for k, item in counter.items():
        # print("Key {} ".format(k))
        for obj_type, count in item.items():
            # print("obj {} count {}".format(obj_type, count))
            if count == 0:
                if obj_type not in objects_with_zero_by_obj:
                    objects_with_zero_by_obj[obj_type] = set()

                # print("With zero for obj: {} in scene {}".format(obj_type, k))
                objects_with_zero_by_obj[obj_type].add(k)
                objects_with_zero.add(obj_type)

    print("Objects with zero: {}".format(objects_with_zero))
    with open("with_zero.json", "w") as fw:
        dict_list = {k: list(v) for k, v in objects_with_zero_by_obj.items()}
        json.dump(dict_list, fw, sort_keys=True, indent=4)
    pprint.pprint(objects_with_zero_by_obj)
    filtered = [o for o in obj if o["object_type"] not in objects_with_zero]
    counter = 0
    current_scene = ""
    current_object_type = ""

    for i, o in enumerate(filtered):
        if current_scene != o["scene"] or current_object_type != o["object_type"]:
            counter = 0
            current_scene = o["scene"]
            current_object_type = o["object_type"]

        m = re.search("FloorPlan_([a-zA-Z\-]*)([0-9]+)_([0-9]+)", o["scene"])
        point_id = "{}_{}_{}_{}_{}".format(
            m.group(1), m.group(2), m.group(3), o["object_type"], counter
        )
        counter += 1

        o["id"] = point_id
    with open(output_filename, "w") as f:
        json.dump(filtered, f, indent=4)


@task
def fix_dataset_object_types(
    ctx, input_file, output_file, editor_mode=False, local_build=False
):
    import ai2thor.controller

    with open(input_file, "r") as f:
        obj = json.load(f)
        scene = "FloorPlan_Train1_1"
        angle = 45
        gridSize = 0.25
        controller = ai2thor.controller.Controller(
            width=300,
            height=300,
            local_build=local_build,
            start_unity=False if editor_mode else True,
            scene=scene,
            port=8200,
            host="127.0.0.1",
            # Unity params
            gridSize=gridSize,
            fieldOfView=60,
            rotateStepDegrees=angle,
            agentMode="bot",
            visibilityDistance=1,
        )
        current_scene = None
        object_map = {}
        for i, point in enumerate(obj):
            if current_scene != point["scene"]:
                print("Fixing for scene '{}'...".format(point["scene"]))
                controller.reset(point["scene"])
                current_scene = point["scene"]
                object_map = {
                    o["objectType"].lower(): {
                        "id": o["objectId"],
                        "type": o["objectType"],
                    }
                    for o in controller.last_event.metadata["objects"]
                }
            key = point["object_type"].replace(" ", "").lower()
            point["object_id"] = object_map[key]["id"]
            point["object_type"] = object_map[key]["type"]

        with open(output_file, "w") as fw:
            json.dump(obj, fw, indent=True)


@task
def test_dataset(
    ctx, filename, scenes=None, objects=None, editor_mode=False, local_build=False
):
    import ai2thor.controller
    import ai2thor.util.metrics as metrics

    scene = "FloorPlan_Train1_1" if scenes is None else scenes.split(",")[0]
    controller = ai2thor.controller.Controller(
        width=300,
        height=300,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        scene=scene,
        port=8200,
        host="127.0.0.1",
        # Unity params
        gridSize=0.25,
        fieldOfView=60,
        rotateStepDegrees=45,
        agentMode="bot",
        visibilityDistance=1,
    )
    with open(filename, "r") as f:
        dataset = json.load(f)
        filtered_dataset = dataset
        if scenes is not None:
            scene_set = set(scenes.split(","))
            print("Filtering {}".format(scene_set))
            filtered_dataset = [d for d in dataset if d["scene"] in scene_set]
        if objects is not None:
            object_set = set(objects.split(","))
            print("Filtering {}".format(object_set))
            filtered_dataset = [
                d for d in filtered_dataset if d["object_type"] in object_set
            ]
        current_scene = None
        current_object = None
        point_counter = 0
        print(len(filtered_dataset))
        for point in filtered_dataset:
            if current_scene != point["scene"]:
                current_scene = point["scene"]
                print("Testing for scene '{}'...".format(current_scene))
            if current_object != point["object_type"]:
                current_object = point["object_type"]
                point_counter = 0
                print("    Object '{}'...".format(current_object))
            try:
                path = metrics.get_shortest_path_to_object_type(
                    controller,
                    point["object_type"],
                    point["initial_position"],
                    {"x": 0, "y": point["initial_orientation"], "z": 0},
                )
                path_dist = metrics.path_distance(path)
                point_counter += 1

                print("        Total points: {}".format(point_counter))

                print(path_dist)
            except ValueError:
                print("Cannot find path from point")


@task
def visualize_shortest_paths(
    ctx,
    dataset_path,
    width=600,
    height=300,
    editor_mode=False,
    local_build=False,
    scenes=None,
    gridSize=0.25,
    output_dir=".",
    object_types=None,
):
    angle = 45
    import ai2thor.controller
    from PIL import Image

    controller = ai2thor.controller.Controller(
        width=width,
        height=height,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        port=8200,
        host="127.0.0.1",
        # Unity params
        gridSize=gridSize,
        fieldOfView=60,
        rotateStepDegrees=angle,
        agentMode="bot",
        visibilityDistance=1,
    )
    if output_dir != "." and os.path.exists(output_dir):
        shutil.rmtree(output_dir)
    if output_dir != ".":
        os.mkdir(output_dir)
    evt = controller.step(
        action="AddThirdPartyCamera",
        rotation=dict(x=90, y=0, z=0),
        position=dict(x=5.40, y=3.25, z=-3.0),
        fieldOfView=2.25,
        orthographic=True,
    )

    evt = controller.step(action="SetTopLevelView", topView=True)
    evt = controller.step(action="ToggleMapView")

    # im = Image.fromarray(evt.third_party_camera_frames[0])
    # im.save(os.path.join(output_dir, "top_view.jpg"))

    with open(dataset_path, "r") as f:
        dataset = json.load(f)

        dataset_filtered = dataset
        if scenes is not None:
            scene_f_set = set(scenes.split(","))
            dataset_filtered = [d for d in dataset if d["scene"] in scene_f_set]
        if object_types is not None:
            object_f_set = set(object_types.split(","))
            dataset_filtered = [
                d for d in dataset_filtered if d["object_type"] in object_f_set
            ]
        print("Running for {} points...".format(len(dataset_filtered)))

        index = 0
        print(index)
        print(len(dataset_filtered))
        datapoint = dataset_filtered[index]
        current_scene = datapoint["scene"]
        current_object = datapoint["object_type"]
        failed = {}
        while index < len(dataset_filtered):
            previous_index = index
            controller.reset(current_scene)
            while (
                current_scene == datapoint["scene"]
                and current_object == datapoint["object_type"]
            ):
                index += 1
                if index > len(dataset_filtered) - 1:
                    break
                datapoint = dataset_filtered[index]

            current_scene = datapoint["scene"]
            current_object = datapoint["object_type"]

            key = "{}_{}".format(current_scene, current_object)

            failed[key] = []

            print(
                "Points for '{}' in scene '{}'...".format(current_object, current_scene)
            )
            evt = controller.step(
                action="AddThirdPartyCamera",
                rotation=dict(x=90, y=0, z=0),
                position=dict(x=5.40, y=3.25, z=-3.0),
                fieldOfView=2.25,
                orthographic=True,
            )

            sc = dataset_filtered[previous_index]["scene"]
            obj_type = dataset_filtered[previous_index]["object_type"]
            positions = [
                d["initial_position"] for d in dataset_filtered[previous_index:index]
            ]
            # print("{} : {} : {}".format(sc, obj_type, positions))
            evt = controller.step(
                action="VisualizeShortestPaths",
                objectType=obj_type,
                positions=positions,
                grid=True,
            )
            im = Image.fromarray(evt.third_party_camera_frames[0])
            im.save(os.path.join(output_dir, "{}-{}.jpg".format(sc, obj_type)))

            # print("Retur {}, {} ".format(evt.metadata['actionReturn'], evt.metadata['lastActionSuccess']))
            # print(evt.metadata['errorMessage'])
            failed[key] = [
                positions[i]
                for i, success in enumerate(evt.metadata["actionReturn"])
                if not success
            ]

        pprint.pprint(failed)


@task
def fill_in_dataset(
    ctx,
    dataset_dir,
    dataset_filename,
    filter_filename,
    intermediate_dir,
    output_filename="filled.json",
    local_build=False,
    editor_mode=False,
    visibility_distance=1.0,
):
    import glob
    import ai2thor.controller

    dataset_path = os.path.join(dataset_dir, dataset_filename)

    def key_sort_func(scene_name):
        m = re.search("FloorPlan_([a-zA-Z\-]*)([0-9]+)_([0-9]+)", scene_name)
        return m.group(1), int(m.group(2)), int(m.group(3))

    targets = [
        "Apple",
        "Baseball Bat",
        "Basketball",
        "Bowl",
        "Garbage Can",
        "House Plant",
        "Laptop",
        "Mug",
        "Remote",
        "Spray Bottle",
        "Vase",
        "Alarm Clock",
        "Television",
        "Pillow",
    ]

    controller = ai2thor.controller.Controller(
        width=300,
        height=300,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        port=8200,
        host="127.0.0.1",
        # Unity params
        gridSize=0.25,
        fieldOfView=60,
        rotateStepDegrees=45,
        agentMode="bot",
        visibilityDistance=1,
    )

    scenes = sorted(
        [scene for scene in controller._scenes_in_build if "physics" not in scene],
        key=key_sort_func,
    )

    missing_datapoints_by_scene = {}
    partial_dataset_by_scene = {}
    for scene in scenes:
        missing_datapoints_by_scene[scene] = []
        partial_dataset_by_scene[scene] = []

    with open(dataset_path, "r") as f:
        create_dataset(
            ctx,
            local_build=local_build,
            editor_mode=editor_mode,
            output=output_filename,
            intermediate_directory=intermediate_dir,
            visibility_distance=visibility_distance,
        )

        for datapoint in filter_dataset:
            missing_datapoints_by_scene[datapoint["scene"]].append(datapoint)

        partial_dataset_filenames = sorted(
            glob.glob("{}/FloorPlan_*.png".format(dataset_dir))
        )
        print("Datas")

        difficulty_order_map = {"easy": 0, "medium": 1, "hard": 2}

        for d_filename in partial_dataset_filenames:
            with open(d_filename, "r") as fp:
                partial_dataset = json.load(fp)
                partial_dataset[0]["scene"] = partial_dataset

        final_dataset = []
        for scene in scenes:
            for object_type in targets:
                arr = [
                    p for p in partial_dataset[scene] if p["object_type"] == object_type
                ] + [
                    p
                    for p in missing_datapoints_by_scene[scene]
                    if p["object_type"] == object_type
                ]
                final_dataset = final_dataset + sorted(
                    arr,
                    key=lambda p: (
                        p["object_type"],
                        difficulty_order_map[p["difficulty"]],
                    ),
                )


@task
def test_teleport(ctx, editor_mode=False, local_build=False):
    import ai2thor.controller
    import time

    controller = ai2thor.controller.Controller(
        rotateStepDegrees=30,
        visibilityDistance=1.0,
        gridSize=0.25,
        port=8200,
        host="127.0.0.1",
        local_build=local_build,
        start_unity=False if editor_mode else True,
        agentType="stochastic",
        continuousMode=True,
        continuous=False,
        snapToGrid=False,
        agentMode="locobot",
        scene="FloorPlan_Train1_2",
        width=640,
        height=480,
        continus=True,
    )

    controller.step(action="GetReachablePositions", gridSize=0.25)
    params = {
        "x": 8.0,
        "y": 0.924999952,
        "z": -1.75,
        "rotation": {"x": 0.0, "y": 240.0, "z": 0.0},
        "horizon": 330.0,
    }
    evt = controller.step(action="TeleportFull", **params)

    print("New pos: {}".format(evt.metadata["agent"]["position"]))


@task
def resort_dataset(ctx, dataset_path, output_path, editor_mode=False, local_build=True):

    with open(dataset_path, "r") as f:
        dataset = json.load(f)

    index = 0
    previous_index = 0
    datapoint = dataset[index]
    current_scene = datapoint["scene"]
    current_object = datapoint["object_type"]
    # controller.reset(current_scene)
    sum_t = 0
    new_dataset = []
    while index < len(dataset):
        previous_index = index
        while (
            current_scene == datapoint["scene"]
            and current_object == datapoint["object_type"]
        ):
            index += 1
            if index > len(dataset) - 1:
                break
            datapoint = dataset[index]

        current_scene = datapoint["scene"]
        current_object = datapoint["object_type"]

        print("Scene '{}'...".format(current_scene))
        sorted_datapoints = sorted(
            dataset[previous_index:index], key=lambda dp: dp["shortest_path_length"]
        )
        third = int(len(sorted_datapoints) / 3.0)
        for i, obj in enumerate(sorted_datapoints):
            if i < third:
                level = "easy"
            elif i < 2 * third:
                level = "medium"
            else:
                level = "hard"
            sorted_datapoints[i]["difficulty"] = level
            m = re.search("FloorPlan_([a-zA-Z\-]*)([0-9]+)_([0-9]+)", obj["scene"])
            point_id = "{}_{}_{}_{}_{}".format(
                m.group(1), m.group(2), m.group(3), obj["object_type"], i
            )
            sorted_datapoints[i]["id"] = point_id
            sorted_datapoints[i]["difficulty"] = level
        new_dataset = new_dataset + sorted_datapoints
        sum_t += len(sorted_datapoints)

    print("original len: {}, new len: {}".format(len(dataset), sum_t))

    with open(output_path, "w") as fw:
        json.dump(new_dataset, fw, indent=4)


@task
def remove_dataset_spaces(ctx, dataset_dir):

    train = os.path.join(dataset_dir, "train.json")
    test = os.path.join(dataset_dir, "val.json")

    with open(train, "r") as f:
        train_data = json.load(f)

    with open(test, "r") as f:
        test_data = json.load(f)

    id_set = set()
    for o in train_data:
        o["id"] = o["id"].replace(" ", "")
        id_set.add(o["id"])

    print(sorted(id_set))

    id_set = set()

    for o in test_data:
        o["id"] = o["id"].replace(" ", "")
        id_set.add(o["id"])

    print(sorted(id_set))

    with open("train.json", "w") as fw:
        json.dump(train_data, fw, indent=4, sort_keys=True)

    with open("val.json", "w") as fw:
        json.dump(test_data, fw, indent=4, sort_keys=True)


@task
def shortest_path_to_point(ctx, scene, x0, y0, z0, x1, y1, z1, editor_mode=False):
    import ai2thor.util.metrics as metrics
    import ai2thor.controller

    controller = ai2thor.controller.Controller(
        rotateStepDegrees=30,
        visibilityDistance=1.0,
        gridSize=0.25,
        port=8200,
        host="127.0.0.1",
        local_build=local_build,
        start_unity=False if editor_mode else True,
        agentType="stochastic",
        continuousMode=True,
        continuous=False,
        snapToGrid=False,
        agentMode="locobot",
        scene=scene,
        width=300,
        height=300,
        continus=True,
    )

    evt = metrics.get_shortest_path_to_point(
        controller, dict(x=x0, y=y0, z=z0), dict(x=x1, y=y1, z=z1)
    )

    print(evt.metadata["lastActionSuccess"])
    print(evt.metadata["errorMessage"])


@task
def reachable_pos(ctx, scene, editor_mode=False, local_build=False):
    import ai2thor.util.metrics as metrics
    import ai2thor.controller

    gridSize = 0.25
    controller = ai2thor.controller.Controller(
        rotateStepDegrees=30,
        visibilityDistance=1.0,
        gridSize=gridSize,
        port=8200,
        host="127.0.0.1",
        local_build=local_build,
        start_unity=False if editor_mode else True,
        agentType="stochastic",
        continuousMode=True,
        continuous=False,
        snapToGrid=False,
        agentMode="locobot",
        scene=scene,
        width=300,
        height=300,
        continus=True,
    )

    print(
        "constoller.last_action Agent Pos: {}".format(
            controller.last_event.metadata["agent"]["position"]
        )
    )

    evt = controller.step(action="GetReachablePositions", gridSize=gridSize)

    print("After GetReachable AgentPos: {}".format(evt.metadata["agent"]["position"]))

    print(evt.metadata["lastActionSuccess"])
    print(evt.metadata["errorMessage"])

    reachable_pos = evt.metadata["actionReturn"]

    print(evt.metadata["actionReturn"])

    evt = controller.step(
        dict(
            action="TeleportFull",
            x=3.0,
            y=reachable_pos[0]["y"],
            z=-1.5,
            rotation=dict(x=0, y=45.0, z=0),
            horizon=0.0,
        )
    )

    print("After teleport: {}".format(evt.metadata["agent"]["position"]))


@task
def get_physics_determinism(
    ctx, scene="FloorPlan1_physics", agent_mode="arm", n=100, samples=100
):
    import ai2thor.controller
    import random

    num_trials = n
    width = 300
    height = 300
    fov = 100

    def act(controller, actions, n):
        for i in range(n):
            action = random.choice(actions)
            controller.step(dict(action=action))

    controller = ai2thor.controller.Controller(
        local_executable_path=None,
        scene=scene,
        gridSize=0.25,
        width=width,
        height=height,
        agentMode=agent_mode,
        fieldOfView=fov,
        agentControllerType="mid-level",
        server_class=ai2thor.fifo_server.FifoServer,
        visibilityScheme="Distance",
    )

    from ai2thor.util.trials import trial_runner, ObjectPositionVarianceAverage

    move_actions = ["MoveAhead", "MoveBack", "MoveLeft", "MoveRight"]
    rotate_actions = ["RotateRight", "RotateLeft"]
    look_actions = ["LookUp", "LookDown"]
    all_actions = move_actions + rotate_actions + look_actions

    sample_number = samples
    action_tuples = [
        ("move", move_actions, sample_number),
        ("rotate", rotate_actions, sample_number),
        ("look", look_actions, sample_number),
        ("all", all_actions, sample_number),
    ]

    for action_name, actions, n in action_tuples:
        for controller, metric in trial_runner(
            controller, num_trials, ObjectPositionVarianceAverage()
        ):
            act(controller, actions, n)
        print(
            " actions: '{}', object_position_variance_average: {} ".format(
                action_name, metric
            )
        )

@task
def generate_pypi_index(context):
    s3 = boto3.resource("s3")
    root_index = """
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0//EN">
<HTML>
  <BODY>
    <a href="/ai2thor/index.html">/ai2thor/</a><br>
  </BODY>
</HTML>
"""
    s3.Object(ai2thor.build.PYPI_S3_BUCKET, "index.html").put(
        Body=root_index, ACL="public-read", ContentType="text/html"
    )
    objects = list_objects_with_metadata(ai2thor.build.PYPI_S3_BUCKET)
    links = []
    for k, v in objects.items():
        if k.split("/")[-1] != "index.html":
            links.append('<a href="/%s">/%s</a><br>' % (k, k))
    ai2thor_index = """
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0//EN">
<HTML>
  <BODY>
    %s
  </BODY>
</HTML>
""" % "\n".join(
        links
    )
    s3.Object(ai2thor.build.PYPI_S3_BUCKET, "ai2thor/index.html").put(
        Body=ai2thor_index, ACL="public-read", ContentType="text/html"
    )


def ci_test_utf(branch, commit_id, base_dir):
    logger.info(
        "running Unity Test framework testRunner for %s %s %s"
        % (branch, commit_id, base_dir)
    )

    results_path, results_logfile = test_utf(base_dir)

    class_data = generate_pytest_utf(results_path)

    test_path = "tmp/test_utf.py"
    with open(test_path, "w") as f:
        f.write("\n".join(class_data))

    proc = subprocess.run(
        "pytest %s" % test_path, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE
    )

    result = dict(
        success=proc.returncode == 0,
        stdout=proc.stdout.decode("ascii"),
        stderr=proc.stderr.decode("ascii"),
    )

    with open("tmp/test_utf_results.json", "w") as f:
        f.write(json.dumps(result))


    logger.info(
        "finished Unity Test framework runner for %s %s"
        % (branch, commit_id)
    )


@task
def format(context):
    format_py(context)
    format_cs(context)


@task
def format_cs(context):
    install_dotnet_format(context)

    # the following message will get emitted, this can safely be ignored
    # "Warnings were encountered while loading the workspace. Set the verbosity option to the 'diagnostic' level to log warnings"
    subprocess.check_call(
        ".dotnet/dotnet tool run dotnet-format unity/AI2-THOR-Base.csproj -w -s",
        shell=True,
    )


@task
def install_dotnet_format(context, force=False):
    install_dotnet(context)

    base_dir = os.path.normpath(os.path.dirname(os.path.realpath(__file__)))
    if not os.path.isfile(".config/dotnet-tools.json"):
        command = os.path.join(base_dir, ".dotnet/dotnet") + " new tool-manifest"
        subprocess.check_call(command, shell=True)

    with open(".config/dotnet-tools.json") as f:
        tools = json.loads(f.read())

    # we may want to specify a version here in the future
    if not force and "dotnet-format" in tools.get("tools", {}):
        # dotnet-format already installed
        return

    command = os.path.join(base_dir, ".dotnet/dotnet") + " tool install dotnet-format"
    subprocess.check_call(command, shell=True)


@task
def install_dotnet(context, force=False):
    import requests
    import stat

    base_dir = os.path.normpath(os.path.dirname(os.path.realpath(__file__)))
    if not force and os.path.isfile(os.path.join(base_dir, ".dotnet/dotnet")):
        # dotnet already installed
        return
    # https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script
    res = requests.get("https://dot.net/v1/dotnet-install.sh")
    res.raise_for_status()
    target = os.path.join(base_dir, "dotnet-install.sh")
    with open(target, "wb") as f:
        f.write(res.content)

    os.chmod(target, stat.S_IREAD | stat.S_IEXEC | stat.S_IWRITE)
    env = os.environ.copy()
    env["DOTNET_INSTALL_DIR"] = os.path.join(base_dir, ".dotnet")
    subprocess.check_call(target, shell=True, env=env)
    os.unlink(target)


@task
def format_py(context):

    try:
        import black
    except ImportError:
        raise Exception("black not installed - run pip install black")

    subprocess.check_call(
        "black -v -t py38 --exclude unity/ --exclude .git/ .", shell=True
    )


@task
def install_unity_hub(context, target_dir=os.path.join(os.path.expanduser("~"), "local/bin")):
    import stat
    import requests

    if not sys.platform.startswith("linux"):
        raise Exception("Installation only support for Linux")
    
    res = requests.get("https://public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage")
    res.raise_for_status()
    os.makedirs(target_dir, exist_ok=True)

    target_path = os.path.join(target_dir, "UnityHub.AppImage")

    tmp_path = target_path + ".tmp-" + str(os.getpid())
    with open(tmp_path, "wb") as f:
        f.write(res.content)

    if os.path.isfile(target_path):
        os.unlink(target_path)

    os.rename(tmp_path, target_path)
    os.chmod(target_path, stat.S_IRWXU | stat.S_IRGRP | stat.S_IXGRP  | stat.S_IROTH | stat.S_IXOTH)
    print("Installed UnityHub at %s" % target_path)


@task
def install_unity_editor(context, version=None, changeset=None):
    import yaml
    import re
    unity_hub_path = None
    if sys.platform.startswith("linux"):
        unity_hub_path = os.path.join(os.path.expanduser("~"), "local/bin/UnityHub.AppImage")
    elif sys.platform.startswith("darwin"):
        unity_hub_path = "/Applications/Unity\ Hub.app/Contents/MacOS/Unity\ Hub --"
    else:
        raise Exception("UnityHub CLI not supported")

    if version is None:
        with open("unity/ProjectSettings/ProjectVersion.txt") as pf:
            project_version = yaml.load(pf.read(), Loader=yaml.FullLoader)
        m = re.match(r'^([^\s]+)\s+\(([a-zAZ0-9]+)\)', project_version["m_EditorVersionWithRevision"])

        assert m, "Could not extract version/changeset from %s" % project_version["m_EditorVersionWithRevision"]
        version = m.group(1)
        changeset = m.group(2)
    command = "%s --headless install --version %s" % (unity_hub_path, version) 
    if changeset:
        command += " --changeset %s" % changeset

    platform_modules = dict(
        linux=["mac-mono", "linux-il2cpp", "webgl"],
        darwin=["mac-il2cpp", "linux-il2cpp", "linux-mono", "webgl"],
    )
    for m in platform_modules[sys.platform]:
        command += " -m %s" % m


    subprocess.check_call(command, shell=True)

@task
def generate_unity_alf(context):
    # generates Unity License Acitivation file for use 
    # with manual activation https://docs.unity3d.com/Manual/ManualActivationGuide.html

    alf_path = "Unity_v%s.alf" % _unity_version()
    subprocess.run("%s -batchmode -createManualActivationFile" % _unity_path(), shell=True)
    assert os.path.isfile(alf_path), "ALF not found at %s" % alf_path

    print("ALF created at %s. Activate license at: https://license.unity3d.com/manual" % alf_path)
   
@task
def activate_unity_license(context, ulf_path):

    assert os.path.isfile(ulf_path), "License file '%s' not found" % ulf_path

    subprocess.run('%s -batchmode -manualLicenseFile "%s"' % (_unity_path(), ulf_path), shell=True)

def test_utf(base_dir=None):
    """
    Generates a module named ai2thor/tests/test_utf.py with test_XYZ style methods
    that include failures (if any) extracted from the xml output
    of the Unity Test Runner
    """
    if base_dir is None:
        base_dir = os.getcwd()

    project_path = os.path.join(base_dir, "unity")
    commit_id = git_commit_id()
    test_results_path = os.path.join(project_path, "utf_testResults-%s.xml" % commit_id)
    logfile_path = os.path.join(base_dir, "thor-testResults-%s.log" % commit_id)

    command = (
        "%s -runTests -testResults %s -logFile %s -testPlatform PlayMode -projectpath %s "
        % (_unity_path(), test_results_path, logfile_path, project_path)
    )

    subprocess.call(command, shell=True, cwd=base_dir)

    return test_results_path, logfile_path


def generate_pytest_utf(test_results_path):
    import xml.etree.ElementTree as ET

    with open(test_results_path) as f:
        root = ET.fromstring(f.read())

    from collections import defaultdict

    class_tests = defaultdict(list)
    for test_case in root.findall(".//test-case"):
        # print(test_case.attrib['methodname'])
        class_tests[test_case.attrib["classname"]].append(test_case)

    class_data = []
    class_data.append(
        f"""
# GENERATED BY tasks.generate_pytest_utf - DO NOT EDIT/COMMIT
import pytest
import json
import os

def test_testresults_exist():
    test_results_path = "{test_results_path}" 
    assert os.path.isfile("{test_results_path}"), "TestResults at: {test_results_path}  do not exist"

"""
    )
    for class_name, test_cases in class_tests.items():
        test_records = []
        for test_case in test_cases:
            methodname = test_case.attrib["methodname"]
            if test_case.attrib["result"] == "Failed":
                fail_message = test_case.find("failure/message")
                stack_trace = test_case.find("failure/stack-trace")
                message = json.dumps(fail_message.text + " " + stack_trace.text)
                test_data = f"""
        def test_{methodname}(self):
            pytest.fail(json.loads(r\"\"\"
{message}
\"\"\"
            ))
    """
            else:
                test_data = f"""
        def test_{methodname}(self):
            pass
    """
            test_records.append(test_data)
        test_record_data = "    pass"
        if test_records:
            test_record_data = "\n".join(test_records)
        encoded_class_name = re.sub(
            r"[^a-zA-Z0-9_]", "_", re.sub("_", "__", class_name)
        )
        class_data.append(
            f"""
class {encoded_class_name}:
    {test_record_data}
"""
        )
    with open("ai2thor/tests/test_utf.py", "w") as f:
        f.write("\n".join(class_data))

    return class_data

@task
def create_room(ctx, file_path="unity/Assets/Resources/rooms/1.json", editor_mode=False, local_build=False):
    import ai2thor.controller
    import random
    import json
    import os
    print(os.getcwd())
    width = 300
    height = 300
    fov = 100
    n = 20
    import os
    controller = ai2thor.controller.Controller(
        local_executable_path=None,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        scene="Procedural",
        gridSize=0.25,
        width=width,
        height=height,
        fieldOfView=fov,
        agentControllerType='mid-level',
        server_class=ai2thor.fifo_server.FifoServer,
        visibilityScheme='Distance'
    )

    # print(
    #     "constoller.last_action Agent Pos: {}".format(
    #         controller.last_event.metadata["agent"]["position"]
    #     )
    # )

    # evt = controller.step(action="GetReachablePositions", gridSize=gridSize)

    # print("After GetReachable AgentPos: {}".format(evt.metadata["agent"]["position"]))
    #
    # print(evt.metadata["lastActionSuccess"])
    # print(evt.metadata["errorMessage"])
    #
    # reachable_pos = evt.metadata["actionReturn"]
    #
    # print(evt.metadata["actionReturn"])
    print(os.getcwd())
    with open(file_path, "r") as f:
        obj = json.load(f)
        walls = obj["walls"]

        evt = controller.step(
            dict(
                action="CreateRoom",
                walls=walls,
                wallHeight=2.0,
                wallMaterialId="DrywallOrange",
                floorMaterialId="DarkWoodFloors"
            )
        )

        for i in range(n):
            controller.step("MoveAhead")

@task
def test_render(ctx, editor_mode=False, local_build=False):
    import ai2thor.controller
    import cv2
    import numpy as np
    print(os.getcwd())
    width = 300
    height = 300
    fov = 45
    controller = ai2thor.controller.Controller(
        local_executable_path=None,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        scene="Procedural",
        gridSize=0.25,
        port=8200,
        width=width,
        height=height,
        fieldOfView=fov,
        agentCount=1,
        renderDepthImage=True,
        server_class=ai2thor.fifo_server.FifoServer
    )

    image_folder_path = "debug_img"
    rgb_filename = "colortest.png"
    depth_filename = "depth_rawtest.npy"

    img = cv2.imread(os.path.join(image_folder_path, rgb_filename))

    from pprint import pprint
    from ai2thor.interact import InteractiveControllerPrompt

    obj = {
      "id":"house_0",
       "layout": """
           0 0 0 0 0 0
           0 2 2 2 2 0
           0 2 2 2 2 0
           0 1 1 1 1 0
           0 1 1 1 1 0
           0 0 0 0 0 0
        """,
       "objectsLayouts":[
          """
            0 0 0 0 0 0
            0 2 2 2 2 0
            0 2 2 2 = 0
            0 1 1 1 = 0
            0 1 1 1 + 0
            0 0 0 0 0 0
          """
       ],
       "rooms":{
          "1":{
             "wallTemplate":{
                "unlit":False,
                "color":{
                   "r":1.0,
                   "g":0.0,
                   "b":0.0,
                   "a":1.0
                }
             },
             "floorTemplate":{
                "roomType":"Bedroom",
                "floorMaterial":"DarkWoodFloors",
             },
             "floorYPosition":0.0,
             "wallHeight":3.0
          },
          "2":{
             "wallTemplate":{
                "unlit":False,
                "color":{
                   "r":0.0,
                   "g":0.0,
                   "b":1.0,
                   "a":1.0
                }
             },
             "floorTemplate":{
                "roomType":"LivingRoom",
                "floorMaterial":"RedBrick"
             },
             "floorYPosition":0.0,
             "wallHeight":3.0
          }
       },
       "holes":{
          "=":{
             "room0":"1",
             "openness":1.0,
             "assetId":"Doorway_1"
          }
       },
       "objects":{
          "+":{
             "kinematic": True,
             "assetId":"Chair_007_1"
          }
       },
       "proceduralParameters":{
          "floorColliderThickness":1.0,
          "receptacleHeight":0.7,
          "skyboxId":"Sky1",
          "ceilingMaterial":"ps_mat"
       }

    }

    pprint(obj)

    template = obj

    evt = controller.step(
        action="GetHouseFromTemplate",
        template=template
    )

    print("Action success {0}, message {1}".format( evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]))
    house = evt.metadata["actionReturn"]

    controller.step(
        action="CreateHouse",
        house=house
    )

    evt = controller.step(dict(
        action="TeleportFull", x=3.0, y=0.9010001, z=1.0, rotation=dict(x=0, y=0, z=0),
        horizon=0, standing=True, forceAction=True
    ))

    cv2.namedWindow("image2")
    cv2.imshow("image2", evt.cv2img)

    if img is not None:

        print(f'img r {img[0][0][0]} g {img[0][0][1]} b {img[0][0][2]}')
        print(f'evt frame r {evt.cv2img[0][0][0]} g {evt.cv2img[0][0][1]} b {evt.cv2img[0][0][2]}')

        cv2.namedWindow("image")

        cv2.imshow("image", img)

        print(img.shape)


        print(np.allclose(evt.cv2img, img))

        raw_depth = np.load(os.path.join(image_folder_path, depth_filename))

        print(f'depth evt {evt.depth_frame.shape} compare {raw_depth.shape}')

        print(np.allclose(evt.depth_frame, raw_depth))

        dx = np.where(~np.all(evt.cv2img == img,  axis=-1))

        print(list(dx))

        img[dx] = (255, 0, 255)

        print(img[dx])

        cv2.namedWindow("image-diff")
        cv2.imshow("image-diff", img)

        print(img.shape)
        cv2.waitKey(0)


    else:
        cv2.waitKey(0)

    InteractiveControllerPrompt.write_image(
        evt,
        "debug_img",
        "test",
        depth_frame=True,
        color_frame=True
    )

@task
def create_json(ctx, file_path, output=None):
    import json
    import functools
    import itertools
    from pprint import pprint

    add = lambda x, y: x + y
    sub = lambda x, y: x - y

    def vec3(x, y, z):
        return {"x": x, "y": y, "z": z}

    def point_wise_2(v1, v2, func):
        return {k: func(v1[k], v2[k]) for k in ['x', 'y', 'z']}

    def point_wise(v1, func):
        return {k: func(v1[k]) for k in ['x', 'y', 'z']}

    def sum(vec):
        return functools.reduce(lambda a, b: a + b, vec.values())

    def sqr_dist(v1, v2):
        return sum(point_wise(point_wise_2(v1, v2, sub), lambda x: x ** 2))

    def wall_to_poly(wall):
        return [ wall['p0'], wall['p1'], point_wise_2(wall['p1'], vec3(0, wall['height'], 0), add), point_wise_2(wall['p0'], vec3(0, wall['height'], 0), add)]


    def walls_to_floor_poly(walls):
        result = []
        wall_list = list(walls)
        eps = 1e-4
        eps_sqr = eps ** 2

        result.append(walls[0]['p0'])

        while len(wall_list) != 0:
            wall = wall_list.pop(0)
            p1 = wall['p1']
            wall_list = sorted(wall_list, key=lambda w: sqr_dist(p1, w['p0']))
            if len(wall_list) != 0:
                closest = wall_list[0]
                dist = sqr_dist(p1, closest['p0'])
                if dist < eps_sqr:
                    result.append(closest['p0'])
                else:
                    return None
        return result


    with open(file_path, "r") as f:
        obj = json.load(f)
        walls = \
        [
            [
                {
                    "id": "wall_{}_{}".format(room_i, wall_indx),
                    "roomId": "room_{}".format(room_i),
                    "material": wall['materialId'],
                    "empty": wall['empty'] if 'empty' in wall else False,
                    'polygon': wall_to_poly(wall)
                } for (wall, wall_indx) in zip(room["walls"], range(0, len(room["walls"])))
            ] for (room, room_i) in zip(obj["rooms"], range(len(obj["rooms"])))
        ]


        rooms = \
        [
            {
                "id": "room_{}".format(room_i),
                "type": "",
                "floorMaterial": room['rectangleFloor']['materialId'],
                "children": [],
                "ceilings": [],
                "floorPolygon": walls_to_floor_poly(room["walls"])}
            for (room, room_i) in zip(obj["rooms"], range(len(obj["rooms"])))

        ]

        walls = list(itertools.chain(*walls))

        house = {
            'rooms': rooms,
            'walls': walls,
            'proceduralParameters': {
                'ceilingMaterial': obj['ceilingMaterialId'],
                "floorColliderThickness": 1.0,
                "receptacleHeight": 0.7,
                "skyboxId": "Sky1",
                "lights": []
            }
        }

        pprint(house)

        if output is not None:
            with open(output, "w") as fw:
                json.dump(house, fw, indent=4, sort_keys=True)


@task
def spawn_obj_test(ctx, file_path, room_id, editor_mode=False, local_build=False):
    import ai2thor.controller
    import random
    import json
    import os
    import time

    print(os.getcwd())
    width = 300
    height = 300
    fov = 100
    n = 20
    import os
    from pprint import pprint
    controller = ai2thor.controller.Controller(
        local_executable_path=None,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        scene="Procedural",
        gridSize=0.25,
        width=width,
        height=height,
        fieldOfView=fov,
        agentControllerType='mid-level',
        server_class=ai2thor.fifo_server.FifoServer,
        visibilityScheme='Distance'
    )

    # print(
    #     "constoller.last_action Agent Pos: {}".format(
    #         controller.last_event.metadata["agent"]["position"]
    #     )
    # )

    # evt = controller.step(action="GetReachablePositions", gridSize=gridSize)

    # print("After GetReachable AgentPos: {}".format(evt.metadata["agent"]["position"]))
    #
    # print(evt.metadata["lastActionSuccess"])
    # print(evt.metadata["errorMessage"])
    #
    # reachable_pos = evt.metadata["actionReturn"]
    #
    # print(evt.metadata["actionReturn"])
    print(os.getcwd())
    with open(file_path, "r") as f:
        obj = json.load(f)

        obj['walls'] = [wall for wall in obj['walls'] if wall['roomId'] == room_id]
        obj['rooms'] = [room for room in obj['rooms'] if room['id'] == room_id]
        obj['objects'] = []

        pprint(obj)
        evt = controller.step(
            dict(
                action="CreateHouseFromJson",
                house=obj
            )
        )

        evt = controller.step(dict(
            action="TeleportFull", x=4.0, y=0.9010001, z=4.0, rotation=dict(x=0, y=0, z=0),
            horizon = 30, standing = True, forceAction = True
        ))
        # dict("axis" = dict(x=0, y=1.0, z=0), "degrees": 90)

        # SpawnObjectInReceptacleRandomly(string objectId, string prefabName, string targetReceptacle, AxisAngleRotation rotation)
        evt = controller.step(dict(
            action="SpawnObjectInReceptacleRandomly",
            objectId="table_1",
            prefabName="Coffee_Table_211_1",
            targetReceptacle="Floor|+00.00|+00.00|+00.00",

            rotation=dict(axis=dict(x=0, y=1.0, z=0), degrees=90)
        ))
        print(evt.metadata['lastActionSuccess'])
        print(evt.metadata['errorMessage'])

        # this is what you need
        object_position = evt.metadata['actionReturn'];

        print(object_position)

        for i in range(n):
            controller.step("MoveAhead")
            time.sleep(0.4)
        for j in range(6):
            controller.step("RotateRight")
            time.sleep(0.7)

@task
def plot(
        ctx,
        benchamrk_filenames,
        plot_titles=None,
        x_label="Rooms",
        y_label="Actions Per Second",
        title="Procedural Benchmark",
        last_ithor=False,
        output_filename="benchmark",
        width=9.50,
        height=7.5,
        action_breakdown=False
):
    import matplotlib.pyplot as plt
    from functools import reduce
    from matplotlib.lines import Line2D

    filter = 'all'

    def get_data(benchmark, filter='all'):
        keys = list(benchmark['scenes'].keys())
        if filter is not None:
            y = [benchmark['scenes'][m][filter] for m in keys]
        else:
            y = [benchmark['scenes'][m] for m in keys]
        return keys, y

    def load_benchmark_filename(filename):
        with open(filename) as f:
            return json.load(f)

    def get_benchmark_title(benchmark, default_title=''):
        if 'title' in benchmark:
            return benchmark['title']
        else:
            return default_title

    benchmark_filenames = benchamrk_filenames.split(",")

    # markers = ["o", "*", "^", "+", "~"]
    markers = list(Line2D.markers.keys())
    # remove empty marker
    markers.pop(1)
    benchmarks = [load_benchmark_filename(filename) for filename in benchmark_filenames]

    benchmark_titles = [get_benchmark_title(b, '') for (i, b) in zip(range(0, len(benchmarks)), benchmarks)]

    if plot_titles is not None:
        titles = plot_titles.split(",")
    else:
        titles = [''] * len(benchmark_titles)

    plot_titles = [benchmark_titles[i] if title == '' else title for (i, title) in zip(range(0, len(titles)), titles)]

    filter = 'all' if not action_breakdown else None
    all_data = [get_data(b, filter) for b in benchmarks]

    import numpy as np
    if action_breakdown:
        plot_titles = reduce(list.__add__, [["{} {}".format(title, action) for action in all_data[0][1][0]] for title in plot_titles])
        all_data = reduce(list.__add__,[[(x, [y[action] for y in b]) for action in all_data[0][1][0]] for (x, b) in all_data])

    keys = [k for (k, y) in all_data]
    y = [y for (k, y) in all_data]
    min_key_number = min(keys)

    ax = plt.gca()

    plt.rcParams["figure.figsize"] = [width, height]
    plt.rcParams["figure.autolayout"] = True
    fig = plt.figure()

    for (i, (x, y)) in zip(range(0, len(all_data)), all_data):
        marker = markers[i] if i < len(markers) else "*"

        ithor_datapoint = last_ithor and i == len(all_data) - 1

        x_a =  all_data[i-1][0] if ithor_datapoint else x

        plt.plot([x_s.split('/')[-1].split("_")[0] for x_s in x_a], y, marker=marker, label=plot_titles[i])

        if ithor_datapoint:
            for j in range(len(x)):
                print(j)
                print(x[j])
                plt.annotate(x[j].split("_")[0], (j, y[j] + 0.2))

    plt.xlabel(x_label)
    plt.ylabel(y_label)

    plt.title(title)
    plt.legend()

    plt.savefig('{}.png'.format(output_filename.replace(".png", "")))
