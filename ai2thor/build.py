from aws_requests_auth.boto_utils import BotoAWSRequestsAuth
import os
import requests
import platform
from ai2thor.util import makedirs
import ai2thor.downloader
import fcntl
import zipfile
import logging
import io

logger = logging.getLogger(__name__)

PUBLIC_S3_BUCKET = "ai2-thor-public"
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


class ExternalBuild(object):
    def __init__(self, executable_path):
        self.executable_path = executable_path

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
        if releases_dir:
            self.tmp_dir = os.path.normpath(self.releases_dir + "/../tmp")

    def download(self):

        if platform.architecture()[0] != '64bit':
            raise Exception("Only 64bit currently supported")

        makedirs(self.releases_dir)
        makedirs(self.tmp_dir)
        download_lf = os.open(os.path.join(self.tmp_dir, self.name + ".lock"), os.O_RDWR | os.O_CREAT)
        try:
            fcntl.lockf(download_lf, fcntl.LOCK_EX)

            if not os.path.isfile(self.executable_path):
                zip_data = ai2thor.downloader.download(
                    self.url,
                    self.sha256(),
                    self.include_private_scenes)

                z = zipfile.ZipFile(io.BytesIO(zip_data))
                # use tmpdir instead or a random number
                extract_dir = os.path.join(self.tmp_dir, self.name)
                logger.debug("Extracting zipfile %s" % os.path.basename(self.url))
                z.extractall(extract_dir)
                os.rename(extract_dir, os.path.join(self.releases_dir, self.name))
                # we can lose the executable permission when unzipping a build
                os.chmod(self.executable_path, 0o755)
            else:
                logger.debug("%s exists - skipping download" % self.executable_path)

        finally:
            fcntl.lockf(download_lf, fcntl.LOCK_UN)
            os.close(download_lf)

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
        if self._lock_file:
            fcntl.lockf(self._lock_file, fcntl.LOCK_UN)
            os.close(self._lock_file)

    def _lock(self, mode):
        build_dir = os.path.join(self.releases_dir, self.name)
        self._lock_file = os.open(build_dir + ".lock", os.O_RDWR | os.O_CREAT)
        fcntl.lockf(self._lock_file, mode)

    def lock_sh(self):
        self._lock(fcntl.LOCK_SH)
        build_dir = os.path.join(self.releases_dir, self.name)
        self._lock_file = os.open(build_dir + ".lock", os.O_RDWR | os.O_CREAT)
        fcntl.lockf(self._lock_file, fcntl.LOCK_SH)

    @property
    def log_url(self):
        return os.path.splitext(self.url)[0] + '.log'

    @property
    def sha256_url(self):
        return os.path.splitext(self.url)[0] + '.sha256'

    def exists(self):
        return requests.head(self.url, auth=self.auth()).status_code == 200

    def log_exists(self):
        return requests.head(self.log_url, auth=self.auth()).status_code == 200

    def sha256(self):
        res = requests.get(self.sha256_url, auth=self.auth())
        res.raise_for_status()
        return res.content.decode('ascii')
