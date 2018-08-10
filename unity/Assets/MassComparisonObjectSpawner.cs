using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassComparisonObjectSpawner : MonoBehaviour 
{
	public SimObjPhysics[] ObjectsToSpawn;
	public GameObject SpawnPosition;

	[Header("Toggles")]
	public bool MakeThisTrueForKeyboardSpawning = false;

	public bool RandomSpawnAngle = false;
	///private List<SimObjPhysics> ObjectsToSpawn_List;

	public bool PlayAnimation = false;
	//public GameObject test;

	private Animator MyAnim;

	// Use this for initialization
	void Start () 
	{
		//This allows us to load mass objects directly from a filepath which might be useful if we ever want to
        //expand this functionality to even more objects, for the Hackathon 2018 demo we will just use these
        //6 food prefabs that are, for now, super hard coded in
		//test = Resources.Load<GameObject>("MassPrefabs/Apple_mass");

		if(PlayAnimation)
		{
			MyAnim = gameObject.GetComponent<Animator>();
		}
	}

	// Update is called once per frame
	void Update () 
	{
		if(MakeThisTrueForKeyboardSpawning == true)
		{
			if (Input.GetKeyDown(KeyCode.Alpha2))
            {
				SpawnSingle_SingleObjectType("bread");

            }
			if (Input.GetKeyDown(KeyCode.Alpha3))
            {
				SpawnSingle_One_RandomObjectType();

            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                
				SpawnMultiple_SingleObjectType(3, "lettuce", 0.2f);
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
				SpawnMultiple_One_RandomObjectType(3, 0.2f);

            }

            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
				SpawnMultiple_Each_RandomObjectType(6, 0.2f);

            }
         
			if (Input.GetKeyDown(KeyCode.Alpha7))
            {
				SpawnRandRange_SingleObjectType(1, 10, "lettuce", 0.2f);
            }
            
            if(Input.GetKeyDown(KeyCode.Alpha8))
			{
				SpawnRandRange_One_RandomObjectType(1, 10, 0.2f);
			}

            if(Input.GetKeyDown(KeyCode.Alpha9))
			{
				SpawnRandRange_Each_RandomObjectType(1, 10, 0.2f);
			}
		}
	}

	IEnumerator SpawnMultiple_Single_CR(int count, string whichobject, float delay)
	{

		for (int i = 0; i < count; i++)
		{
			SpawnSingle_SingleObjectType(whichobject);
			yield return new WaitForSeconds(delay);

		}

	}

    IEnumerator SpawnMultiple_One_Rand_CR(int count, float delay)
	{
		int whichobject = Random.Range(0, ObjectsToSpawn.Length);

		for (int i = 0; i < count; i++)
        {
			if (RandomSpawnAngle)
			{
				if(PlayAnimation)
				{
					MyAnim.SetTrigger("Play");
				}

				Instantiate(ObjectsToSpawn[whichobject], SpawnPosition.transform.position,
            Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
			}

            else
			{
				if (PlayAnimation)
                {
                    MyAnim.SetTrigger("Play");
                }
				Instantiate(ObjectsToSpawn[whichobject], SpawnPosition.transform);
			}

            yield return new WaitForSeconds(delay);

        }
	}

	IEnumerator SpawnMultiple_Each_Rand_CR(int count, float delay)
	{
		for (int i = 0; i < count; i++)
        {
			if (PlayAnimation)
            {
                MyAnim.SetTrigger("Play");
            }

            if (RandomSpawnAngle)
				Instantiate(ObjectsToSpawn[Random.Range(0, ObjectsToSpawn.Length)], SpawnPosition.transform.position,
                            Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
            else
				Instantiate(ObjectsToSpawn[Random.Range(0, ObjectsToSpawn.Length)], SpawnPosition.transform);
            yield return new WaitForSeconds(delay);

        }
	}

	IEnumerator SpawnRandRange_Single_CR(int min, int max, string whichobject, float delay)
	{
		int count = Random.Range(min, max);

		StartCoroutine(SpawnMultiple_Single_CR(count, whichobject, delay));

		yield break;
	}

	IEnumerator SpawnRandRange_One_Rand_CR(int min, int max, float delay)
	{
		int count = Random.Range(min, max);
		int whichobject = Random.Range(0, ObjectsToSpawn.Length);

		for (int i = 0; i < count; i ++)
		{
			if (PlayAnimation)
            {
                MyAnim.SetTrigger("Play");
            }

			if (RandomSpawnAngle)
				Instantiate(ObjectsToSpawn[whichobject], SpawnPosition.transform.position,
                            Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
            else
				Instantiate(ObjectsToSpawn[whichobject], SpawnPosition.transform);
            yield return new WaitForSeconds(delay);
		}
		yield break;
	}

	IEnumerator SpawnRandRange_Each_Rand_CR(int min, int max, float delay)
	{
		int count = Random.Range(min, max);

        for (int i = 0; i < count; i++)
        {
			if (PlayAnimation)
            {
                MyAnim.SetTrigger("Play");
            }

            if (RandomSpawnAngle)
				Instantiate(ObjectsToSpawn[Random.Range(0, ObjectsToSpawn.Length)], SpawnPosition.transform.position,
                            Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
            else
				Instantiate(ObjectsToSpawn[Random.Range(0, ObjectsToSpawn.Length)], SpawnPosition.transform);
            yield return new WaitForSeconds(delay);
        }
        yield break;
	}
    //spawn a single object of a single type
	//ex: spawn 1 object of type {egg}
	public void SpawnSingle_SingleObjectType(string whichobject)
	{
		if (PlayAnimation)
        {
            MyAnim.SetTrigger("Play");
        }
		switch(whichobject)
		{
			case "bread":
				if (RandomSpawnAngle)
					Instantiate(ObjectsToSpawn[1], SpawnPosition.transform.position,
								Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
				else
					Instantiate(ObjectsToSpawn[1], SpawnPosition.transform);
				break;

			case "tomato":
				if (RandomSpawnAngle)
                    Instantiate(ObjectsToSpawn[5], SpawnPosition.transform.position,
                                Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
                else
                    Instantiate(ObjectsToSpawn[5], SpawnPosition.transform);
				break;

			case "egg":
				if (RandomSpawnAngle)
                    Instantiate(ObjectsToSpawn[2], SpawnPosition.transform.position,
                                Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
                else
                    Instantiate(ObjectsToSpawn[2], SpawnPosition.transform);
				break;
                
			case "potato":
				if (RandomSpawnAngle)
                    Instantiate(ObjectsToSpawn[4], SpawnPosition.transform.position,
                                Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
                else
                    Instantiate(ObjectsToSpawn[4], SpawnPosition.transform);
				break;
                
			case "lettuce":
				if (RandomSpawnAngle)
                    Instantiate(ObjectsToSpawn[3], SpawnPosition.transform.position,
                                Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
                else
                    Instantiate(ObjectsToSpawn[3], SpawnPosition.transform);
				break;
                
			case "apple":
				if (RandomSpawnAngle)
                    Instantiate(ObjectsToSpawn[0], SpawnPosition.transform.position,
                                Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
                else
                    Instantiate(ObjectsToSpawn[0], SpawnPosition.transform);
				break;
                            
		}
	}

    //spawn a single object of a single random type
	//ex: spawn 1 object of type {bread, tomato, egg, potato, lettuce, apple}
    public void SpawnSingle_One_RandomObjectType()
    {
		if (PlayAnimation)
        {
            MyAnim.SetTrigger("Play");
        }

		if (RandomSpawnAngle)
			Instantiate(ObjectsToSpawn[Random.Range(0, 5)], SpawnPosition.transform.position,
						Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));

		else
			Instantiate(ObjectsToSpawn[Random.Range(0, 5)], SpawnPosition.transform);
    }

    //spawn multiple objects of a single type
	//ex: spawn 5 objects of type {bread}
    public void SpawnMultiple_SingleObjectType(int count, string whichobject, float delay)
	{
		StartCoroutine(SpawnMultiple_Single_CR(count, whichobject, delay));
	}

    //spawn multiple objects of a single random type
	//ex: spawn 5 objects of type {bread, tomato, egg, potato, lettuce, apple}
    public void SpawnMultiple_One_RandomObjectType(int count, float delay)
	{
		StartCoroutine(SpawnMultiple_One_Rand_CR(count ,delay));
	}

    //spawn multiple objects each of a random type
	//ex: Spawn 5 objects, each has a chance to be of type {bread, tomato, egg, potato, lettuce, apple}
    public void SpawnMultiple_Each_RandomObjectType(int count, float delay)
	{
		StartCoroutine(SpawnMultiple_Each_Rand_CR(count, delay));
	}

	//spawn a random number (range) of a single object type
	//ex: spawn rand(1-10) {eggs}
    public void SpawnRandRange_SingleObjectType(int min, int max, string whichobject, float delay)
	{
		StartCoroutine(SpawnRandRange_Single_CR(min, max, whichobject, delay));
	}

	//spawn a random number (range) of a single random type
	//ex: spawn rand(1-10) of type {bread, tomato, egg, potato, lettuce, apple}
    public void SpawnRandRange_One_RandomObjectType(int min, int max, float delay)
	{
		StartCoroutine(SpawnRandRange_One_Rand_CR(min, max, delay));
	}

	//spawn a random number (range) each of a random type
	//ex: spawn rand(1-10), each has a chance to be of type {bread, tomato, egg, potato, lettuce, apple}
    public void SpawnRandRange_Each_RandomObjectType(int min, int max, float delay)
	{
		StartCoroutine(SpawnRandRange_Each_Rand_CR(min, max, delay));
	}
}
