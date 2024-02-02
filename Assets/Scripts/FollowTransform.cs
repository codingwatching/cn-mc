using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTransform : MonoBehaviour
{
	public Transform obj;

	public void Update()
	{
		transform.localPosition = obj.transform.position;
	}
}
