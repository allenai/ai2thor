
import yaml
from pprint import pprint

from unityparser import UnityDocument

# Requires unity parser, run:
# pip install unityparser

def removeUnityTagAlias(filepath):
    """
    Name:               removeUnityTagAlias()

    Description:        Loads a file object from a Unity textual scene file, which is in a pseudo YAML style, and strips the
                        parts that are not YAML 1.1 compliant. Then returns a string as a stream, which can be passed to PyYAML.
                        Essentially removes the "!u!" tag directive, class type and the "&" file ID directive. PyYAML seems to handle
                        rest just fine after that.

    Returns:                String (YAML stream as string)  


    """
    result = str()
    sourceFile = open(filepath, 'r')

    for lineNumber,line in enumerate( sourceFile.readlines() ): 
        if line.startswith('--- !u!'):          
            result += '--- ' + line.split(' ')[2] + '\n'   # remove the tag, but keep file ID
        else:
            # Just copy the contents...
            result += line

    sourceFile.close()  

    return result

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
    testSceneNames = GetSceneNames(5, 2, "RTest")
    valSceneNames = GetSceneNames(2, 2, "RVal")
    trainSceneNames = GetSceneNames(15, 5, "Train")
    allScenes = testSceneNames + valSceneNames + trainSceneNames
    for scene_file_name in allScenes:
        updateNavMeshParamsForScene(scene_file_name)
  
    # fileToLoad = 'unity/Assets/Scenes/test.unity'
    # UnityStreamNoTags = removeUnityTagAlias(fileToLoad)

    # ListOfNodes = list()

    # for data in yaml.load_all(UnityStreamNoTags):
    #     ListOfNodes.append( data )

# Example, print each object's name and type
# for node in ListOfNodes:
#     if 'm_Name' in node[ node.keys()[0] ]:
#         print( 'Name: ' + node[ node.keys()[0] ]['m_Name']  + ' NodeType: ' + node.keys()[0] )
#     else:
#         print( 'Name: ' + 'No Name Attribute'  + ' NodeType: ' + node.keys()[0] )

#     yaml.add_multi_constructor('!', default_ctor)
#     # yaml.load(y)
    
#     with open('unity/Assets/Scenes/test.unity') as f:
#         docs = yaml.load_all(f, Loader=yaml.Loader)
#         for doc in docs:
#             for k,v in doc.items():
#                 print(k + "->" + v)
#                 print("\n")
#         # doc = yaml.load(f)
#         # pprint(doc['OcclusionCullingSettings'])

#     with open('file_to_edit.yaml', 'w') as f:
#         yaml.dump(doc, f)


if __name__== "__main__":
    main()