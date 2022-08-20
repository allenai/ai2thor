using UnityEngine;

/// <summary>
/// This is a helper component. When added to a GameObject that represents a scene entity,
/// such as a floor, ceiling, or desk, this component generates a mesh from its boundary vertices.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class OVRPlaneMeshFilter : MonoBehaviour
{
	private MeshFilter _meshFilter;
	private Mesh _mesh;

	private void Start()
	{
		_mesh = new Mesh();
		_meshFilter = GetComponent<MeshFilter>();
		_meshFilter.sharedMesh = _mesh;

		CreateMeshFromBoundary();
	}

	private void CreateMeshFromBoundary()
	{
		var sceneAnchor = GetComponent<OVRSceneAnchor>();
		if (sceneAnchor == null ||
			!OVRPlugin.GetSpaceBoundary2D(sceneAnchor.Space, out var boundaryVertices))
		{
			return;
		}

		_mesh.name = $"OVRPlaneMeshFilter {sceneAnchor.Space}";
		OVRMeshGenerator.GenerateMesh(boundaryVertices, _mesh);
	}
}
