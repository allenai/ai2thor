from aws_requests_auth.boto_utils import BotoAWSRequestsAuth
import os
import requests
import platform
import json
from ai2thor.util import makedirs
import time
import random
import ai2thor.downloader
import zipfile
import logging
from ai2thor.util.lock import LockSh, LockEx
from ai2thor.util import atomic_write
import io

logger = logging.getLogger(__name__)

PUBLIC_S3_BUCKET = "ai2-thor-public"
PUBLIC_WEBGL_S3_BUCKET = "ai2-thor-webgl-public"
PRIVATE_S3_BUCKET = "ai2-thor-private"

COMMIT_ID = None
try:
    from ai2thor._builds import COMMIT_ID
except ImportError:
    pass

platform_map = dict(Linux64="Linux", OSXIntel64="Darwin")
arch_platform_map = {v: k for k, v in platform_map.items()}


def build_name(arch, commit_id, include_private_scenes=False):
    if include_private_scenes:
        return "thor-private-%s-%s" % (arch, commit_id)
    else:
        return "thor-%s-%s" % (arch, commit_id)


def boto_auth():
    return BotoAWSRequestsAuth(aws_host='s3-us-west-2.amazonaws.com', aws_region='us-west-2', aws_service='s3')


base_url = "http://s3-us-west-2.amazonaws.com/%s/" % PUBLIC_S3_BUCKET
private_base_url = "http://s3-us-west-2.amazonaws.com/%s/" % PRIVATE_S3_BUCKET


# dummy build when connecting to the editor
class EditorBuild(object):
    def __init__(self):
        # assuming that an external build supports both server types
        self.server_types = ['FIFO', 'WSGI']
        self.url = None
        self.unity_proc = None

    def download(self):
        pass

    def unlock(self):
        pass

    def lock_sh(self):
        pass

class ExternalBuild(object):
    def __init__(self, executable_path):
        self.executable_path = executable_path

        # assuming that an external build supports both server types
        self.server_types = ['FIFO', 'WSGI']

    def download(self):
        pass

    def unlock(self):
        pass

    def lock_sh(self):
        pass


class Build(object):

    def __init__(self, arch, commit_id, include_private_scenes, releases_dir=None):

        self.arch = arch
        self.commit_id = commit_id
        self.include_private_scenes = include_private_scenes
        self.releases_dir = releases_dir
        self.tmp_dir = None

        if self.releases_dir:
            self.tmp_dir = os.path.normpath(self.releases_dir + "/../tmp")

    def download(self):

        if platform.architecture()[0] != '64bit':
            raise Exception("Only 64bit currently supported")

        makedirs(self.releases_dir)
        makedirs(self.tmp_dir)

        with LockEx(os.path.join(self.tmp_dir, self.name)):
            if not os.path.isfile(self.executable_path):
                z = self.zipfile()
                # use tmpdir instead or a random number
                extract_dir = os.path.join(self.tmp_dir, self.name)
                logger.debug("Extracting zipfile %s" % os.path.basename(self.url))
                z.extractall(extract_dir)

                os.rename(extract_dir, os.path.join(self.releases_dir, self.name))
                # This can be removed after migrating OSXIntel64 builds to have the AI2Thor executable
                if os.path.exists(self.old_executable_path) and not os.path.exists(self.executable_path):
                    os.link(self.old_executable_path, self.executable_path)
                
                # we can lose the executable permission when unzipping a build
                os.chmod(self.executable_path, 0o755)

            else:
                logger.debug("%s exists - skipping download" % (self.executable_path,))

    def zipfile(self):
        zip_data = ai2thor.downloader.download(
            self.url,
            self.sha256(),
            self.include_private_scenes)

        return zipfile.ZipFile(io.BytesIO(zip_data))

    def download_metadata(self):
        # this can happen if someone has an existing release without the metadata 
        # built prior to the backfill
        # can add check to see if metadata has expired/we update metadata
        # if we want to add more info to metadata
        if not self.metadata:
            z = self.zipfile()
            atomic_write(self.metadata_path, z.read('metadata.json'))

    @property
    def old_executable_path(self):
        target_arch = platform.system()
        if target_arch == 'Linux':
            return self.executable_path
        elif target_arch == 'Darwin':
            return os.path.join(
                self.releases_dir,
                self.name,
                self.name + ".app",
                "Contents/MacOS",
                self.name)
        else:
            raise Exception('unable to handle target arch %s' % target_arch)

    @property
    def executable_path(self):
        target_arch = platform.system()

        if target_arch == 'Linux':
            return os.path.join(self.releases_dir, self.name, self.name)
        elif target_arch == 'Darwin':
            return os.path.join(
                self.releases_dir,
                self.name,
                self.name + ".app",
                "Contents/MacOS",
                "AI2-Thor")
        else:
            raise Exception('unable to handle target arch %s' % target_arch)
    
    @property
    def metadata_path(self):
        return os.path.join(self.releases_dir, self.name, "metadata.json")

    @property
    def metadata(self):
        if os.path.isfile(self.metadata_path):
            with open(self.metadata_path, "r") as f:
                return json.loads(f.read())
        else:
            return None

    @property
    def server_types(self):
        self.download_metadata()
        return self.metadata.get('server_types', [])

    def auth(self):
        if self.include_private_scenes:
            return boto_auth()
        else:
            return None

    def _base_url(self):
        if self.include_private_scenes:
            return private_base_url
        else:
            return base_url

    @property
    def url(self):
        return self._base_url() + os.path.join('builds', self.name + ".zip")

    @property
    def name(self):
        return build_name(self.arch, self.commit_id, self.include_private_scenes)

    def unlock(self):
        if self._lock:
            self._lock.unlock()
            self._lock = None

    def lock_sh(self):
        self._lock = LockSh(os.path.join(self.releases_dir, self.name))
        self._lock.lock()

    @property
    def log_url(self):
        return os.path.splitext(self.url)[0] + '.log'

    @property
    def metadata_url(self):
        return os.path.splitext(self.url)[0] + '.json'

    @property
    def sha256_url(self):
        return os.path.splitext(self.url)[0] + '.sha256'

    def exists(self):
        x = requests.head(self.url, auth=self.auth())
        return x.status_code == 200

    def log_exists(self):
        return requests.head(self.log_url, auth=self.auth()).status_code == 200

    def sha256(self):
        res = requests.get(self.sha256_url, auth=self.auth())
        res.raise_for_status()
        return res.content.decode('ascii')
