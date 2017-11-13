import boto3
from invoke import task
import os
import datetime
import zipfile

S3_BUCKET='ai2-vision-robosims'

def add_files(zipf, start_dir):
    for root, dirs, files in os.walk(start_dir):
        for f in files:
            fn = os.path.join(root, f)
            arcname = os.path.relpath(fn, 'builds')
            #print("adding %s" % arcname)
            zipf.write(fn, arcname)


def push_build(build_archive_name):

    s3 = boto3.resource('s3')
    key = 'builds/%s' % (os.path.basename(build_archive_name),)

    s3.Object(S3_BUCKET, key).put(Body=open(build_archive_name, 'rb'), ACL="public-read")
    print("pushed build %s to %s" % (S3_BUCKET, build_archive_name))


def _build(context, arch, build_name):
    project_path = os.getcwd()
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

@task
def build(context, local=False):
    timestamp = datetime.datetime.now().strftime('%Y%m%d%H%M')

    for arch in ['OSXIntel64', 'Linux64']:
        build_name = "builds/thor-%s-%s" % (timestamp, arch)

        x = _build(context, arch, build_name)

        if x:
            archive_name = build_name + ".zip"
            zipf = zipfile.ZipFile(archive_name, 'w', zipfile.ZIP_DEFLATED)
            if arch == 'OSXIntel64':
                add_files(zipf, build_name + ".app")
            elif arch == 'Linux64':
                add_files(zipf, build_name + "_Data")
                zipf.write(build_name, os.path.basename(build_name))

            zipf.close()
            push_build(archive_name)
            print("Build successful")
        else:
            print("Build Failure")
