using UnityEngine;
using System.Collections;

//[RequireComponent(typeof(AudioSource))]

public class QuerySoundController : MonoBehaviour {
	
	
	public AudioClip[] soundData;

	public enum QueryChanSoundType
	{
		// Greetings
		DO_ME_A_FAVOR = 0,
		GOOD_MORNING = 1,
		GOOD_NIGHT_01 = 2,
		GOOD_NIGHT_02 = 3,
		GREETING_01 = 4,
		GREETING_02 = 5,
		HAVE_A_NICE_DAY = 6,
		HELLO = 7,
		MAY_I_START = 8,
		OTSUKARESAMA = 9,
		SEE_YOU = 10,
		SORRY = 11,

		// Shouts
		BAAANG = 12,
		BAD = 13,
		CONGRATULATIONS = 14,
		CRYING = 15,
		DECIDED = 16,
		DOMO = 17,
		DOZO = 18,
		GO_AHEAD = 19,
		GOOD = 20,
		LAUGHING = 21,
		LETS_GO = 22,
		LETS_START = 23,
		MUMU = 24,
		MUUU = 25,
		NO_DESU = 26,
		NO = 27,
		OH_NO = 28,
		OK = 29,
		ONE_TWO = 30,
		RELAX_RELAX = 31,
		SURPRISED = 32,
		TAKE_CARE = 33,
		THANK_YOU = 34,
		WAIT_A_MINUTE = 35,
		WAITING_FOR_YOU = 36,
		YES_YES = 37,
		YES = 38,

		// Others
		AHA = 39,
		AI_DEEPLEARNING = 40,
		AI_ERA = 41,
		AI = 42,
		ANGRY = 43,
		ARE_YOU_OK = 44,
		BAD_CONDITIONS = 45,
		CHEER_UP = 46,
		ENERGY = 47,
		FOLLOW_ME = 48,
		FUN = 49,
		GIVE_ME_JOB = 50,
		GO_AWAY_PAIN = 51,
		GO_CHARGE = 52,
		GOOD_JOB_01 = 53,
		GOOD_JOB_02 = 54,
		HAVE_A_BREAK = 55,
		HAWAWAWA = 56,
		I_AM_FULL = 57,
		IS_IT_OK = 58,
		ITS_TIME = 59,
		LUNCHTIME = 60,
		MONDAI_NOTHING = 61,
		MY_TURN = 62,
		NASTY = 63,
		PRESENT = 64,
		SAFE = 65,
		SLEEPY = 66,
		SOLVED = 67,
		SUNNY_DAY = 68,
		WATCH_OUT = 69

	}

	
	public void PlaySoundByType (QueryChanSoundType soundNumber) {

		this.GetComponent<AudioSource>().Stop();
		this.GetComponent<AudioSource>().PlayOneShot(soundData[(int)soundNumber]);
		
	}


	public void PlaySoundByNumber (int soundNumber) {
		
		this.GetComponent<AudioSource>().Stop();
		this.GetComponent<AudioSource>().PlayOneShot(soundData[soundNumber]);
		
	}


	public void StopSound () {

		this.GetComponent<AudioSource>().Stop();
		
	}
	
}
