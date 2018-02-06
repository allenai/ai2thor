import os
import datetime
import zipfile
from invoke import task
import hashlib
import pprint
import subprocess

S3_BUCKET='ai2-thor'

def add_files(zipf, start_dir):
    for root, dirs, files in os.walk(start_dir):
        for f in files:
            fn = os.path.join(root, f)
            arcname = os.path.relpath(fn, 'unity/builds')
            #print("adding %s" % arcname)
            zipf.write(fn, arcname)


def push_build(build_archive_name):
    import boto3
    import subprocess
    #subprocess.run("ls %s" % build_archive_name, shell=True)
    #subprocess.run("gsha256sum %s" % build_archive_name)
    s3 = boto3.resource('s3')
    key = 'builds/%s' % (os.path.basename(build_archive_name),)

    s3.Object(S3_BUCKET, key).put(Body=open(build_archive_name, 'rb'), ACL="public-read")
    print("pushed build %s to %s" % (S3_BUCKET, build_archive_name))


def _build(context, arch, build_name):
    project_path = os.path.join(os.getcwd(), 'unity')
    command = "/Applications/Unity/Unity.app/Contents/MacOS/Unity -quit -batchmode -logFile build.log -projectpath %s -executeMethod Build.%s" % (project_path, arch)
    return context.run(command, warn=True, env=dict(UNITY_BUILD_NAME=build_name))

@task
def local_build(context, prefix='local'):
    arch = 'OSXIntel64'
    build_name = "builds/thor-%s-%s" % (prefix, arch)
    if _build(context, arch, build_name):
        print("Build Successful")
    else:
        print("Build Failure")

def build_sha256(path):

    m = hashlib.sha256()

    with open(path, "rb") as f:
        m.update(f.read())

    return m.hexdigest()

def generate_dockerignore(version):
    return """ai2thor/
ai2thor.egg-info/
unity/
build/
dist/
doc/
!unity/builds/thor-%s-Linux64*
""" % version


def build_docker(version):
    with open(".dockerignore", "w") as f:
        f.write(generate_dockerignore(version))

    subprocess.check_call(
        "docker build --rm --no-cache --build-arg AI2THOR_VERSION={version} -t  ai2thor/ai2thor-base:{version} .".format(version=version),
        shell=True)

    subprocess.check_call(
        "docker push ai2thor/ai2thor-base:{version}".format(version=version),
        shell=True)

@task
def build(context, local=False):
    version = datetime.datetime.now().strftime('%Y%m%d%H%M')
    build_url_base = 'http://s3-us-west-2.amazonaws.com/%s/' % S3_BUCKET

    builds = {'Docker': {'tag': version}}

    for arch in ['OSXIntel64', 'Linux64']:
        build_name = "builds/thor-%s-%s" % (version, arch)
        url = build_url_base + build_name + ".zip"

        x = _build(context, arch, build_name)

        build_info = None
        if x:
            archive_name = os.path.join('unity', build_name + ".zip")
            zipf = zipfile.ZipFile(archive_name, 'w', zipfile.ZIP_DEFLATED)
            if arch == 'OSXIntel64':
                add_files(zipf, os.path.join('unity', build_name + ".app"))
                build_info = builds['Darwin'] = {}
            elif arch == 'Linux64':
                build_info = builds['Linux'] = {}
                add_files(zipf, os.path.join('unity', build_name + "_Data"))
                zipf.write(os.path.join('unity', build_name), os.path.basename(build_name))
            build_info['url'] = url
            zipf.close()

            build_info['sha256'] = build_sha256(archive_name)
            push_build(archive_name)
            print("Build successful")
        else:
            raise Exception("Build Failure")

        with open("ai2thor/_builds.py", "w") as fi:
            fi.write("# GENERATED FILE - DO NOT EDIT\n")
            fi.write("VERSION = '%s'\n" % version)
            fi.write("BUILDS = " + pprint.pformat(builds))

    build_docker(version)
