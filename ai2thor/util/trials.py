import numpy as np
import math


class TrialMetric(object):
    def init_trials(self, num_trials, metadata):
        ...

    def update_with_trial(self, trial_index, metadata):
        ...


class ObjectPositionVarianceAverage(TrialMetric):
    """
    Metric that computes the average of the variance of all objects in a scene across multiple runs.
    """

    def __init__(self):
        self.trials = []
        self.object_ids = []

    def init_trials(self, num_trials, metadata):
        objects = metadata["objects"]
        self.object_ids = sorted([o["objectId"] for o in objects])
        num_objects = len(self.object_ids)
        self.trials = np.empty([num_trials, num_objects, 3])

    def update_with_trial(self, trial_index, metadata):
        objects = metadata["objects"]
        object_pos_map = {
            o["objectId"]: vec_to_np_array(o["position"]) for o in objects
        }
        for object_index in range(len(self.object_ids)):
            object_id = self.object_ids[object_index]
            self.trials[trial_index][object_index] = object_pos_map[object_id]

    def compute(self, n=None):
        return np.mean(np.var(self.trials[:n], axis=0))


def vec_to_np_array(vec):
    return np.array([vec["x"], vec["y"], vec["z"]])


def trial_runner(controller, number, metric, compute_running_metric=False):
    """
    Generator that wraps metric capture from controller metadata for a number of trials
    :param controller: ai2thor controller
    :param number: int number of trials to collect metrics from
    :param metric: TrialMetric the metric to use
    :param compute_running_metric: bool whether or not to compute the metric after every trial
    :return: tuple(controller, float) with the controller and the metric after every trial
    """

    metric.init_trials(number, controller.last_event.metadata)

    for trial_index in range(number):
        try:
            yield controller, metric.compute(
                n=trial_index
            ) if compute_running_metric else math.nan
            metric.update_with_trial(trial_index, controller.last_event.metadata)
            controller.reset()
        except RuntimeError as e:
            print(
                e,
                "Last action status: {}".format(
                    controller.last_event.meatadata["actionSuccess"]
                ),
                controller.last_event.meatadata["errorMessage"],
            )
    yield controller, metric.compute()
