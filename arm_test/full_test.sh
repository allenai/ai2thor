#export COMMIT_ID="805c9ade85d0f0ff7d91a818dbb6a5c99db3180e"
#export COMMIT_ID="30fa18fdcd0ab828d6efccc5dbfdcc37bbcbb727"; export TEST_DETERMINISM_NAME="30fa18f_determinism.json"
export COMMIT_ID="12a36839a9670e70ffe6f2171212147ce306d818"; export TEST_DETERMINISM_NAME="12a3683_determinism.json"
python3 test_action_success_and_movement_values.py --commit_id $COMMIT_ID
python3 test_pickup_and_move_arm_stuck.py --commit_id $COMMIT_ID
python3 test_determinism.py --commit_id $COMMIT_ID
python3 test_determinism_different_machines.py --commit_id $COMMIT_ID --generate_test --number_of_test 100 --test_file_name "${TEST_DETERMINISM_NAME}_big.json"
python3 test_nan_inf_issues.py --commit_id $COMMIT_ID
python3 test_pickup_and_move_arm_stuck.py --commit_id $COMMIT_ID
python3 test_determinism_different_machines.py --commit_id $COMMIT_ID --test_file_name "${TEST_DETERMINISM_NAME}_big.json"