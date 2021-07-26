using UnityEngine;

namespace Parabox.CSG.Demo
{
	/// <summary>
	/// Simple demo of CSG operations.
	/// </summary>
	public class Demo : MonoBehaviour
	{
		GameObject left, right, composite;
		bool wireframe = false;

		public Material wireframeMaterial = null;

		public GameObject[] fodder; // prefabs containing two mesh children
		int index = 0; // the index of example mesh prefabs

		enum BoolOp
		{
			Union,
			SubtractLR,
			SubtractRL,
			Intersect
		};

		void Awake()
		{
			Reset();

			wireframeMaterial.SetFloat("_Opacity", 0);
			cur_alpha = 0f;
			dest_alpha = 0f;

			ToggleWireframe();
		}

		/// <summary>
		/// Reset the scene to it's original state.
		/// </summary>
		public void Reset()
		{
			if (composite) Destroy(composite);
			if (left) Destroy(left);
			if (right) Destroy(right);

			var go = Instantiate(fodder[index]);

			left = Instantiate(go.transform.GetChild(0).gameObject);
			right = Instantiate(go.transform.GetChild(1).gameObject);

			Destroy(go);

			wireframeMaterial = left.GetComponent<MeshRenderer>().sharedMaterial;

			GenerateBarycentric(left);
			GenerateBarycentric(right);
		}

		public void Union()
		{
			Reset();
			DoBooleanOperation(BoolOp.Union);
		}

		public void SubtractionLR()
		{
			Reset();
			DoBooleanOperation(BoolOp.SubtractLR);
		}

		public void SubtractionRL()
		{
			Reset();
			DoBooleanOperation(BoolOp.SubtractRL);
		}

		public void Intersection()
		{
			Reset();
			DoBooleanOperation(BoolOp.Intersect);
		}

		void DoBooleanOperation(BoolOp operation)
		{
			CSG_Model result;

			/**
			 * All boolean operations accept two gameobjects and return a new mesh.
			 * Order matters - left, right vs. right, left will yield different
			 * results in some cases.
			 */
			switch (operation)
			{
				case BoolOp.Union:
					result = Boolean.Union(left, right);
					break;

				case BoolOp.SubtractLR:
					result = Boolean.Subtract(left, right);
					break;

				case BoolOp.SubtractRL:
					result = Boolean.Subtract(right, left);
					break;

				default:
					result = Boolean.Intersect(right, left);
					break;
			}

			composite = new GameObject();
			composite.AddComponent<MeshFilter>().sharedMesh = result.mesh;
			composite.AddComponent<MeshRenderer>().sharedMaterials = result.materials.ToArray();

			GenerateBarycentric(composite);

			Destroy(left);
			Destroy(right);
		}

		/// <summary>
		/// Turn the wireframe overlay on or off.
		/// </summary>
		public void ToggleWireframe()
		{
			wireframe = !wireframe;

			cur_alpha = wireframe ? 0f : 1f;
			dest_alpha = wireframe ? 1f : 0f;
			start_time = Time.time;
		}

		/// <summary>
		/// Swap the current example meshes
		/// </summary>
		public void ToggleExampleMeshes()
		{
			index++;
			if (index > fodder.Length - 1) index = 0;

			Reset();
		}

		float wireframe_alpha = 0f, cur_alpha = 0f, dest_alpha = 1f, start_time = 0f;

		void Update()
		{
			wireframe_alpha = Mathf.Lerp(cur_alpha, dest_alpha, Time.time - start_time);
			wireframeMaterial.SetFloat("_Opacity", wireframe_alpha);
		}

		/**
		 * Rebuild mesh with individual triangles, adding barycentric coordinates
		 * in the colors channel.  Not the most ideal wireframe implementation,
		 * but it works and didn't take an inordinate amount of time :)
		 */
		void GenerateBarycentric(GameObject go)
		{
			Mesh m = go.GetComponent<MeshFilter>().sharedMesh;

			if (m == null) return;

			int[] tris = m.triangles;
			int triangleCount = tris.Length;

			Vector3[] mesh_vertices = m.vertices;
			Vector3[] mesh_normals = m.normals;
			Vector2[] mesh_uv = m.uv;

			Vector3[] vertices = new Vector3[triangleCount];
			Vector3[] normals = new Vector3[triangleCount];
			Vector2[] uv = new Vector2[triangleCount];
			Color[] colors = new Color[triangleCount];

			for (int i = 0; i < triangleCount; i++)
			{
				vertices[i] = mesh_vertices[tris[i]];
				normals[i] = mesh_normals[tris[i]];
				uv[i] = mesh_uv[tris[i]];

				colors[i] = i % 3 == 0 ? new Color(1, 0, 0, 0) : (i % 3) == 1 ? new Color(0, 1, 0, 0) : new Color(0, 0, 1, 0);

				tris[i] = i;
			}

			Mesh wireframeMesh = new Mesh();

			wireframeMesh.Clear();
			wireframeMesh.vertices = vertices;
			wireframeMesh.triangles = tris;
			wireframeMesh.normals = normals;
			wireframeMesh.colors = colors;
			wireframeMesh.uv = uv;

			go.GetComponent<MeshFilter>().sharedMesh = wireframeMesh;
		}
	}
}
