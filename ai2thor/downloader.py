# Copyright Allen Institute for Artificial Intelligence 2017
import requests
from progressbar import ProgressBar, Bar, Percentage, FileTransferSpeed
import hashlib
import logging

logger = logging.getLogger(__name__)

def download(url, build_name, sha256_digest):
    logger.debug("Downloading file from %s" % url)
    r = requests.get(url, stream=True)
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
