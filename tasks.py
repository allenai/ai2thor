import os
import sys
import datetime
import json
import time
import zipfile
import threading
import hashlib
import shutil
import subprocess
import pprint
from invoke import task
import boto3
import platform
import ai2thor.build
from ai2thor.build import PUBLIC_S3_BUCKET, PRIVATE_S3_BUCKET


def add_files(zipf, start_dir):
    for root, dirs, files in os.walk(start_dir):
        for f in files:
            fn = os.path.join(root, f)
            arcname = os.path.relpath(fn, start_dir)
            # print("adding %s" % arcname)
            zipf.write(fn, arcname)


def push_build(build_archive_name, archive_sha256, include_private_scenes):
    import boto3

    # subprocess.run("ls %s" % build_archive_name, shell=True)
    # subprocess.run("gsha256sum %s" % build_archive_name)
    s3 = boto3.resource("s3")
    acl = 'public-read'
    bucket = PUBLIC_S3_BUCKET
    if include_private_scenes:
        bucket = PRIVATE_S3_BUCKET 
        acl = 'private'

    archive_base = os.path.basename(build_archive_name)
    key = "builds/%s" % (archive_base,)
    sha256_key = "builds/%s.sha256" % (os.path.splitext(archive_base)[0],)

    with open(build_archive_name, "rb") as af:
        s3.Object(bucket, key).put(Body=af, ACL=acl)

    s3.Object(bucket, sha256_key).put(
        Body=archive_sha256, ACL=acl, ContentType="text/plain"
    )
    print("pushed build %s to %s" % (bucket, build_archive_name))


def _webgl_local_build_path(prefix, source_dir="builds"):
    return os.path.join(
        os.getcwd(), "unity/{}/thor-{}-WebGL/".format(source_dir, prefix)
    )


def _unity_version():
    import yaml
    with open("unity/ProjectSettings/ProjectVersion.txt") as pf:
        project_version = yaml.load(pf.read(), Loader=yaml.FullLoader)

    return project_version['m_EditorVersion']

def _build(unity_path, arch, build_dir, build_name, env={}):
    import yaml
        
    project_path = os.path.join(os.getcwd(), unity_path)
    standalone_path = None

    unity_version = _unity_version()

    if sys.platform.startswith('darwin'):
        unity_hub_path = "/Applications/Unity/Hub/Editor/{}/Unity.app/Contents/MacOS/Unity".format(
            unity_version
        )
        standalone_path = "/Applications/Unity-{}/Unity.app/Contents/MacOS/Unity".format(unity_version)
    elif 'win' in sys.platform:
        unity_hub_path = "C:/PROGRA~1/Unity/Hub/Editor/{}/Editor/Unity.exe".format(unity_version)
        # TODO: Verify windows unity standalone path
        standalone_path = "C:/PROGRA~1/{}/Editor/Unity.exe".format(unity_version)
    elif sys.platform.startswith('linux'):
        unity_hub_path = "{}/Unity/{}/Editor/Unity".format(os.environ['HOME'], unity_version)

    if standalone_path and os.path.exists(standalone_path):
        unity_path = standalone_path
    else:
        unity_path = unity_hub_path
    command = (
        "%s -quit -batchmode -logFile %s.log -projectpath %s -executeMethod Build.%s"
        % (unity_path, build_name, project_path, arch)
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
    server_types = ['WSGI']
    try:
        import ai2thor.fifo_server
        server_types.append('FIFO')
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
    import hashlib

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
            renderObjectImage=True,
            renderClassImage=False,
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
            seg_img = event.cv2img[min_y:max_y, min_x:max_x, :]
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
    import multiprocessing as mp

    mp.set_start_method("spawn")

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
def local_build(context, prefix="local", arch="OSXIntel64"):
    build = ai2thor.build.Build(arch, 'local', False)
    env = dict()
    if os.path.isdir('unity/Assets/Private/Scenes'):
        env['INCLUDE_PRIVATE_SCENES'] = 'true'

    build_dir = os.path.join("builds", build.name)

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
        prefix='local',
        verbose=False,
        content_addressable=False,
        crowdsource_build=False
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

    def file_to_content_addressable(file_path, json_metadata_file_path, json_key):
        # name_split = os.path.splitext(file_path)
        path_split = os.path.split(file_path)
        directory = path_split[0]
        file_name = path_split[1]

        print("File name {} ".format(file_name))
        with open(file_path, 'rb') as f:
            h = hashlib.md5()
            h.update(f.read())
            md5_id = h.hexdigest()
        new_file_name = "{}_{}".format(md5_id, file_name)
        os.rename(
            file_path,
            os.path.join(directory, new_file_name)
        )

        with open(json_metadata_file_path, 'r+') as f:
            unity_json = json.load(f)
            print("UNITY json {}".format(unity_json))
            unity_json[json_key] = new_file_name

            print("UNITY L {}".format(unity_json))

            f.seek(0)
            json.dump(unity_json, f, indent=4)

    arch = 'WebGL'
    build_name = local_build_name(prefix, arch)
    if room_ranges is not None:
        floor_plans = ["FloorPlan{}_physics".format(i) for i in
            reduce(
                lambda x, y: x + y,
                map(
                    lambda x: x + [x[-1] + 1],
                    [list(range(*tuple(int(y) for y in x.split("-"))))
                        for x in room_ranges.split(",")]
                )
            )
         ]

        scenes = ",".join(floor_plans)
    if verbose:
        print(scenes)

    env = dict(SCENE=scenes)
    if crowdsource_build:
        env['DEFINES'] = 'CROWDSOURCE_TASK'
    if _build('unity', arch, directory, build_name, env=env):
        print("Build Successful")
    else:
        print("Build Failure")
    generate_quality_settings(context)
    build_path = _webgl_local_build_path(prefix, directory)

    rooms = {
        "kitchens": {
            "name": "Kitchens",
            "roomRanges": range(1, 31)
        },
        "livingRooms": {
            "name": "Living Rooms",
            "roomRanges": range(201, 231)
        },
        "bedrooms": {
            "name": "Bedrooms",
            "roomRanges": range(301, 331)
        },
        "bathrooms": {
            "name": "Bathrooms",
            "roomRanges": range(401, 431)
        },
        "foyers": {
            "name": "Foyers",
            "roomRanges": range(501, 531)
        }
    }

    room_type_by_id = {}
    scene_metadata = {}
    for room_type, room_data in rooms.items():
        for room_num in room_data["roomRanges"]:
            room_id = "FloorPlan{}_physics".format(room_num)
            room_type_by_id[room_id] = {
                "type": room_type,
                "name": room_data["name"]
            }

    for scene_name in scenes.split(","):
        room_type = room_type_by_id[scene_name]
        if room_type["type"] not in scene_metadata:
            scene_metadata[room_type["type"]] = {
                "scenes": [],
                "name": room_type["name"]
            }

        scene_metadata[room_type["type"]]["scenes"].append(scene_name)

    if verbose:
        print(scene_metadata)

    to_content_addressable = [
        ('{}.data.unityweb'.format(build_name), 'dataUrl'),
        ('{}.wasm.code.unityweb'.format(build_name), 'wasmCodeUrl'),
        ('{}.wasm.framework.unityweb'.format(build_name), 'wasmFrameworkUrl')
    ]
    for file_name, key in to_content_addressable:
        file_to_content_addressable(
            os.path.join(build_path, "Build/{}".format(file_name)),
            os.path.join(build_path, "Build/{}.json".format(build_name)),
            key
        )

    with open(os.path.join(build_path, "scenes.json"), 'w') as f:
        f.write(json.dumps(scene_metadata, sort_keys=False, indent=4))


@task
def generate_quality_settings(ctx):
    import yaml

    class YamlUnity3dTag(yaml.SafeLoader):
        def let_through(self, node):
            return self.construct_mapping(node)

    YamlUnity3dTag.add_constructor(
        u"tag:unity3d.com,2011:47", YamlUnity3dTag.let_through
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



def build_sha256(path):

    m = hashlib.sha256()

    with open(path, "rb") as f:
        m.update(f.read())

    return m.hexdigest()


def build_docker(version):

    subprocess.check_call(
        "docker build --quiet --rm --no-cache -t  ai2thor/ai2thor-base:{version} .".format(
            version=version
        ),
        shell=True,
    )

    subprocess.check_call(
        "docker push ai2thor/ai2thor-base:{version}".format(version=version), shell=True
    )

def git_commit_id():
    commit_id = (
        subprocess.check_output("git log -n 1 --format=%H", shell=True)
        .decode("ascii")
        .strip()
    )

    return commit_id

@task
def deploy_pip(context):
    if 'TWINE_PASSWORD' not in os.environ:
        raise Exception("Twine token not specified in environment")
    subprocess.check_call("twine upload -u __token__ dist/*", shell=True)

@task
def build_pip(context, version):
    from ai2thor.build import platform_map
    import re
    import xml.etree.ElementTree as ET 
    import requests

    res = requests.get('https://pypi.org/rss/project/ai2thor/releases.xml')

    res.raise_for_status()

    root = ET.fromstring(res.content)
    latest_version = None

    for title in root.findall('./channel/item/title'): 
        latest_version = title.text
        break

    # make sure that the tag is on this commit
    commit_tags = (
        subprocess.check_output("git tag --points-at", shell=True)
        .decode("ascii")
        .strip().split("\n")
    )

    if version not in commit_tags:
        raise Exception("tag %s is not on current commit" % version)

    commit_id = git_commit_id()

    res = requests.get('https://api.github.com/repos/allenai/ai2thor/commits?sha=master')
    res.raise_for_status()

    if commit_id not in map(lambda c: c['sha'], res.json()):
        raise Exception("tag %s is not off the master branch" % version)

    if not re.match(r'^[0-9]{1,3}\.+[0-9]{1,3}\.[0-9]{1,3}$', version):
        raise Exception("invalid version: %s" % version)

    for arch in platform_map.keys():
        commit_build = ai2thor.build.Build(arch, commit_id, False)
        if not commit_build.exists():
            raise Exception("Build does not exist for %s/%s" % (commit_id, arch))

    
    current_maj, current_min, current_sub = list(map(int, latest_version.split('.')))
    next_maj, next_min, next_sub = list(map(int, version.split('.')))

    if (next_maj > current_maj) or \
        (next_maj >= current_maj and next_min > current_min) or \
        (next_maj >= current_maj and next_min >= current_min and next_sub > current_sub):

        if os.path.isdir("dist"):
            shutil.rmtree("dist")


        generate_quality_settings(context)

        with open("ai2thor/_builds.py", "w") as fi:
            fi.write("# GENERATED FILE - DO NOT EDIT\n")
            fi.write("COMMIT_ID = '%s'\n" % commit_id)

        with open("ai2thor/_version.py", "w") as fi:
            fi.write("# Copyright Allen Institute for Artificial Intelligence 2017\n")
            fi.write("# GENERATED FILE - DO NOT EDIT\n")
            fi.write("__version__ = '%s'\n" % (version))

        subprocess.check_call("python setup.py clean --all", shell=True)
        subprocess.check_call("python setup.py sdist bdist_wheel --universal", shell=True)

    else:
        raise Exception("Invalid version: %s is not greater than latest version %s" % (version, latest_version))
            

@task
def fetch_source_textures(context):
    import ai2thor.downloader
    import io

    zip_data = ai2thor.downloader.download(
        "http://s3-us-west-2.amazonaws.com/ai2-thor/assets/source-textures.zip",
        "source-textures",
        "75476d60a05747873f1173ba2e1dbe3686500f63bcde3fc3b010eea45fa58de7",
    )

    z = zipfile.ZipFile(io.BytesIO(zip_data))
    z.extractall(os.getcwd())


def build_log_push(build_info, include_private_scenes):
    with open(build_info["log"]) as f:
        build_log = f.read() + "\n" + build_info["build_exception"]

    build_log_key = "builds/" + build_info["log"]
    s3 = boto3.resource("s3")

    bucket = PUBLIC_S3_BUCKET 
    acl = "public-read"

    if include_private_scenes:
        bucket = PRIVATE_S3_BUCKET 
        acl = 'private'

    s3.Object(bucket, build_log_key).put(
        Body=build_log, ACL=acl, ContentType="text/plain"
    )


def archive_push(unity_path, build_path, build_dir, build_info, include_private_scenes):
    threading.current_thread().success = False
    archive_name = os.path.join(unity_path, build_path)
    zipf = zipfile.ZipFile(archive_name, 'w', zipfile.ZIP_DEFLATED)
    add_files(zipf, os.path.join(unity_path, build_dir))
    zipf.close()

    build_info["sha256"] = build_sha256(archive_name)
    push_build(archive_name, build_info["sha256"], include_private_scenes)
    build_log_push(build_info, include_private_scenes)
    print("Build successful")
    threading.current_thread().success = True


@task
def pre_test(context):
    import ai2thor.controller
    import shutil

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
    scripts.update_private.private_repo_url = 'git@ai2thor-private-github:allenai/ai2thor-private.git'
    subprocess.check_call("git reset --hard", shell=True)
    subprocess.check_call("git clean -f -d", shell=True)
    subprocess.check_call("git clean -f -x", shell=True)
    shutil.rmtree("unity/builds", ignore_errors=True)
    shutil.rmtree(scripts.update_private.private_dir, ignore_errors=True)
    scripts.update_private.checkout_branch()


def link_build_cache(branch):
    library_path = os.path.join("unity", "Library")

    if os.path.exists(library_path):
        os.unlink(library_path)

    branch_cache_dir = os.path.join(os.environ["HOME"], "cache", branch, "Library")
    os.makedirs(branch_cache_dir, exist_ok=True)
    os.symlink(branch_cache_dir, library_path)

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
        "https://api.travis-ci.com/repo/3459357/builds?repository_id=3459357&include=build.id%2Cbuild.commit%2Cbuild.branch%2Cbuild.request%2Cbuild.created_by%2Cbuild.repository&build.state=started&sort_by=started_at:desc",
        headers={
            "Accept": "application/json",
            "Content-Type": "application/json",
            "Travis-API-Version": "3",
        },
    )

    for b in res.json()["builds"]:
        tag = None
        if b['tag']:
            tag = b['tag']['name']

        return {'branch':b["branch"]["name"], 'commit_id':b["commit"]["sha"], 'tag':tag, 'id': b['id']}


def pytest_s3_object(commit_id):
    s3 = boto3.resource("s3")
    pytest_key = 'builds/pytest-%s.json' % commit_id

    return s3.Object(PUBLIC_S3_BUCKET, pytest_key)

@task
def ci_pytest(context):
    import requests
    commit_id = git_commit_id()

    s3_obj = pytest_s3_object(commit_id)
    s3_pytest_url = 'http://s3-us-west-2.amazonaws.com/%s/%s' % (s3_obj.bucket_name, s3_obj.key)
    print("pytest url %s" % s3_pytest_url)
    res = requests.get(s3_pytest_url)

    if res.status_code == 200 and res.json()['success']:
        # if we already have a successful pytest, skip running
        return


    proc = subprocess.run("pytest", shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)


    result = dict(
        success=proc.returncode == 0,
        stdout=proc.stdout.decode('ascii'),
        stderr=proc.stderr.decode('ascii')
    )


    s3_obj.put(
        Body=json.dumps(result), ACL="public-read", ContentType='application/json'
    )


@task
def ci_build(context):
    import fcntl
    import io

    lock_f = open(os.path.join(os.environ["HOME"], ".ci-build.lock"), "w")

    try:
        fcntl.flock(lock_f, fcntl.LOCK_EX | fcntl.LOCK_NB)
        build = pending_travis_build()
        blacklist_branches = ["vids", "video"]
        if build and build["branch"] not in blacklist_branches:
            clean()
            link_build_cache(build["branch"])
            subprocess.check_call("git fetch", shell=True)
            subprocess.check_call("git checkout %s" % build["branch"], shell=True)
            subprocess.check_call(
                "git checkout -qf %s" % build["commit_id"], shell=True
            )

            procs = []
            for arch in ["OSXIntel64", "Linux64"]:
                p = ci_build_arch(arch, False)
                procs.append(p)



            # don't run tests for a tag since results should exist
            # for the branch commit
            if build['tag'] is None:

                ci_pytest(context)

            # give the travis poller time to see the result
            for i in range(6):
                b = travis_build(build['id'])
                print("build state for %s: %s" % (build['id'], b['state']))

                if b['state'] != 'started': 
                    break
                time.sleep(10)

            if build["branch"] == "master":
                webgl_build_deploy_demo(
                    context, verbose=True, content_addressable=True, force=True
                )

            for p in procs:
                if p:
                    p.join()
            

            
        fcntl.flock(lock_f, fcntl.LOCK_UN)

    except io.BlockingIOError as e:
        pass

    lock_f.close()


def ci_build_arch(arch, include_private_scenes=False):
    from multiprocessing import Process

    commit_id = git_commit_id()
    commit_build = ai2thor.build.Build(arch, commit_id, include_private_scenes)
    if commit_build.exists():
        print("found build for commit %s %s" % (commit_id, arch))
        return

    unity_path = "unity"
    build_name = ai2thor.build.build_name(arch, commit_id, include_private_scenes)
    build_dir = os.path.join("builds", build_name)
    build_path = build_dir + ".zip"
    build_info = {}

    build_info["build_exception"] = ""

    proc = None
    try:
        build_info["log"] = "%s.log" % (build_name,)
        _build(unity_path, arch, build_dir, build_name)

        print("pushing archive")
        proc = Process(
            target=archive_push, args=(unity_path, build_path, build_dir, build_info, include_private_scenes)
        )
        proc.start()

    except Exception as e:
        print("Caught exception %s" % e)
        build_info["build_exception"] = "Exception building: %s" % e
        build_log_push(build_info, include_private_scenes)

    return proc


@task
def poll_ci_build(context):
    from ai2thor.build import platform_map
    import requests.exceptions
    import requests

    commit_id = git_commit_id()

    for i in range(360):
        missing = False
        for arch in platform_map.keys():
            if (i % 5) == 0:
                print("checking %s for commit id %s" % (arch, commit_id))
            commit_build = ai2thor.build.Build(arch, commit_id, False)
            try:
                if commit_build.log_exists():
                    print("log exists %s" % commit_id)
                else:
                    missing = True
            # we observe errors when polling AWS periodically - we don't want these to stop
            # the build
            except requests.exceptions.ConnectionError as e:
                print("Caught exception %s" % e)

        if not missing:
            break
        sys.stdout.flush()
        time.sleep(10)

    for arch in platform_map.keys():
        commit_build = ai2thor.build.Build(arch, commit_id, False)
        if not commit_build.exists():
            print(
                "Build log url: %s"
                % commit_build.log_url()
            )
            raise Exception("Failed to build %s for commit: %s " % (arch, commit_id))

    pytest_missing = True
    for i in range(30):
        s3_obj = pytest_s3_object(commit_id)
        s3_pytest_url = 'http://s3-us-west-2.amazonaws.com/%s/%s' % (s3_obj.bucket_name, s3_obj.key)
        print("pytest url %s" % s3_pytest_url)
        res = requests.get(s3_pytest_url)
        if res.status_code == 200:
            pytest_missing = False
            pytest_result = res.json()
            print(pytest_result['stdout']) # print so that it appears in travis log
            print(pytest_result['stderr'])
            if not pytest_result['success']:
                raise Exception("pytest failure")
            break
        time.sleep(10)
    
    if pytest_missing:
        raise Exception("Missing pytest output")


@task
def build(context, local=False):
    from multiprocessing import Process
    from ai2thor.build import platform_map


    version = datetime.datetime.now().strftime("%Y%m%d%H%M")

    builds = {"Docker": {"tag": version}}
    threads = []
    # dp = Process(target=build_docker, args=(version,))
    # dp.start()

    for include_private_scenes in (True, False):
        for arch in platform_map.keys():
            env = {}
            if include_private_scenes:
                env['INCLUDE_PRIVATE_SCENES'] = 'true'
            unity_path = "unity"
            build_name = ai2thor.build.build_name(arch, version, include_private_scenes)
            build_dir = os.path.join("builds", build_name)
            build_path = build_dir + ".zip"
            build_info = builds[platform_map[arch]] = {}

            build_info["build_exception"] = ""
            build_info["log"] = "%s.log" % (build_name,)

            _build(unity_path, arch, build_dir, build_name, env=env)
            t = threading.Thread(
                target=archive_push, args=(unity_path, build_path, build_dir, build_info, include_private_scenes)
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
    host='127.0.0.1',
    image_directory='.',
    width=300,
    height=300,
    include_private_scenes=False,
    noise=False
):
    import ai2thor.controller
    import ai2thor.robot_controller

    if image_directory != '.':
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
            scene=scene
        )
    else:
        env = ai2thor.robot_controller.Controller(
            host=host,
            port=port,
            width=width,
            height=height,
            image_dir=image_directory,
            save_image_per_frame=True
        )

    env.reset(scene)
    initialize_event = env.step(
        dict(
            action="Initialize",
            gridSize=0.25,
            renderObjectImage=object_image,
            renderClassImage=class_image,
            renderDepthImage=depth_image
        )
    )

    from ai2thor.interact import InteractiveControllerPrompt
    InteractiveControllerPrompt.write_image(
        initialize_event,
        image_directory,
        '_init',
        image_per_frame=True,
        class_segmentation_frame=class_image,
        instance_segmentation_frame=object_image,
        color_frame=image,
        depth_frame=depth_image,
        metadata=metadata
    )

    env.interact(
        class_segmentation_frame=class_image,
        instance_segmentation_frame=object_image,
        depth_frame=depth_image,
        color_frame=image,
        metadata=metadata
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
        host='127.0.0.1',
        image_directory='.',
        number=1,
        local_build=False,
        teleport=None,
        rotation=0
    ):
    import ai2thor.controller
    import ai2thor.robot_controller

    if image_directory != '.':
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
            save_image_per_frame=True

        )
    else:
        env = ai2thor.controller.Controller(
            width=600,
            height=600,
            local_build=local_build
        )

    if scene is not None:
        env.reset(scene)

    initialize_event = env.step(
        dict(
            action="Initialize",
            gridSize=0.25,
            renderObjectImage=object_image,
            renderClassImage=class_image,
            renderDepthImage=depth_image,
            agentMode="Bot",
            fieldOfView=59,
            continuous=True,
            snapToGrid=False
        )
    )

    from ai2thor.interact import InteractiveControllerPrompt
    if scene is not None:
        teleport_arg = dict(
            action="TeleportFull",
            y=0.9010001,
            rotation=dict(x=0, y=rotation, z=0)
        )
        if teleport is not None:
            teleport = [float(pos) for pos in teleport.split(',')]

            t_size = len(teleport)
            if 1 <= t_size:
                teleport_arg['x'] = teleport[0]
            if 2 <= t_size:
                teleport_arg['z'] = teleport[1]
            if 3 <= t_size:
                teleport_arg['y'] = teleport[2]

        evt = env.step(
            teleport_arg
        )

        InteractiveControllerPrompt.write_image(
            evt,
            image_directory,
            '_{}'.format('teleport'),
            image_per_frame=True,
            class_segmentation_frame=class_image,
            instance_segmentation_frame=object_image,
            color_frame=image,
            depth_frame=depth_image,
            metadata=metadata
        )

    InteractiveControllerPrompt.write_image(
        initialize_event,
        image_directory,
        '_init',
        image_per_frame=True,
        class_segmentation_frame=class_image,
        instance_segmentation_frame=object_image,
        color_frame=image,
        depth_frame=depth_image,
        metadata=metadata
    )

    for i in range(number):
        event = env.step(action='MoveAhead', moveMagnitude=0.0)

        InteractiveControllerPrompt.write_image(
            event,
            image_directory,
            '_{}'.format(i),
            image_per_frame=True,
            class_segmentation_frame=class_image,
            instance_segmentation_frame=object_image,
            color_frame=image,
            depth_frame=depth_image,
            metadata=metadata
        )
    env.stop()

@task
def inspect_depth(ctx, directory, all=False, indices=None, jet=False, under_score=False):
    import numpy as np
    import cv2
    import glob
    import re

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
            key=sort_key_function
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
        depth_raw_filename = os.path.join(directory, "depth_raw{}{}.npy".format("_" if under_score else "", index))
        raw_depth = np.load(depth_raw_filename)

        if jet:
            mn = np.min(raw_depth)
            mx = np.max(raw_depth)
            print("min depth value: {}, max depth: {}".format(mn, mx))
            norm = (((raw_depth - mn).astype(np.float32) / (mx - mn)) * 255.0).astype(np.uint8)

            img = cv2.applyColorMap(norm, cv2.COLORMAP_JET)
        else:
            grayscale = (255.0 / raw_depth.max() * (raw_depth - raw_depth.min())).astype(np.uint8)
            print("max {} min {}".format(raw_depth.max(), raw_depth.min()))
            img = grayscale

        print(raw_depth.shape)

        def inspect_pixel(event, x, y, flags, param):
            if event == cv2.EVENT_LBUTTONDOWN:
                print("Pixel at x: {}, y: {} ".format(y, x))
                print(raw_depth[y][x])

        cv2.namedWindow("image")
        cv2.setMouseCallback("image", inspect_pixel)

        cv2.imshow('image', img)
        cv2.waitKey(0)


@task
def real_2_sim(ctx, source_dir, index, scene, output_dir, rotation=0, local_build=False, jet=False):
    import json
    import numpy as np
    import cv2
    from ai2thor.util.transforms import transform_real_2_sim
    depth_real_fn = os.path.join(source_dir, "depth_raw_{}.npy".format(index))
    depth_metadata_fn = depth_real = os.path.join(source_dir, "metadata_{}.json".format(index))
    color_real_fn = os.path.join(source_dir, "color_{}.png".format(index))
    color_sim_fn = os.path.join(output_dir, "color_teleport.png".format(index))
    with open(depth_metadata_fn, 'r') as f:
        metadata = json.load(f)

        pos = metadata['agent']['position']

        sim_pos = transform_real_2_sim(pos)

        teleport_arg = "{},{},{}".format(sim_pos['x'], sim_pos['z'], sim_pos['y'])

        print(sim_pos)
        print(teleport_arg)

        inspect_depth(
            ctx,
            source_dir,
            indices=index,
            under_score=True,
            jet=jet
        )

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
            rotation=rotation
        )

        im = cv2.imread(color_real_fn)
        cv2.imshow("color_real.png", im)

        im2 = cv2.imread(color_sim_fn)
        cv2.imshow("color_sim.png", im2)

        inspect_depth(
            ctx,
            output_dir,
            indices="teleport",
            under_score=True,
            jet=jet
        )

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
            cv2.imshow('labeled.png', labeled_img)
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
        indices_top_right = np.where(labels == labels[0][img_size[1]-1])
        indices_bottom_left = np.where(labels == labels[img_size[0]-1][0])
        indices_bottom_right = np.where(labels == labels[img_size[0]-1][img_size[1] - 1])

        indices = [
             indices_top_left,
             indices_top_right,
             indices_bottom_left,
             indices_bottom_right
        ]

        blank_image = np.zeros((300, 300, 1), np.uint8)

        blank_image.fill(255)
        blank_image[indices_top_left] = 0
        blank_image[indices_top_right] = 0
        blank_image[indices_bottom_left] = 0
        blank_image[indices_bottom_right] = 0

        if show:
            cv2.imshow('labeled.png', blank_image)
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
    subprocess.check_call("git push origin master --tags", shell=True)
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

    controller = ai2thor.controller.BFSController(local_build=True)
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
    screen_width=600,
    screen_height=600,
    editor_mode=False,
    out="benchmark.json",
    verbose=False,
):
    import ai2thor.controller
    import random
    import json

    move_actions = ["MoveAhead", "MoveBack", "MoveLeft", "MoveRight"]
    rotate_actions = ["RotateRight", "RotateLeft"]
    look_actions = ["LookUp", "LookDown"]
    all_actions = move_actions + rotate_actions + look_actions

    def test_routine(env, test_actions, n=100):
        average_frame_time = 0
        for i in range(n):
            action = random.choice(test_actions)
            start = time.time()
            event = env.step(dict(action=action))
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

    env = ai2thor.controller.Controller(local_build=True)
    if editor_mode:
        env.start(
            8200,
            False,
            width=screen_width,
            height=screen_height,
        )
    else:
        env.start(width=screen_width, height=screen_height)
    # Kitchens:       FloorPlan1 - FloorPlan30
    # Living rooms:   FloorPlan201 - FloorPlan230
    # Bedrooms:       FloorPlan301 - FloorPlan330
    # Bathrooms:      FloorPLan401 - FloorPlan430

    room_ranges = [(1, 30), (201, 230), (301, 330), (401, 430)]

    benchmark_map = {"scenes": {}}
    total_average_ft = 0
    scene_count = 0
    print("Start loop")
    for room_range in room_ranges:
        for i in range(room_range[0], room_range[1]):
            scene = "FloorPlan{}_physics".format(i)
            scene_benchmark = {}
            if verbose:
                print("Loading scene {}".format(scene))
            # env.reset(scene)
            env.step(dict(action="Initialize", gridSize=0.25))

            if verbose:
                print("------ {}".format(scene))

            sample_number = 100
            action_tuples = [
                ("move", move_actions, sample_number),
                ("rotate", rotate_actions, sample_number),
                ("look", look_actions, sample_number),
                ("all", all_actions, sample_number),
            ]
            scene_average_fr = 0
            for action_name, actions, n in action_tuples:
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
def webgl_deploy(ctx, bucket='ai2-thor-webgl-public', prefix='local', source_dir='builds', target_dir='', verbose=False, force=False, extensions_no_cache=''):
    from pathlib import Path
    from os.path import isfile, join, isdir

    content_types = {
        '.js': 'application/javascript; charset=utf-8',
        '.html': 'text/html; charset=utf-8',
        '.ico': 'image/x-icon',
        '.svg': 'image/svg+xml; charset=utf-8',
        '.css': 'text/css; charset=utf-8',
        '.png': 'image/png',
        '.txt': 'text/plain',
        '.jpg': 'image/jpeg',
        '.unityweb': 'application/octet-stream',
        '.json': 'application/json'
    }

    content_encoding = {
        '.unityweb': 'gzip'
    }

    bucket_name = bucket
    s3 = boto3.resource('s3')

    current_objects = list_objects_with_metadata(bucket_name)

    no_cache_extensions = {
        ".txt",
        ".html",
        ".json",
        ".js"
    }

    no_cache_extensions.union(set(extensions_no_cache.split(',')))

    if verbose:
        session = boto3.Session()
        credentials = session.get_credentials()

        # Credentials are refreshable, so accessing your access key / secret key
        # separately can lead to a race condition. Use this to get an actual matched
        # set.
        credentials = credentials.get_frozen_credentials()
        access_key = credentials.access_key
        secret_key = credentials.secret_key
        # print("key:  {} pass: {}".format(access_key, secret_key))
        # print("Deploying to: {}/{}".format(bucket_name, target_dir))

    def walk_recursive(path, func, parent_dir=''):
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

        with open(f_path, 'rb') as f:
            file_data = f.read()
            etag = s3_etag_data(file_data)
            kwargs = {}
            if ext in content_encoding:
                kwargs['ContentEncoding'] = content_encoding[ext]

            if not force and key in current_objects and etag == current_objects[key]['ETag']:
                if verbose:
                    print("ETag match - skipping %s" % key)
                return

            if ext in content_types:
                cache = 'no-cache, no-store, must-revalidate' if ext in no_cache_extensions else 'public, max-age={}'.format(
                    cache_seconds
                )
                now = datetime.datetime.utcnow()
                expires = now if ext == '.html' or ext == '.txt' else now + datetime.timedelta(
                    seconds=cache_seconds)
                s3.Object(bucket_name, key).put(
                    Body=file_data,
                    ACL="public-read",
                    ContentType=content_types[ext],
                    CacheControl=cache,
                    Expires=expires,
                    **kwargs
                )
            else:
                if verbose:
                    print("Warning: Content type for extension '{}' not defined,"
                          " uploading with no content type".format(ext))
                s3.Object(bucket_name, key).put(
                    Body=f.read(),
                    ACL="public-read")

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
    webgl_build(
        ctx,
        room_ranges="1-30,201-230,301-330,401-430",
        content_addressable=content_addressable,
    )
    webgl_deploy(ctx, verbose=verbose, force=force, target_dir="full")

    if verbose:
        print("Deployed all scenes to bucket's root.")


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
def webgl_s3_deploy(ctx, bucket, target_dir, scenes='', verbose=False, all=False, deploy_skip=False):
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
        "bathrooms": (401, 430)
    }

    if all:
        flatten = lambda l: [item for sublist in l for item in sublist]
        room_numbers = flatten([[i for i in range(room_range[0], room_range[1])] for key, room_range in rooms.items()])
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

        webgl_build(ctx, scenes=floor_plan_name, directory=build_dir, crowdsource_build=True)
        if verbose:
            print("Deploying room '{}'...".format(floor_plan_name))
        if not deploy_skip:
            webgl_deploy(ctx, bucket=bucket,  source_dir=build_dir, target_dir=target_s3_dir, verbose=verbose, extensions_no_cache='.css')


@task
def webgl_site_deploy(context, template_name, output_dir, bucket, unity_build_dir='', s3_target_dir='', force=False, verbose=False):
    from pathlib import Path
    from os.path import isfile, join, isdir
    template_dir = Path("unity/Assets/WebGLTemplates/{}".format(template_name))

    if os.path.exists(output_dir):
        shutil.rmtree(output_dir)
    # os.mkdir(output_dir)

    ignore_func = lambda d, files: [f for f in files if isfile(join(d, f)) and f.endswith('.meta')]

    if unity_build_dir != '':
        shutil.copytree(unity_build_dir, output_dir, ignore=ignore_func)
        # shutil.copytree(os.path.join(unity_build_dir, "Build"), os.path.join(output_dir, "Build"), ignore=ignore_func)
    else:
        shutil.copytree(template_dir, output_dir, ignore=ignore_func)

    webgl_deploy(context, bucket=bucket, prefix=None, source_dir=output_dir,  target_dir=s3_target_dir, verbose=verbose, force=force, extensions_no_cache='.css')
@task
def mock_client_request(context):
    import msgpack
    import numpy as np
    import requests
    import cv2
    from pprint import pprint

    r = requests.post('http://127.0.0.1:9200/step', json=dict(action='MoveAhead', sequenceId=1))
    s = time.time()
    payload = msgpack.unpackb(r.content, raw=False)
    metadata = payload['metadata']['agents'][0]
    image = np.frombuffer(payload['frames'][0], dtype=np.uint8).reshape(metadata['screenHeight'], metadata['screenWidth'], 3)
    pprint(metadata)
    cv2.imshow('aoeu', image)
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
        output='robothor-dataset.json',
        intermediate_directory='.',
        visibility_distance=1.0,
        objects_filter=None,
        scene_filter=None,
        filter_file=None
    ):
    """
    Creates a dataset for the robothor challenge in `intermediate_directory`
    named `robothor-dataset.json`
    """
    import ai2thor.controller
    import ai2thor.util.metrics as metrics
    import json
    from pprint import pprint

    scene = 'FloorPlan_Train1_1'
    angle = 45
    gridSize = 0.25
    # Restrict points visibility_multiplier_filter * visibility_distance away from the target object
    visibility_multiplier_filter = 2

    scene_object_filter = {}
    if filter_file is not None:
        with open(filter_file, 'r') as f:
            scene_object_filter = json.load(f)
            print("Filter:")
            pprint(scene_object_filter)

    print("Visibility distance: {}".format(visibility_distance))
    controller = ai2thor.controller.Controller(
        width=width,
        height=height,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        scene=scene,
        port=8200,
        host='127.0.0.1',
        # Unity params
        gridSize=gridSize,
        fieldOfView=60,
        rotateStepDegrees=angle,
        agentMode='bot',
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
        "Pillow"

    ]
    failed_points = []

    if objects_filter is not None:
        obj_filter = set([o for o in objects_filter.split(",")])
        targets = [o for o in targets if o.replace(" ", "") in obj_filter]

    desired_points = 30
    event = controller.step(
        dict(
            action='GetScenesInBuild',
        )
    )
    scenes_in_build = event.metadata['actionReturn']
    objects_types_in_scene = set()

    def sqr_dist(a, b):
        x = a[0] - b[0]
        z = a[2] - b[2]
        return x * x + z * z

    def sqr_dist_dict(a, b):
        x = a['x'] - b['x']
        z = a['z'] - b['z']
        return x * x + z * z

    def get_points(contoller, object_type, scene):
        print("Getting points in scene: '{}'...: ".format(scene))
        controller.reset(scene)
        event = controller.step(
            dict(
                action='ObjectTypeToObjectIds',
                objectType=object_type.replace(" ", "")
            )
        )
        object_ids = event.metadata['actionReturn']

        if object_ids is None or len(object_ids) > 1 or len(object_ids) == 0:
            print("Object type '{}' not available in scene.".format(object_type))
            return None

        objects_types_in_scene.add(object_type)
        object_id = object_ids[0]

        event_reachable = controller.step(
            dict(
                action='GetReachablePositions',
                gridSize=0.25
            )
        )

        target_position = controller.step(action='GetObjectPosition', objectId=object_id).metadata['actionReturn']

        reachable_positions = event_reachable.metadata['actionReturn']

        reachable_pos_set = set([
            (pos['x'], pos['y'], pos['z']) for pos in reachable_positions
            # if sqr_dist_dict(pos, target_position) >= visibility_distance * visibility_multiplier_filter
        ])

        def filter_points(selected_points, point_set, minimum_distance):
            result = set()
            for selected in selected_points:
                if selected in point_set:
                    result.add(selected)
                    remove_set = set(
                        [p for p in point_set if sqr_dist(p, selected) <= minimum_distance * minimum_distance]
                    )
                    point_set = point_set.difference(remove_set)
            return result

        import random
        points = random.sample(reachable_pos_set, desired_points * 4)

        final_point_set = filter_points(points, reachable_pos_set, gridSize * 2)

        print("Total number of points: {}".format(len(final_point_set)))

        print("Id {}".format(event.metadata['actionReturn']))

        point_objects = []

        eps = 0.0001
        counter = 0
        for (x, y, z) in final_point_set:
            possible_orientations = [0, 90, 180, 270]
            pos_unity = dict(x=x, y=y, z=z)
            try:
                path = metrics.get_shortest_path_to_object(
                    controller,
                    object_id,
                    pos_unity,
                    {'x': 0, 'y': 0, 'z': 0}
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
                        x=pos_unity['x'],
                        y=pos_unity['y'],
                        z=pos_unity['z'],
                        rotation=dict(x=0, y=roatation_y, z=0)
                    )
                    rotation_allowed = evt.metadata['lastActionSuccess']
                    if not evt.metadata['lastActionSuccess']:
                        print(evt.metadata['errorMessage'])
                        print("--------- Rotation not allowed! for pos {} rot {} ".format(pos_unity, roatation_y))

                if minimum_path_length > eps and rotation_allowed:
                    m = re.search('FloorPlan_([a-zA-Z\-]*)([0-9]+)_([0-9]+)', scene)
                    point_id = "{}_{}_{}_{}_{}".format(
                        m.group(1),
                        m.group(2),
                        m.group(3),
                        object_type,
                        counter
                    )
                    point_objects.append({
                        'id': point_id,
                        'scene': scene,
                        'object_type': object_type,
                        'object_id': object_id,
                        'target_position': target_position,
                        'initial_position': pos_unity,
                        'initial_orientation': roatation_y,
                        'shortest_path': path,
                        'shortest_path_length': minimum_path_length
                    })
                    counter += 1

            except ValueError:
                print("-----Invalid path discarding point...")
                failed_points.append({
                    'scene': scene,
                    'object_type': object_type,
                    'object_id': object_id,
                    'target_position': target_position,
                    'initial_position': pos_unity
                })


        sorted_objs = sorted(point_objects,
                             key=lambda m: m['shortest_path_length'])
        third = int(len(sorted_objs) / 3.0)

        for i, obj in enumerate(sorted_objs):
            if i < third:
                level = 'easy'
            elif i < 2 * third:
                level = 'medium'
            else:
                level = 'hard'

            sorted_objs[i]['difficulty'] = level

        return sorted_objs

    dataset = {}
    dataset_flat = []

    if intermediate_directory is not None:
        if intermediate_directory != '.':
            if os.path.exists(intermediate_directory):
                shutil.rmtree(intermediate_directory)
            os.makedirs(intermediate_directory)
    import re

    def key_sort_func(scene_name):
        m = re.search('FloorPlan_([a-zA-Z\-]*)([0-9]+)_([0-9]+)', scene_name)
        return m.group(1), int(m.group(2)), int(m.group(3))

    scenes = sorted(
        [scene for scene in scenes_in_build if 'physics' not in scene],
                    key=key_sort_func
                    )

    if scene_filter is not None:
        scene_filter_set = set(scene_filter.split(","))
        scenes = [s for s in scenes if s in scene_filter_set]

    print("Sorted scenes: {}".format(scenes))
    for scene in scenes:
        dataset[scene] = {}
        dataset['object_types'] = targets
        objects = []
        for objectType in targets:

            if filter_file is None or (objectType in scene_object_filter and scene in scene_object_filter[objectType]):
                dataset[scene][objectType] = []
                obj = get_points(controller, objectType, scene)
                if obj is not None:

                    objects = objects + obj

        dataset_flat = dataset_flat + objects
        if intermediate_directory != '.':
            with open(os.path.join(intermediate_directory, '{}.json'.format(scene)), 'w') as f:
                json.dump(objects, f, indent=4)


    with open(os.path.join(intermediate_directory, output), 'w') as f:
        json.dump(dataset_flat, f, indent=4)
    print("Object types in scene union: {}".format(objects_types_in_scene))
    print("Total unique objects: {}".format(len(objects_types_in_scene)))
    print("Total scenes: {}".format(len(scenes)))
    print("Total datapoints: {}".format(len(dataset_flat)))

    print(failed_points)
    with open(os.path.join(intermediate_directory, 'failed.json'), 'w') as f:
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
        grid_size=0.25
):
    p = dict(x=x, y=y, z=z)

    import ai2thor.controller
    import ai2thor.util.metrics as metrics

    scene = scene
    angle = 45
    gridSize = grid_size
    controller = ai2thor.controller.Controller(
        width=300,
        height=300,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        scene=scene,
        port=8200,
        host='127.0.0.1',
        # Unity params
        gridSize=gridSize,
        fieldOfView=60,
        rotateStepDegrees=angle,
        agentMode='bot',
        visibilityDistance=visibility_distance,
    )
    path = metrics.get_shortest_path_to_object_type(
        controller,
        object,
        p,
        {'x': 0, 'y': 0, 'z': 0}
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
    import json
    from pprint import pprint
    with open(filename, 'r') as f:
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
        "Pillow"
    ]

    counter = {}
    for f in obj:
        obj_type = f['object_type']

        if f['scene'] not in counter:
            counter[f['scene']] = {target: 0 for target in targets}
        scene_counter = counter[f['scene']]
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
    with open('with_zero.json', 'w') as fw:
        dict_list = {k: list(v) for k, v in objects_with_zero_by_obj.items()}
        json.dump(dict_list, fw, sort_keys=True, indent=4)
    pprint(objects_with_zero_by_obj)
    filtered = [o for o in obj if o['object_type'] not in objects_with_zero]
    counter = 0
    current_scene = ""
    current_object_type = ""
    import re
    for i, o in enumerate(filtered):
        if current_scene != o['scene'] or current_object_type != o['object_type']:
            counter = 0
            current_scene = o['scene']
            current_object_type = o['object_type']

        m = re.search('FloorPlan_([a-zA-Z\-]*)([0-9]+)_([0-9]+)', o['scene'])
        point_id = "{}_{}_{}_{}_{}".format(
            m.group(1),
            m.group(2),
            m.group(3),
            o['object_type'],
            counter
        )
        counter += 1

        o['id'] = point_id
    with open(output_filename, 'w') as f:
        json.dump(filtered, f, indent=4)


@task
def fix_dataset_object_types(ctx, input_file, output_file, editor_mode=False, local_build=False):
    import json
    import ai2thor.controller

    with open(input_file, 'r') as f:
        obj = json.load(f)
        scene = 'FloorPlan_Train1_1'
        angle = 45
        gridSize = 0.25
        controller = ai2thor.controller.Controller(
            width=300,
            height=300,
            local_build=local_build,
            start_unity=False if editor_mode else True,
            scene=scene,
            port=8200,
            host='127.0.0.1',
            # Unity params
            gridSize=gridSize,
            fieldOfView=60,
            rotateStepDegrees=angle,
            agentMode='bot',
            visibilityDistance=1,
        )
        current_scene = None
        object_map = {}
        for i, point in enumerate(obj):
            if current_scene != point['scene']:
                print("Fixing for scene '{}'...".format(point['scene']))
                controller.reset(point['scene'])
                current_scene = point['scene']
                object_map = {o['objectType'].lower(): {'id': o['objectId'], 'type': o['objectType']} for o in controller.last_event.metadata['objects']}
            key = point['object_type'].replace(" ", "").lower()
            point['object_id'] = object_map[key]['id']
            point['object_type'] = object_map[key]['type']

        with open(output_file, 'w') as fw:
            json.dump(obj, fw, indent=True)


@task
def test_dataset(ctx, filename, scenes=None, objects=None, editor_mode=False, local_build=False):
    import json
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
        host='127.0.0.1',
        # Unity params
        gridSize=0.25,
        fieldOfView=60,
        rotateStepDegrees=45,
        agentMode='bot',
        visibilityDistance=1,
    )
    with open(filename, 'r') as f:
        dataset = json.load(f)
        filtered_dataset = dataset
        if scenes is not None:
            scene_set = set(scenes.split(","))
            print("Filtering {}".format(scene_set))
            filtered_dataset = [d for d in dataset if d['scene'] in scene_set]
        if objects is not None:
            object_set = set(objects.split(","))
            print("Filtering {}".format(object_set))
            filtered_dataset = [d for d in filtered_dataset if d['object_type'] in object_set]
        current_scene = None
        current_object = None
        point_counter = 0
        print(len(filtered_dataset))
        for point in filtered_dataset:
            if current_scene != point['scene']:
                current_scene = point['scene']
                print("Testing for scene '{}'...".format(current_scene))
            if current_object != point['object_type']:
                current_object = point['object_type']
                point_counter = 0
                print("    Object '{}'...".format(current_object))
            try:
                path = metrics.get_shortest_path_to_object_type(
                    controller,
                    point['object_type'],
                    point['initial_position'],
                    {'x': 0, 'y': point['initial_orientation'], 'z': 0}
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
        output_dir='.',
        object_types=None
):
    angle = 45
    import ai2thor.controller
    import json
    from PIL import Image
    controller = ai2thor.controller.Controller(
        width=width,
        height=height,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        port=8200,
        host='127.0.0.1',
        # Unity params
        gridSize=gridSize,
        fieldOfView=60,
        rotateStepDegrees=angle,
        agentMode='bot',
        visibilityDistance=1,
    )
    if output_dir != '.' and os.path.exists(output_dir):
        shutil.rmtree(output_dir)
    if output_dir != '.':
        os.mkdir(output_dir)
    evt = controller.step(
        action='AddThirdPartyCamera',
        rotation=dict(x=90, y=0, z=0),
        position=dict(x=5.40, y=3.25, z=-3.0),
        fieldOfView=2.25,
        orthographic=True
    )

    evt = controller.step(action='SetTopLevelView', topView=True)
    evt = controller.step(action='ToggleMapView')

    # im = Image.fromarray(evt.third_party_camera_frames[0])
    # im.save(os.path.join(output_dir, "top_view.jpg"))

    with open(dataset_path, 'r') as f:
        dataset = json.load(f)

        dataset_filtered = dataset
        if scenes is not None:
            scene_f_set = set(scenes.split(","))
            dataset_filtered = [d for d in dataset if d['scene'] in scene_f_set]
        if object_types is not None:
            object_f_set = set(object_types.split(","))
            dataset_filtered = [d for d in dataset_filtered if d['object_type'] in object_f_set]
        print("Running for {} points...".format(len(dataset_filtered)))

        index = 0
        print(index)
        print(len(dataset_filtered))
        datapoint = dataset_filtered[index]
        current_scene = datapoint['scene']
        current_object = datapoint['object_type']
        failed ={}
        while index < len(dataset_filtered):
            previous_index = index
            controller.reset(current_scene)
            while current_scene == datapoint['scene'] and current_object == datapoint['object_type']:
                index += 1
                if index > len(dataset_filtered) - 1:
                    break
                datapoint = dataset_filtered[index]

            current_scene = datapoint['scene']
            current_object = datapoint['object_type']

            key = "{}_{}".format(current_scene, current_object)

            failed[key] = []

            print("Points for '{}' in scene '{}'...".format(current_object, current_scene))
            evt = controller.step(
                action='AddThirdPartyCamera',
                rotation=dict(x=90, y=0, z=0),
                position=dict(x=5.40, y=3.25, z=-3.0),
                fieldOfView=2.25,
                orthographic=True
            )

            sc = dataset_filtered[previous_index]['scene']
            obj_type = dataset_filtered[previous_index]['object_type']
            positions = [d['initial_position'] for d in dataset_filtered[previous_index:index]]
            # print("{} : {} : {}".format(sc, obj_type, positions))
            evt = controller.step(
                action="VisualizeShortestPaths",
                objectType=obj_type,
                positions=positions,
                grid=True
            )
            im = Image.fromarray(evt.third_party_camera_frames[0])
            im.save(os.path.join(output_dir, "{}-{}.jpg".format(sc, obj_type)))

            # print("Retur {}, {} ".format(evt.metadata['actionReturn'], evt.metadata['lastActionSuccess']))
            # print(evt.metadata['errorMessage'])
            failed[key] = [positions[i] for i, success in enumerate(evt.metadata['actionReturn']) if not success]

        from pprint import pprint
        pprint(failed)

@task
def fill_in_dataset(
        ctx,
        dataset_dir,
        dataset_filename,
        filter_filename,
        intermediate_dir,
        output_filename='filled.json',
        local_build=False,
        editor_mode=False,
        visibility_distance=1.0
):
    import json
    import re
    import glob
    import ai2thor.controller
    dataset_path = os.path.join(dataset_dir, dataset_filename)
    output_dataset_path = os.path.join(dataset_dir, output_filename)
    filled_dataset_path = os.path.join(intermediate_dir, output_filename)
    def key_sort_func(scene_name):
        m = re.search('FloorPlan_([a-zA-Z\-]*)([0-9]+)_([0-9]+)', scene_name)
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
        "Pillow"
    ]

    controller = ai2thor.controller.Controller(
        width=300,
        height=300,
        local_build=local_build,
        start_unity=False if editor_mode else True,
        port=8200,
        host='127.0.0.1',
        # Unity params
        gridSize=0.25,
        fieldOfView=60,
        rotateStepDegrees=45,
        agentMode='bot',
        visibilityDistance=1,
    )

    scenes = sorted(
        [scene for scene in controller._scenes_in_build if 'physics' not in scene],
        key=key_sort_func
    )

    missing_datapoints_by_scene = {}
    partial_dataset_by_scene = {}
    for scene in scenes:
        missing_datapoints_by_scene[scene] = []
        partial_dataset_by_scene[scene] = []

    with open(dataset_path, 'r') as f:
        dataset = json.load(f)
        fill_in_dataset = create_dataset(
            ctx,
            local_build=local_build,
            editor_mode=editor_mode,
            output=output_filename,
            intermediate_directory=intermediate_dir,
            visibility_distance=visibility_distance
        )

        for datapoint in filter_dataset:
            missing_datapoints_by_scene[datapoint['scene']].append(datapoint)

        partial_dataset_filenames = sorted(glob.glob("{}/FloorPlan_*.png".format(dataset_dir)))
        print("Datas")

        difficulty_order_map = {
            'easy': 0,
            'medium': 1,
            'hard': 2
        }

        for d_filename in partial_dataset_filenames:
            with open(d_filename, 'r') as fp:
                partial_dataset = json.load(fp)
                partial_dataset[0]['scene'] = partial_dataset

        final_dataset = []
        for scene in scenes:
            for object_type in targets:
                arr = [p for p in partial_dataset[scene] if p['object_type'] == object_type] + \
                      [p for p in missing_datapoints_by_scene[scene] if p['object_type'] == object_type]
                final_dataset = final_dataset + sorted(
                    arr,
                    key=lambda p: (p['object_type'], difficulty_order_map[p['difficulty']])
                )

@task
def test_teleport(ctx, editor_mode=False, local_build=False):
    import ai2thor.controller
    import  time
    controller = ai2thor.controller.Controller(
        rotateStepDegrees=30,
        visibilityDistance=1.0,
        gridSize=0.25,
        port=8200,
        host='127.0.0.1',
        local_build=local_build,
        start_unity=False if editor_mode else True,
        agentType="stochastic",
        continuousMode=True,
        continuous=False,
        snapToGrid=False,
        agentMode="bot",
        scene="FloorPlan_Train1_2",
        width=640,
        height=480,
        continus=True
    )

    controller.step(
        action="GetReachablePositions",
        gridSize=0.25
    )
    params = {'x': 8.0, 'y': 0.924999952, 'z': -1.75, 'rotation': {'x': 0.0, 'y': 240.0, 'z': 0.0}, 'horizon': 330.0}
    evt = controller.step(
        action="TeleportFull",
        **params
    )

    print("New pos: {}".format(evt.metadata["agent"]["position"]))


@task
def resort_dataset(ctx, dataset_path, output_path, editor_mode=False, local_build = True):
    import json
    import re
    with open(dataset_path, 'r') as f:
        dataset = json.load(f)

    index = 0
    previous_index = 0
    datapoint = dataset[index]
    current_scene = datapoint['scene']
    current_object = datapoint['object_type']
    # controller.reset(current_scene)
    sum_t = 0
    new_dataset = []
    while index < len(dataset):
        previous_index = index
        while current_scene == datapoint['scene'] and current_object == datapoint['object_type']:
            index += 1
            if index > len(dataset) - 1:
                break
            datapoint = dataset[index]

        current_scene = datapoint['scene']
        current_object = datapoint['object_type']

        print("Scene '{}'...".format(current_scene))
        sorted_datapoints = sorted(
            dataset[previous_index:index],
            key=lambda dp: dp['shortest_path_length']
        )
        third = int(len(sorted_datapoints) / 3.0)
        for i, obj in enumerate(sorted_datapoints):
            if i < third:
                level = 'easy'
            elif i < 2 * third:
                level = 'medium'
            else:
                level = 'hard'
            sorted_datapoints[i]['difficulty'] = level
            m = re.search('FloorPlan_([a-zA-Z\-]*)([0-9]+)_([0-9]+)', obj['scene'])
            point_id = "{}_{}_{}_{}_{}".format(
                m.group(1),
                m.group(2),
                m.group(3),
                obj['object_type'],
                i
            )
            sorted_datapoints[i]['id'] = point_id
            sorted_datapoints[i]['difficulty'] = level
        new_dataset = new_dataset + sorted_datapoints
        sum_t += len(sorted_datapoints)

    print("original len: {}, new len: {}".format(len(dataset), sum_t))

    with open(output_path, 'w') as fw:
        json.dump(new_dataset, fw, indent=4)


@task
def remove_dataset_spaces(ctx, dataset_dir):
    import json
    train = os.path.join(dataset_dir, 'train.json')
    test = os.path.join(dataset_dir, 'val.json')

    with open(train, 'r') as f:
        train_data = json.load(f)

    with open(test, 'r') as f:
        test_data = json.load(f)

    id_set = set()
    for o in train_data:
        o['id'] = o['id'].replace(" ", '')
        id_set.add(o['id'])

    print(sorted(id_set))

    id_set = set()

    for o in test_data:
        o['id'] = o['id'].replace(" ", '')
        id_set.add(o['id'])

    print(sorted(id_set))

    with open('train.json', 'w') as fw:
        json.dump(train_data, fw, indent=4, sort_keys=True)

    with open('val.json', 'w') as fw:
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
        host='127.0.0.1',
        local_build=local_build,
        start_unity=False if editor_mode else True,
        agentType="stochastic",
        continuousMode=True,
        continuous=False,
        snapToGrid=False,
        agentMode="bot",
        scene=scene,
        width=300,
        height=300,
        continus=True
    )

    evt = metrics.get_shortest_path_to_point(
        controller,
        dict(x=x0, y=y0, z=z0),
        dict(x=x1, y=y1, z=z1)
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
        host='127.0.0.1',
        local_build=local_build,
        start_unity=False if editor_mode else True,
        agentType="stochastic",
        continuousMode=True,
        continuous=False,
        snapToGrid=False,
        agentMode="bot",
        scene=scene,
        width=300,
        height=300,
        continus=True
    )

    print("constoller.last_action Agent Pos: {}".format(controller.last_event.metadata["agent"]["position"]))

    evt = controller.step(action="GetReachablePositions", gridSize=gridSize)

    print("After GetReachable AgentPos: {}".format(evt.metadata['agent']['position']))

    print(evt.metadata["lastActionSuccess"])
    print(evt.metadata["errorMessage"])

    reachable_pos = evt.metadata["actionReturn"]

    print(evt.metadata["actionReturn"])



    evt = controller.step(
        dict(
            action="TeleportFull",
            x=3.0,
            y=reachable_pos[0]['y'],
            z=-1.5,
            rotation=dict(x=0, y=45.0, z=0),
            horizon=0.0,
        )
    )

    print("After teleport: {}".format(evt.metadata['agent']['position']))
