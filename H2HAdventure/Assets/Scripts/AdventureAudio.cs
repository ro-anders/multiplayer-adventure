using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class AdventureAudio : MonoBehaviour {

    public AudioClip pickupClip;

    public AudioClip putdownClip;

    public AudioSource speaker;

	public void play (SOUND sound, float volume) {
        switch (sound)
        {
            case SOUND.PICKUP:
                speaker.clip = pickupClip;
                break;
            case SOUND.PUTDOWN:
                speaker.clip = putdownClip;
                break;
            default:
                break;
        }
        speaker.volume = volume / MAX.VOLUME;
        speaker.Play();
	}
}
