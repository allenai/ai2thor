
import yaml
from pprint import pprint
from unityparser import UnityDocument

# Requires unity parser, run:
# pip install unityparser

def updateNavMeshParamsForScene(scene_file_name):
    pprint("Updating file '{}'...".format(scene_file_name))
    doc = UnityDocument.load_yaml(scene_file_name)
    for entry in doc.entries:
        if entry.__class__.__name__ == 'NavMeshSettings':
            # print(entry.__class__.__name__)
            buildSettings = getattr(entry, 'm_BuildSettings', None)
            # pprint(buildSettings)
            buildSettings['agentRadius'] = '0.175'
            buildSettings['agentHeight'] = '1.1'
            buildSettings['agentClimb'] = '0.5'
            buildSettings['manualCellSize'] = '0'
            buildSettings['cellSize'] = '0.058333334'

    doc.dump_yaml()


def GetSceneNames(last_index, last_subIndex, nameTemplate):
    return ["unity/Assets/Scenes/FloorPlan_{}{}_{}.unity".format(nameTemplate,i, j)  for i in range(1, last_index+1) for j in range(1, last_subIndex+1)]


def main():
    testSceneNames = GetSceneNames(3, 5, "Val")
    valSceneNames = GetSceneNames(2, 2, "test-dev")
    trainSceneNames = GetSceneNames(14, 5, "Train")
    allScenes = testSceneNames + valSceneNames + trainSceneNames
    for scene_file_name in allScenes:
        updateNavMeshParamsForScene(scene_file_name)


if __name__== "__main__":
    main()