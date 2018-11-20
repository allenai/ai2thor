using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualizationHeatmapCSV : MonoBehaviour 
{
	public TextAsset CSVFile;

	public List<float> xvalues;
	public List<float> zvalues;
	public List<float> rotationvalues;

	public GameObject prefabLight;
	public GameObject prefabMedium;
	public GameObject prefabHeavy;

	// Use this for initialization
	void Start () 
	{
		string[] data = CSVFile.text.Split(new char[] { '\n'});
		float x, z, r;

		for(int i = 1; i < data.Length - 1; i++)
		{
			string[] row = data[i].Split(new char[] { ','});

			float.TryParse(row[0], out x);
			xvalues.Add(x);

			float.TryParse(row[1], out z);
			zvalues.Add(z);

			float.TryParse(row[2], out r);
			rotationvalues.Add(r);
		}

		for(int i = 0; i < xvalues.Count; i++)
		{
			Vector3 pos = new Vector3(xvalues[i], 1, zvalues[i]);
			Vector3 rot = new Vector3(0, rotationvalues[i], 0);

			//just spawn the Heavy prefab for now since it is the most visible
			Instantiate(prefabHeavy, pos, Quaternion.Euler(rot));
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}
}
