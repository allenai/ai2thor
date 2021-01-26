rsync  -avz \
     --exclude .idea \
     --exclude __pycache__/ \
     --exclude runs/ \
     --exclude .DS_Store \
     --exclude .direnv \
     --exclude .envrc \
     --exclude .git \
     --exclude experiment_output/ \
     --exclude docs/ \
     --exclude trained_weights/  \
     --exclude pretrained_model_ckpts/  \
     --exclude trained_weights/do_not_sync_weights/  \
     ~/git/ai2thor/ kiana-workstation:~/