using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
	public AudioClip[] clips;

	public static SoundManager SOUND_MANAGER;
	public static AudioClip[] CLIPS;

	public void Awake()
	{
		SOUND_MANAGER = this;
		CLIPS = clips;
	}

	public void PlayAudio(string clipName)
	{
		AudioClip clip = null;
		for (int i = 0; i < CLIPS.Length; i++)
		{
			if (CLIPS[i].name == clipName)
			{
				clip = CLIPS[i];
			}
		}
		if (clip == null) return;

		GameObject audObj = new GameObject();
		audObj.AddComponent<DestroyWithDelay>();
		AudioSource audioNode = audObj.AddComponent<AudioSource>();

		AudioSource node = Instantiate(audioNode, SOUND_MANAGER.transform) as AudioSource;

		node.GetComponent<DestroyWithDelay>().delay = clip.length;

		node.clip = clip;
		node.volume = 0.5f;
		node.Play();
	}
	public static void PlayAudio(string clipName, float volume)
	{
		AudioClip clip = null;
		for(int i = 0; i < CLIPS.Length; i++)
		{
			if (CLIPS[i].name == clipName)
			{
				clip = CLIPS[i];
			}
		}
		if (clip == null) return;

		GameObject audObj = new GameObject();
		audObj.AddComponent<DestroyWithDelay>();
		AudioSource audioNode = audObj.AddComponent<AudioSource>();

		AudioSource node = Instantiate(audioNode, SOUND_MANAGER.transform) as AudioSource;

		node.GetComponent<DestroyWithDelay>().delay = clip.length;

		node.clip = clip;
		node.volume = volume;
		node.Play();
	}
	public static void PlayAudio(string clipName, float volume, float pitch, bool bypassReverbZones)
	{
		AudioClip clip = null;
		for (int i = 0; i < CLIPS.Length; i++)
		{
			if (CLIPS[i].name == clipName)
			{
				clip = CLIPS[i];
			}
		}
		if (clip == null) return;

		GameObject audObj = new GameObject();
		audObj.AddComponent<DestroyWithDelay>();
		AudioSource audioNode = audObj.AddComponent<AudioSource>();

		AudioSource node = Instantiate(audioNode, SOUND_MANAGER.transform) as AudioSource;

		node.GetComponent<DestroyWithDelay>().delay = clip.length;

		node.bypassReverbZones = bypassReverbZones;
		node.clip = clip;
		node.volume = volume;
		node.pitch = pitch;
		node.Play();
	}
	public static void PlayAudio(string clipName, float volume, float pitch)
	{
		AudioClip clip = null;
		for (int i = 0; i < CLIPS.Length; i++)
		{
			if (CLIPS[i].name == clipName)
			{
				clip = CLIPS[i];
			}
		}
		if (clip == null) return;

		GameObject audObj = new GameObject();
		audObj.AddComponent<DestroyWithDelay>();
		AudioSource audioNode = audObj.AddComponent<AudioSource>();

		AudioSource node = Instantiate(audioNode, SOUND_MANAGER.transform) as AudioSource;

		node.GetComponent<DestroyWithDelay>().delay = clip.length * pitch;

		node.clip = clip;
		node.volume = volume;
		node.pitch = pitch;
		node.Play();
	}
}
