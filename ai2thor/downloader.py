# Copyright Allen Institute for Artificial Intelligence 2017
import requests
from progressbar import ProgressBar, Bar, Percentage, FileTransferSpeed
import hashlib
import logging
import os

logger = logging.getLogger(__name__)
base_url = "http://s3-us-west-2.amazonaws.com/ai2-thor/"

def commit_build_url(arch, commit_id):
    return base_url + os.path.join('builds', "thor-%s-%s.zip" % (arch, commit_id))

def commit_build_log_url(arch, commit_id):
    return os.path.splitext(commit_build_url(arch, commit_id))[0] + '.log'

def commit_build_sha256_url(arch, commit_id):
    return os.path.splitext(commit_build_url(arch, commit_id))[0] + '.sha256'

def commit_build_exists(arch, commit_id):
    return requests.head(commit_build_url(arch, commit_id)).status_code == 200

def commit_build_log_exists(arch, commit_id):
    return requests.head(commit_build_log_url(arch, commit_id)).status_code == 200

def commit_build_sha256(arch, commit_id):
    res = requests.get(commit_build_sha256_url(arch, commit_id))
    res.raise_for_status()
    return res.content.decode('ascii')


def download(url, build_name, sha256_digest):
    logger.debug("Downloading file from %s" % url)
    r = requests.get(url, stream=True)
    r.raise_for_status()
    size = int(r.headers['Content-Length'].strip())
    total_bytes = 0

    widgets = [
        build_name, ": ", Bar(marker="|", left="[", right=" "),
        Percentage(), " ", FileTransferSpeed(), "]  of {0}MB".format(str(round(size / 1024 / 1024, 2))[:4])]

    pbar = ProgressBar(widgets=widgets, maxval=size).start()
    m = hashlib.sha256()
    file_data = []
    for buf in r.iter_content(1024):
        if buf:
            file_data.append(buf)
            m.update(buf)
            total_bytes += len(buf)
            pbar.update(total_bytes)
    pbar.finish()
    if m.hexdigest() != sha256_digest:
        raise Exception("Digest mismatch for url %s" % url)

    return b''.join(file_data)
