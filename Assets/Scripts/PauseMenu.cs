using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject menuObj;
    public GameObject mainObj;
    public GameObject optionsObj;
    public GameObject newLvlObj;

    public AudioSource _music;

    public Text[] musicTexts;
    public Text[] graphicsTexts;
    public Text[] invMouseTexts;

    int mode = 0;

    [HideInInspector]
    public bool paused = false;

    public bool music = true;
    public bool invertMouse = false;

    public static PauseMenu pauseMenu;

    void Awake()
    {
        pauseMenu = this;

        if (!music) _music.Stop();
    }

    void Update()
    {
        if (!World.Instance.worldInitialized) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            paused = !paused;
            mode = 0;

            if (paused)
            {
                menuObj.SetActive(true);
                mainObj.SetActive(true);
            }
            else
            {
                menuObj.SetActive(false);
                mainObj.SetActive(false);
                optionsObj.SetActive(false);
                newLvlObj.SetActive(false);
            }
        }

        if (mode == 1) SetOptionsText();
    }

    void SetTexts(Text[] texts, string msg)
    {
        foreach (Text t in texts)
        {
            t.text = msg;
        }
    }

    public void UnPauseGame()
    {
        paused = false;
        mode = 0;

        menuObj.SetActive(false);
        mainObj.SetActive(false);
        optionsObj.SetActive(false);
    }

    public void ChangeGraphicsMode()
    {
        World.Instance.ChangeGraphicsMode();
    }

    public void ToggleMusic()
    {
        music = !music;

        if (!music && _music.isPlaying) _music.Pause();

        if (music) _music.Play();
    }

    public void ToggleInvertMouse()
    {
        invertMouse = !invertMouse;
    }

    public void ViewOptions()
    {
        mode = 1;
        optionsObj.SetActive(true);
        mainObj.SetActive(false);
    }

    public void CloseOptions()
    {
        mode = 0;
        optionsObj.SetActive(false);
        mainObj.SetActive(true);
    }

    public async void QuitToTitle()
    {
        await UniTask.WaitForSeconds(0.25f);
        Quitzies();
    }

    void Quitzies()
    {
        World.Instance.SaveWorld();
        SceneManager.LoadScene(0);
    }

    void SetOptionsText()
    {
        // 获取当前图形模式
        GraphicsMode currentGMode = GraphicsSettingsManager.Instance.gMode;

        // 设置图形模式文本
        string graphicsModeText = "Graphics: " + currentGMode.ToString();
        SetTexts(graphicsTexts, graphicsModeText);

        // 设置音乐开关文本
        string musicText = music ? "Music: ON" : "Music: OFF";
        SetTexts(musicTexts, musicText);

        // 设置鼠标反转文本
        string invertMouseText = invertMouse ? "Invert Mouse Y: ON" : "Invert Mouse Y: OFF";
        SetTexts(invMouseTexts, invertMouseText);
    }
}