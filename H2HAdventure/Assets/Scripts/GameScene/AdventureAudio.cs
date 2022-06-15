using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class AdventureAudio : MonoBehaviour {

    public AudioClip pickupClip;

    public AudioClip putdownClip;

    public AudioClip dragonRoarClip;

    public AudioClip dragonDieClip;

    public AudioClip dragonEatClip;

    public AudioClip glowClip;

    public AudioClip winClip;

    public AudioSource speaker;

    //-------- System sounds

    public AudioClip blip;



	public void play (SOUND sound, float volume) {
        switch (sound)
        {
            case SOUND.PICKUP:
                speaker.clip = pickupClip;
                break;
            case SOUND.PUTDOWN:
                speaker.clip = putdownClip;
                break;
            case SOUND.DRAGONDIE:
                speaker.clip = dragonDieClip;
                break;
            case SOUND.ROAR:
                speaker.clip =dragonRoarClip;
                break;
            case SOUND.EATEN:
                speaker.clip = dragonEatClip;
                break;
            case SOUND.GLOW:
                speaker.clip = glowClip;
                break;
            case SOUND.WON:
                speaker.clip = winClip;
                break;
            default:
                speaker.clip = null;
                break;
        }
        speaker.volume = volume / MAX.VOLUME;
        if (speaker.clip != null)
        {
            speaker.Play();
        }
	}

    public void PlaySystemSound(AudioClip clip)
    {
        speaker.clip = clip;
        speaker.volume = 1;
        if (clip != null)
        {
            speaker.Play();
        }
    }
}
