using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSpin : MonoBehaviour
{
	public float spinSpeed = 0.1f;

	public void Update()
	{
		transform.localEulerAngles += new Vector3(0, spinSpeed * Time.deltaTime, 0);
	}
}
