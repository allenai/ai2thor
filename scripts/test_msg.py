import json
import msgpack
import gzip
import os

root_dir = "./objaverse"

id = "b8d24c146a6844788c0ba6f7b135e99e"

path = os.path.join(root_dir, id, id + '.json')
out_p = os.path.join(root_dir, id, id + '.msgpack')
out_msg_gz = f"{out_p}.gz"


with open(path, "r") as f:
    obj = json.load(f)
    v = obj['vertices'][0]['x']
    print(f"v0: {v} type:{type(v)}")

    packed = msgpack.packb(obj)

    # Write msgpack file
    with gzip.open(out_msg_gz, "wb") as outfile:
        outfile.write(packed)



with gzip.open(out_msg_gz, 'rb') as f:
   unp = msgpack.unpackb(f.read())
   out_copy = os.path.join(root_dir, id, f"{id}_copy.json")
   with open(out_copy, 'w') as f_out:
    #    f_out.write(unp)
        json.dump(unp, f_out)