import pprint
import os
import datetime
import zipfile
import threading
import hashlib
import shutil
import subprocess
from invoke import task

S3_BUCKET = 'ai2-thor'
UNITY_VERSION = '2018.3.6f1'

def add_files(zipf, start_dir):
    for root, dirs, files in os.walk(start_dir):
        for f in files:
            fn = os.path.join(root, f)
            arcname = os.path.relpath(fn, start_dir)
            # print("adding %s" % arcname)
            zipf.write(fn, arcname)

def push_build(build_archive_name, archive_sha256):
    import boto3
    #subprocess.run("ls %s" % build_archive_name, shell=True)
    #subprocess.run("gsha256sum %s" % build_archive_name)
    s3 = boto3.resource('s3')
    archive_base = os.path.basename(build_archive_name)
    key = 'builds/%s' % (archive_base,)

    sha256_key = 'builds/%s.sha256' % (os.path.splitext(archive_base)[0],)

    with open(build_archive_name, 'rb') as af:
        s3.Object(S3_BUCKET, key).put(Body=af, ACL="public-read")

    s3.Object(S3_BUCKET, sha256_key).put(Body=archive_sha256, ACL="public-read", ContentType='text/plain')
    print("pushed build %s to %s" % (S3_BUCKET, build_archive_name))


def _local_build_path():
    return os.path.join(
        os.getcwd(),
        'unity/builds/thor-local-OSXIntel64.app/Contents/MacOS/thor-local-OSXIntel64'
    )


def _build(unity_path, arch, build_dir, build_name, env={}):
    project_path = os.path.join(os.getcwd(), unity_path)
    unity_hub_path = "/Applications/Unity/Hub/Editor/{}/Unity.app/Contents/MacOS/Unity".format(
        UNITY_VERSION
    )
    standalone_path = "/Applications/Unity-{}/Unity.app/Contents/MacOS/Unity".format(UNITY_VERSION)
    if os.path.exists(standalone_path):
        unity_path = standalone_path
    else:
        unity_path = unity_hub_path
    command = "%s -quit -batchmode -logFile build.log -projectpath %s -executeMethod Build.%s" % (unity_path, project_path, arch)
    target_path = os.path.join(build_dir, build_name)

    full_env = os.environ.copy()
    full_env.update(env)
    full_env['UNITY_BUILD_NAME'] = target_path
    subprocess.check_call(command, shell=True, env=full_env)

@task
def local_build(context, prefix='local', arch='OSXIntel64'):
    build_name = "thor-%s-%s" % (prefix, arch)
    fetch_source_textures(context)
    if _build('unity', arch, "builds", build_name):
        print("Build Successful")
    else:
        print("Build Failure")
    generate_quality_settings(context)

@task
def webgl_build(context, scenes, prefix='local'):
    """
    Creates a WebGL build
    :param context:
    :param scenes: String of scenes to include in the build as a comma separated list
    :param prefix: Prefix name for the build
    :return:
    """
    arch = 'WebGL'
    build_name = "thor-%s-%s" % (prefix, arch)
    fetch_source_textures(context)
    if _build('unity', arch, "builds", build_name, env=dict(SCENE=scenes)):
        print("Build Successful")
    else:
        print("Build Failure")
    generate_quality_settings(context)

@task
def generate_quality_settings(ctx):
    import yaml

    class YamlUnity3dTag(yaml.SafeLoader):
        def let_through(self, node):
            return self.construct_mapping(node)

    YamlUnity3dTag.add_constructor(u'tag:unity3d.com,2011:47', YamlUnity3dTag.let_through)

    qs = yaml.load(open('unity/ProjectSettings/QualitySettings.asset').read(), Loader=YamlUnity3dTag)

    quality_settings = {}
    default = 'Ultra'
    for i, q in enumerate(qs['QualitySettings']['m_QualitySettings']):
        quality_settings[q['name']] = i

    assert default in quality_settings

    with open("ai2thor/_quality_settings.py", "w") as f:
        f.write("# GENERATED FILE - DO NOT EDIT\n")
        f.write("DEFAULT_QUALITY = '%s'\n" % default)
        f.write("QUALITY_SETTINGS = " + pprint.pformat(quality_settings))


@task
def increment_version(context):
    import ai2thor._version

    major, minor, subv = ai2thor._version.__version__.split('.')
    subv = int(subv) + 1
    with open("ai2thor/_version.py", "w") as fi:
        fi.write("# Copyright Allen Institute for Artificial Intelligence 2017\n")
        fi.write("# GENERATED FILE - DO NOT EDIT\n")
        fi.write("__version__ = '%s.%s.%s'\n" % (major, minor, subv))

def build_sha256(path):

    m = hashlib.sha256()

    with open(path, "rb") as f:
        m.update(f.read())

    return m.hexdigest()


def build_docker(version):

    subprocess.check_call(
        "docker build --quiet --rm --no-cache -t  ai2thor/ai2thor-base:{version} .".format(version=version),
        shell=True)

    subprocess.check_call(
        "docker push ai2thor/ai2thor-base:{version}".format(version=version),
        shell=True)

@task
def build_pip(context):
    import shutil
    subprocess.check_call("python setup.py clean --all", shell=True)

    if os.path.isdir('dist'):
        shutil.rmtree("dist")

    subprocess.check_call("python setup.py sdist bdist_wheel --universal", shell=True)

@task
def fetch_source_textures(context):
    import ai2thor.downloader
    import io
    zip_data = ai2thor.downloader.download(
        "http://s3-us-west-2.amazonaws.com/ai2-thor/assets/source-textures.zip",
        "source-textures",
        "75476d60a05747873f1173ba2e1dbe3686500f63bcde3fc3b010eea45fa58de7")

    z = zipfile.ZipFile(io.BytesIO(zip_data))
    z.extractall(os.getcwd())

def archive_push(unity_path, build_path, build_dir, build_info):
    threading.current_thread().success = False
    archive_name = os.path.join(unity_path, build_path)
    zipf = zipfile.ZipFile(archive_name, 'w', zipfile.ZIP_STORED)
    add_files(zipf, os.path.join(unity_path, build_dir))
    zipf.close()

    build_info['sha256'] = build_sha256(archive_name)
    push_build(archive_name, build_info['sha256'])
    print("Build successful")
    threading.current_thread().success = True

@task
def pre_test(context):
    import ai2thor.controller
    import shutil
    c = ai2thor.controller.Controller()
    os.makedirs('unity/builds/%s' % c.build_name())
    shutil.move(os.path.join('unity', 'builds', c.build_name() + '.app'), 'unity/builds/%s' % c.build_name())


def clean():
    subprocess.check_call("git reset --hard", shell=True)
    subprocess.check_call("git clean -f -x", shell=True)
    shutil.rmtree("unity/builds", ignore_errors=True)

    if os.path.isfile('build.log'):
        os.unlink('build.log')

@task
def ci_build(context, build_dir_base, branch):
    import fcntl

    with open(".ci-build.lock", "w") as lock_f:
        fcntl.flock(lock_f, fcntl.LOCK_EX | fcntl.LOCK_NB)


        from multiprocessing import Process, Queue
        result_queue = Queue()
        procs = []
        for arch in ['OSXIntel64', 'Linux64']:
            p = Process(target=ci_build_arch, args=(build_dir_base, arch, branch, result_queue))
            print("launcching %s" % arch)
            p.start()
            procs.append(p)

        for p in procs:
            p.join()

        fcntl.flock(lock_f, fcntl.LOCK_UN)

def ci_build_arch(build_dir_base, arch, branch, result_queue):
    import subprocess
    import boto3
    import ai2thor.downloader

    github_url = "https://github.com/allenai/ai2thor"
    build_dir = os.path.join(build_dir_base, 'ai2thor-%s-%s' % (branch, arch))

    os.makedirs(build_dir_base, exist_ok=True)

    if not os.path.isdir(build_dir):
        subprocess.check_call("git clone %s %s" % (github_url, build_dir), shell=True)

    os.chdir(build_dir)
    clean()
    subprocess.check_call("git checkout %s" % branch, shell=True)
    subprocess.check_call("git pull origin %s" % branch, shell=True)
    commit_id = subprocess.check_output("git log -n 1 --format=%H", shell=True).decode('ascii').strip()

    if ai2thor.downloader.commit_build_exists(arch, commit_id):
        print("found build for commit %s %s" % (commit_id, arch))
        return

    build_url_base = 'http://s3-us-west-2.amazonaws.com/%s/' % S3_BUCKET
    unity_path = 'unity'
    build_name = "thor-%s-%s" % (arch, commit_id)
    build_dir = os.path.join('builds', build_name)
    build_path = build_dir + ".zip"
    build_info = {}

    build_info['url'] = build_url_base + build_path

    # XXX need to trap this?
    build_exception = ''
    try:
        _build(unity_path, arch, build_dir, build_name)

        print("pushing archive")
        archive_push(unity_path, build_path, build_dir, build_info)

    except Exception as e:
        build_exception = "Exception building: %s" % e

    with open("build.log") as f:
        build_log = f.read() + "\n" + build_exception

    build_log_key = 'builds/%s.log' % (build_name,)
    s3 = boto3.resource('s3')
    s3.Object(S3_BUCKET, build_log_key).put(Body=build_log, ACL="public-read", ContentType='text/plain')


@task
def poll_ci_build(context):
    from ai2thor.build import platform_map
    import ai2thor.downloader
    import time
    commit_id = subprocess.check_output("git log -n 1 --format=%H", shell=True).decode('ascii').strip()
    for i in range(10):
        missing = False
        for arch in platform_map.keys():
            if ai2thor.downloader.commit_build_log_exists(arch, commit_id):
                print("log exists %s" % commit_id)
            else:
                missing = True
        time.sleep(30)
        if not missing:
            break

    for arch in platform_map.keys():
        if not ai2thor.downloader.commit_build_exists(arch, commit_id):
            print("Build log url: %s" % ai2thor.downloader.commit_build_log_url(arch, commit_id))
            raise Exception("Failed to build %s for commit: %s " % (arch, commit_id))


@task
def build(context, local=False):
    from multiprocessing import Process
    version = datetime.datetime.now().strftime('%Y%m%d%H%M')
    build_url_base = 'http://s3-us-west-2.amazonaws.com/%s/' % S3_BUCKET

    builds = {'Docker': {'tag': version}}
    fetch_source_textures(context)
    threads = []
    dp = Process(target=build_docker, args=(version,))
    dp.start()

    #for arch in ['OSXIntel64']:
    platform_map = dict(Linux64="Linux", OSXIntel64="Darwin")

    for arch in platform_map.keys():
        unity_path = 'unity'
        build_name = "thor-%s-%s" % (version, arch)
        build_dir = os.path.join('builds', build_name)
        build_path = build_dir + ".zip"
        build_info = builds[platform_map[arch]] = {}

        build_info['url'] = build_url_base + build_path

        build(unity_path, arch, build_dir, build_name)
        t = threading.Thread(target=archive_push, args=(unity_path, build_path, build_dir, build_info))
        t.start()
        threads.append(t)

    dp.join()

    if dp.exitcode != 0:
        raise Exception("Exception with docker build")

    for t in threads:
        t.join()
        if not t.success:
            raise Exception("Error with thread")

    generate_quality_settings(context)

    with open("ai2thor/_builds.py", "w") as fi:
        fi.write("# GENERATED FILE - DO NOT EDIT\n")
        fi.write("VERSION = '%s'\n" % version)
        fi.write("BUILDS = " + pprint.pformat(builds))

    increment_version(context)
    build_pip(context)

@task
def interact(ctx, scene, editor_mode=False, local_build=False):
    import ai2thor.controller

    env = ai2thor.controller.Controller()
    if local_build:
        env.local_executable_path = _local_build_path()
    if editor_mode:
        env.start(8200, False, player_screen_width=600, player_screen_height=600)
    else:
        env.start(player_screen_width=600, player_screen_height=600)
    env.reset(scene)
    env.step(dict(action='Initialize', gridSize=0.25))
    env.interact()
    env.stop()

@task
def release(ctx):
    x = subprocess.check_output("git status --porcelain", shell=True).decode('ASCII')
    for line in x.split('\n'):
        if line.strip().startswith('??') or len(line.strip()) == 0:
            continue
        raise Exception("Found locally modified changes from 'git status' - please commit and push or revert")

    import ai2thor._version

    tag = "v" + ai2thor._version.__version__
    subprocess.check_call('git tag -a %s -m "release  %s"' % (tag, tag), shell=True)
    subprocess.check_call('git push origin master --tags', shell=True)
    subprocess.check_call('twine upload -u ai2thor dist/ai2thor-{ver}-* dist/ai2thor-{ver}.*'.format(ver=ai2thor._version.__version__), shell=True)

@task
def check_visible_objects_closed_receptacles(ctx, start_scene, end_scene):
    from itertools import product

    import ai2thor.controller
    controller = ai2thor.controller.BFSController()
    controller.local_executable_path = 'unity/builds/thor-local-OSXIntel64.app/Contents/MacOS/thor-local-OSXIntel64'
    controller.start()
    for i in range(int(start_scene), int(end_scene)):
        print("working on floorplan %s" % i)
        controller.search_all_closed('FloorPlan%s' % i)

        visibility_object_id = None
        visibility_object_types = ['Mug', 'CellPhone', 'SoapBar']
        for obj in controller.last_event.metadata['objects']:
            if obj['pickupable']:
                controller.step(action=dict(
                    action='PickupObject',
                    objectId=obj['objectId'],
                    forceVisible=True))

            if visibility_object_id is None and obj['objectType'] in visibility_object_types:
                visibility_object_id = obj['objectId']

        if visibility_object_id is None:
            raise Exception("Couldn't get a visibility_object")

        bad_receptacles = set()
        for point in controller.grid_points:
            controller.step(dict(
                action='Teleport',
                x=point['x'],
                y=point['y'],
                z=point['z']), raise_for_failure=True)

            for rot, hor in product(controller.rotations, controller.horizons):
                event = controller.step(
                    dict(action='RotateLook', rotation=rot, horizon=hor),
                    raise_for_failure=True)
                for j in event.metadata['objects']:
                    if j['receptacle'] and j['visible'] and j['openable']:

                        controller.step(
                            action=dict(
                                action='Replace',
                                forceVisible=True,
                                pivot=0,
                                receptacleObjectId=j['objectId'],
                                objectId=visibility_object_id))

                        replace_success = controller.last_event.metadata['lastActionSuccess']

                        if replace_success:
                            if controller.is_object_visible(visibility_object_id) and j['objectId'] not in bad_receptacles:
                                bad_receptacles.add(j['objectId'])
                                print("Got bad receptacle: %s" % j['objectId'])
                                # import cv2
                                # cv2.imshow('aoeu', controller.last_event.cv2image())
                                # cv2.waitKey(0)

                            controller.step(action=dict(
                                action='PickupObject',
                                objectId=visibility_object_id,
                                forceVisible=True))


@task
def benchmark(ctx, screen_width=600, screen_height=600, editor_mode=False, out='benchmark.json',
              verbose=False):
    import ai2thor.controller
    import random
    import time
    import json

    move_actions = ['MoveAhead', 'MoveBack', 'MoveLeft', 'MoveRight']
    rotate_actions = ['RotateRight', 'RotateLeft']
    look_actions = ['LookUp', 'LookDown']
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

    env = ai2thor.controller.Controller()
    env.local_executable_path = _local_build_path()
    if editor_mode:
        env.start(8200, False, player_screen_width=screen_width,
                  player_screen_height=screen_height)
    else:
        env.start(player_screen_width=screen_width, player_screen_height=screen_height)
    # Kitchens:       FloorPlan1 - FloorPlan30
    # Living rooms:   FloorPlan201 - FloorPlan230
    # Bedrooms:       FloorPlan301 - FloorPlan330
    # Bathrooms:      FloorPLan401 - FloorPlan430

    room_ranges = [(1, 30), (201, 230), (301, 330),  (401, 430)]

    benchmark_map = {'scenes': {}}
    total_average_ft = 0
    scene_count = 0
    print("Start loop")
    for room_range in room_ranges:
        for i in range(room_range[0], room_range[1]):
            scene = 'FloorPlan{}_physics'.format(i)
            scene_benchmark = {}
            if verbose:
                print("Loading scene {}".format(scene))
            # env.reset(scene)
            env.step(dict(action='Initialize', gridSize=0.25))

            if verbose:
                print("------ {}".format(scene))

            sample_number = 100
            action_tuples = [
                ('move', move_actions, sample_number),
                ('rotate', rotate_actions, sample_number),
                ('look', look_actions, sample_number),
                ('all', all_actions, sample_number)
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

            benchmark_map['scenes'][scene] = scene_benchmark
            scene_count += 1

    benchmark_map['average_framerate_seconds'] = total_average_ft / scene_count
    with open(out, 'w') as f:
        f.write(json.dumps(benchmark_map, indent=4, sort_keys=True))

    env.stop()
