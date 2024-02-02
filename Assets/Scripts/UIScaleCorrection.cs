using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class UIScaleCorrection : MonoBehaviour
{
	public CanvasScaler canvas;
	public float dividend = 1000f;

	public void Awake()
	{
		canvas = FindObjectOfType<CanvasScaler>();
	}

	public void Update()
	{
		if (!canvas) return;

		if (Screen.width < Screen.height)
		{
			canvas.scaleFactor = Screen.width / dividend; 
		}
		else
		{
			canvas.scaleFactor = Screen.height / dividend;
		}
	}
}
