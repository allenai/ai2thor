import pdb, os
import datetime
import cv2
def save_first_frame(controller, image_path):
    frame = controller.last_event.frame
    cv2.imwrite(image_path, frame[:,:,[2,1,0]])
def save_failed_sequence(controller, sequence, scene_name, image_path='/Users/kianae/Desktop/bug_visualization'):
    now = datetime.datetime.now()
    time_str = now.strftime("%m_%d_%Y_%H_%M_%S_%f")
    image_path = os.path.join(image_path, time_str)
    os.makedirs(image_path, exist_ok=True)
    print('saving in ', image_path)
    with open(os.path.join(image_path, 'sequence_action.txt'), 'w') as f:
        f.write('scene ' + scene_name + '\n')
        f.write(str(sequence))
        f.write('\n')


    controller.reset(scene=scene_name)
    for (i, seq) in enumerate(sequence):
        event = controller.step(seq)
        success = event.metadata['lastActionSuccess']
        event_type = seq['action']
        this_path = os.path.join(image_path, 'img_{}_action_{}_success_{}.png'.format(i, event_type, success))
        save_first_frame(controller, this_path)
