"""
COCO 2017 Train

can generate the lists by calling 
MetadataCatalog.get(cfg.DATASETS.TEST[0]).thing_classes

where
cfg.DATASETS is defined as 
CfgNode({'TRAIN': ('coco_2017_train',), 'PROPOSAL_FILES_TRAIN': (), 'PRECOMPUTED_PROPOSAL_TOPK_TRAIN': 2000, 'TEST': ('coco_2017_val',), 'PROPOSAL_FILES_TEST': (), 'PRECOMPUTED_PROPOSAL_TOPK_TEST': 1000})

"""

ms_coco_to_synset = {
    0: 'person.n.01',
    1: 'bicycle.n.01',
    2: 'car.n.01',
    3: 'motorcycle.n.01',
    4: 'airplane.n.01',
    5: 'bus.n.01',
    6: 'train.n.01',
    7: 'truck.n.01',
    8: 'boat.n.01',
    9: 'traffic_light.n.01',
    10: 'fire_hydrant.n.01',
    11: 'stop_sign.n.01',
    12: 'parking_meter.n.01',
    13: 'bench.n.01',
    14: 'bird.n.01',
    15: 'cat.n.01',
    16: 'dog.n.01',
    17: 'horse.n.01',
    18: 'sheep.n.01',
    19: 'cow.n.01',
    20: 'elephant.n.01',
    21: 'bear.n.01',
    22: 'zebra.n.01',
    23: 'giraffe.n.01',
    24: 'backpack.n.01',
    25: 'umbrella.n.01',
    26: 'handbag.n.01',
    27: 'tie.n.01',
    28: 'suitcase.n.01',
    29: 'frisbee.n.01',
    30: 'ski.n.01',
    31: 'snowboard.n.01',
    32: 'sports_equipment.n.01', # sports_ball in coco dataset
    33: 'kite.n.01',
    34: 'baseball_bat.n.01',
    35: 'baseball_glove.n.01',
    36: 'skateboard.n.01',
    37: 'surfboard.n.01',
    38: 'tennis_racket.n.01',
    39: 'bottle.n.01',
    40: 'wineglass.n.01',
    41: 'cup.n.01',
    42: 'fork.n.01',
    43: 'knife.n.01',
    44: 'spoon.n.01',
    45: 'bowl.n.01',
    46: 'banana.n.01',
    47: 'apple.n.01',
    48: 'sandwich.n.01',
    49: 'orange.n.01',
    50: 'broccoli.n.01',
    51: 'carrot.n.01',
    52: 'hotdog.n.01',
    53: 'pizza.n.01',
    54: 'doughnut.n.01', # donut in coco 
    55: 'cake.n.01',
    56: 'chair.n.01',
    57: 'sofa.n.01', # couch in coco
    58: 'potted_plant.n.01',
    59: 'bed.n.01',
    60: 'dining_table.n.01',
    61: 'toilet.n.01',
    62: 'television.n.01',
    63: 'laptop.n.01',
    64: 'mouse.n.01',
    65: 'remote_control.n.01',
    66: 'keyboard.n.01',
    67: 'cellular_telephone.n.01',
    68: 'microwave.n.01',
    69: 'oven.n.01',
    70: 'toaster.n.01',
    71: 'sink.n.01',
    72: 'refrigerator.n.01',
    73: 'book.n.01',
    74: 'clock.n.01',
    75: 'vase.n.01',
    76: 'scissors.n.01',
    77: 'teddy.n.01',
    78: 'hair_dryer.n.01',
    79: 'toothbrush.n.01',
}

synset_to_ms_coco = {v: k for k, v in ms_coco_to_synset.items()}