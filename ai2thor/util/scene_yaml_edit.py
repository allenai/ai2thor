from pprint import pprint
from unityparser import UnityDocument

# Requires unity parser, run:
# pip install unityparser


def updateNavMeshParamsForScene(scene_file_name):
    pprint("Updating file '{}'...".format(scene_file_name))
    doc = UnityDocument.load_yaml(scene_file_name)
    for entry in doc.entries:
        if entry.__class__.__name__ == "NavMeshSettings":
            # print(entry.__class__.__name__)
            buildSettings = getattr(entry, "m_BuildSettings", None)
            # pprint(buildSettings)
            buildSettings["agentRadius"] = "0.2"
            buildSettings["agentHeight"] = "1.8"
            buildSettings["agentClimb"] = "0.5"
            buildSettings["manualCellSize"] = "1"

            buildSettings["cellSize"] = "0.03"

    doc.dump_yaml()


def GetRoboSceneNames(
    last_index, last_subIndex, nameTemplate, prefix_path="unity/Assets/Scenes"
):
    return [
        "{}/FloorPlan_{}{}_{}.unity".format(prefix_path, nameTemplate, i, j)
        for i in range(1, last_index + 1)
        for j in range(1, last_subIndex + 1)
    ]


def GetSceneNames(
    start_index, last_index, nameTemplate="", prefix_path="unity/Assets/Scenes"
):
    return [
        "{}/FloorPlan{}{}_physics.unity".format(prefix_path, i, nameTemplate)
        for i in range(start_index, last_index + 1)
    ]


def main():

    # testSceneNames = GetRoboSceneNames(3, 5, "Val")
    # valSceneNames = GetRoboSceneNames(2, 2, "test-dev", "unity/Assets/Private/Scenes")
    # trainSceneNames = GetRoboSceneNames(12, 5, "Train")
    # allScenes = testSceneNames  + trainSceneNames
    # allScenes = valSceneNames

    iThorScenes = (
        GetSceneNames(1, 30)
        + GetSceneNames(201, 230)
        + GetSceneNames(301, 330)
        + GetSceneNames(401, 430)
        + GetSceneNames(501, 530)
    )
    allScenes = iThorScenes
    # print(allScenes)
    for scene_file_name in allScenes:
        updateNavMeshParamsForScene(scene_file_name)
        # print(scene_file_name)


if __name__ == "__main__":
    main()

    # Exceptions:
    # Scene FloorPlan_Train7_1
    # Train_11_3 unmade bed
    # Val2_3 unamde bed
