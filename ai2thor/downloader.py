import requests
from progressbar import ProgressBar, Bar, Percentage, FileTransferSpeed

file_url = "https://s3-us-west-2.amazonaws.com/ai2-vision-robosims/builds/thor-201706291201-OSXIntel64.zip"
r = requests.get(file_url, stream=True)
print(r.headers)
#size = int(r.headers['Content-Length'].strip())
#total_bytes = 0
#widgets = ["some name", ": ", Bar(marker="|", left="[", right=" "),
#    Percentage(), " ",  FileTransferSpeed(), "]  of {0}MB".format(str(round(size / 1024 / 1024, 2))[:4])]
#pbar = ProgressBar(widgets=widgets, maxval=size).start()
#file = []
#for buf in r.iter_content(1024):
#    if buf:
#        file.append(buf)
#        total_bytes += len(buf)
#        pbar.update(total_bytes)
#pbar.finish()
