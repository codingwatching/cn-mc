using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
	public void VisitURL(string url)
	{
		Application.OpenURL(url);
	}

	public void LoadGame()
	{
		Invoke("loadscene", 0.25f);
	}

	void loadscene()
	{
		SceneManager.LoadScene(1);
	}

	public void QuitGame()
	{
		Application.Quit();
	}
}
