import pprint
import os
import datetime
import zipfile
import threading
import hashlib
import subprocess
from invoke import task

S3_BUCKET = 'ai2-thor'

def add_files(zipf, start_dir):
    for root, dirs, files in os.walk(start_dir):
        for f in files:
            fn = os.path.join(root, f)
            arcname = os.path.relpath(fn, start_dir)
            # print("adding %s" % arcname)
            zipf.write(fn, arcname)

def push_build(build_archive_name):
    import boto3
    #subprocess.run("ls %s" % build_archive_name, shell=True)
    #subprocess.run("gsha256sum %s" % build_archive_name)
    s3 = boto3.resource('s3')
    key = 'builds/%s' % (os.path.basename(build_archive_name),)

    s3.Object(S3_BUCKET, key).put(Body=open(build_archive_name, 'rb'), ACL="public-read")
    print("pushed build %s to %s" % (S3_BUCKET, build_archive_name))


def _build(context, arch, build_dir, build_name):
    project_path = os.path.join(os.getcwd(), 'unity')
    command = "/Applications/Unity-2017.3.1f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -logFile build.log -projectpath %s -executeMethod Build.%s" % (project_path, arch)
    target_path = os.path.join(build_dir, build_name)

    return context.run(command, warn=True, env=dict(UNITY_BUILD_NAME=target_path))

@task
def local_build(context, prefix='local'):
    arch = 'OSXIntel64'
    build_name = "thor-%s-%s" % (prefix, arch)
    fetch_source_textures(context)
    if _build(context, arch, "builds", build_name):
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

def archive_push(build_path, build_dir, build_info):
    threading.current_thread().success = False
    archive_name = os.path.join('unity', build_path)
    zipf = zipfile.ZipFile(archive_name, 'w', zipfile.ZIP_STORED)
    add_files(zipf, os.path.join('unity', build_dir))
    zipf.close()

    build_info['sha256'] = build_sha256(archive_name)
    push_build(archive_name)
    print("Build successful")
    threading.current_thread().success = True

@task
def pre_test(context):
    import ai2thor.controller
    import shutil
    c = ai2thor.controller.Controller()
    os.makedirs('unity/builds/%s' % c.build_name())
    shutil.move(os.path.join('unity', 'builds', c.build_name() + '.app'), 'unity/builds/%s' % c.build_name())

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
        build_name = "thor-%s-%s" % (version, arch)
        build_dir = os.path.join('builds', build_name)
        build_path = build_dir + ".zip"
        build_info = builds[platform_map[arch]] = {}

        build_info['url'] = build_url_base + build_path

        x = _build(context, arch, build_dir, build_name)

        if x:
            t = threading.Thread(target=archive_push, args=(build_path, build_dir, build_info))
            t.start()
            threads.append(t)
        else:
            raise Exception("Build Failure")

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
    build_docker(version)
    build_pip(context)

@task
def interact(ctx, scene, editor_mode=False):
    import ai2thor.controller

    env = ai2thor.controller.Controller()
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
