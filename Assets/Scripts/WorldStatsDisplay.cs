using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorldStatsDisplay : MonoBehaviour
{
	public Text statsText;
	public float fpsUpdateTime = 0.25f;

	public void Awake()
	{
		UpdateFPS();
	}

	void UpdateFPS()
	{
		int fps = Mathf.RoundToInt(1f / Time.deltaTime);
		statsText.text = "FPS: " + fps.ToString();

		Invoke("UpdateFPS", fpsUpdateTime);
	}
}
