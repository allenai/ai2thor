using UnityEngine;
using System.Collections;

public class QueryEmotionalController : MonoBehaviour {

	[SerializeField]
	Material[] emotionalMaterial;

	[SerializeField]
	GameObject queryFace;

	public enum QueryChanEmotionalType
	{
		// Normal emotion
		NORMAL_EYEOPEN_MOUTHOPEN = 0,
		NORMAL_EYECLOSE_MOUTHCLOSE = 1,
		NORMAL_EYEOPEN_MOUTHCLOSE = 2,
		NORMAL_EYECLOSE_MOUTHOPEN = 3,

		// Anger emotion
		ANGER_EYEOPEN_MOUTHOPEN = 4,
		ANGER_EYECLOSE_MOUTHCLOSE = 5,
		ANGER_EYEOPEN_MOUTHCLOSE = 6,
		ANGER_EYECLOSE_MOUTHOPEN = 7,

		// Sad emotion
		SAD_EYEOPEN_MOUTHOPEN = 8,
		SAD_EYECLOSE_MOUTHCLOSE = 9,
		SAD_EYEOPEN_MOUTHCLOSE = 10,
		SAD_EYECLOSE_MOUTHOPEN = 11,

		// Fun emotion
		FUN_EYEOPEN_MOUTHOPEN = 12,
		FUN_EYECLOSE_MOUTHCLOSE = 13,
		FUN_EYEOPEN_MOUTHCLOSE = 14,
		FUN_EYECLOSE_MOUTHOPEN = 15,

		// Surprised emotion
		SURPRISED_EYEOPEN_MOUTHOPEN = 16,
		SURPRISED_EYECLOSE_MOUTHCLOSE = 17,
		SURPRISED_EYEOPEN_MOUTHCLOSE = 18,
		SURPRISED_EYECLOSE_MOUTHOPEN = 19,
		SURPRISED_EYEOPEN_MOUTHOPEN_MID = 20,
		SURPRISED_EYECLOSE_MOUTHOPEN_MID = 21

	}


	public void ChangeEmotion (QueryChanEmotionalType faceNumber)
	{
		queryFace.GetComponent<Renderer>().material = emotionalMaterial[(int)faceNumber];
	}
	
}
