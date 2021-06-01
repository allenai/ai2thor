# Copyright Allen Institute for Artificial Intelligence 2017
import requests
from progressbar import ProgressBar, Bar, Percentage, FileTransferSpeed
import hashlib
import logging
import ai2thor.build

logger = logging.getLogger(__name__)


def download(url, sha256_digest, include_private_scenes=False):

    auth = None
    if include_private_scenes:
        auth = ai2thor.build.boto_auth()

    logger.debug("Downloading file from %s" % url)
    r = requests.get(url, stream=True, auth=auth)
    r.raise_for_status()
    size = int(r.headers["Content-Length"].strip())
    total_bytes = 0

    widgets = [
        url.split("/")[-1],
        ": ",
        Bar(marker="|", left="[", right=" "),
        Percentage(),
        " ",
        FileTransferSpeed(),
        "]  of {0}MB".format(str(round(size / 1024 / 1024, 2))[:4]),
    ]

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

    return b"".join(file_data)
