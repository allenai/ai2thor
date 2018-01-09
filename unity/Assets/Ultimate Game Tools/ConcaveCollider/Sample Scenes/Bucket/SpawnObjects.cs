using UnityEngine;
using System.Collections;

public class SpawnObjects : MonoBehaviour
{
    public GameObject ObjectToSpawn;
    public float Interval;
    public float NumObjects;

    private float SpawnTimer;
    private int   SpawnCounter;

	void Start()
    {
        SpawnTimer   = 0.0f;
        SpawnCounter = 0;
	}
	
	void Update()
    {
        if(SpawnCounter < NumObjects)
        {
            SpawnTimer -= Time.deltaTime;

            if(SpawnTimer < 0.0f)
            {
                Instantiate(ObjectToSpawn, transform.position, transform.rotation);
                SpawnTimer = Interval;
                SpawnCounter++;
            }
        }	
	}
}
