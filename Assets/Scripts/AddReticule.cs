using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddReticule : MonoBehaviour
{
	[HideInInspector]
	public bool touchingPlayer;

	public void OnTriggerStay(Collider other)
	{
		if (other.tag == "Player") touchingPlayer = true;
	}
	public void OnTriggerExit(Collider other)
	{
		if (other.tag == "Player") touchingPlayer = false;
	}
}
