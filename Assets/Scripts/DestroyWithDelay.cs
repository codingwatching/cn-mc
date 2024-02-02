using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyWithDelay : MonoBehaviour
{
	public float delay;

	void Start()
	{
		Invoke("DestroySelf", delay);
	}

	void DestroySelf()
	{
		Destroy(gameObject);
	}
}
