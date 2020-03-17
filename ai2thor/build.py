from aws_requests_auth.boto_utils import BotoAWSRequestsAuth
import os
import requests

VERSION = None
try:
    from ai2thor._builds import VERSION
except ImportError:
    pass

platform_map = dict(Linux64="Linux", OSXIntel64="Darwin")

arch_platform_map = {v: k for k, v in platform_map.items()}


def boto_auth():
    return BotoAWSRequestsAuth(aws_host='s3-us-west-2.amazonaws.com',
            aws_region='us-west-2',
            aws_service='s3')

def build_name(arch, commit_id, include_private_scenes=False):
    if include_private_scenes:
        return "thor-private-%s-%s" % (arch, commit_id)
    else:
        return "thor-%s-%s" % (arch, commit_id)

base_url = "http://s3-us-west-2.amazonaws.com/ai2-thor/"
private_base_url = "http://s3-us-west-2.amazonaws.com/ai2-thor-private/"

class Build(object):

    def __init__(self, arch, commit_id, include_private_scenes):

        self.arch = arch
        self.commit_id = commit_id
        self.include_private_scenes = include_private_scenes

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

    def url(self):
        return self._base_url() + os.path.join('builds', build_name(self.arch, self.commit_id, self.include_private_scenes) + ".zip")

    def log_url(self):
        return os.path.splitext(self.url())[0] + '.log'

    def sha256_url(self):
        return os.path.splitext(self.url())[0] + '.sha256'

    def exists(self):
        return requests.head(self.url(), auth=self.auth()).status_code == 200

    def log_exists(self):
        return requests.head(self.log_url(), auth=self.auth()).status_code == 200

    def sha256(self):
        res = requests.get(self.sha256_url(), auth=self.auth())
        res.raise_for_status()
        return res.content.decode('ascii')
