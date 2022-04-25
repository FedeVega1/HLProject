using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class VideoOptions
{
    int _EnableMainMenuBackgrounds = -1;
    public bool EnableMainMenuBackgrounds
    {
        get
        {
            if (_EnableMainMenuBackgrounds == -1)
            {
                if (PlayerPrefs.HasKey("MainMenuBackgrounds"))
                {
                    _EnableMainMenuBackgrounds = PlayerPrefs.GetInt("MainMenuBackgrounds");
                }
                else
                {
                    _EnableMainMenuBackgrounds = 1;
                    PlayerPrefs.SetInt("MainMenuBackgrounds", _EnableMainMenuBackgrounds);
                }
            }

            return _EnableMainMenuBackgrounds == 1;
        }

        set
        {
            _EnableMainMenuBackgrounds = value ? 1 : 0;
            PlayerPrefs.SetInt("MainMenuBackgrounds", _EnableMainMenuBackgrounds);
        }
    }

    int _EnableFullscreen = -1;
    public bool EnableFullScreen
    {
        get
        {
            if (_EnableFullscreen == -1)
            {
                if (PlayerPrefs.HasKey("EnableFullscreen"))
                {
                    _EnableFullscreen = PlayerPrefs.GetInt("EnableFullscreen");
                }
                else
                {
                    _EnableFullscreen = 1;
                    PlayerPrefs.SetInt("EnableFullscreen", _EnableFullscreen);
                }
            }

            return _EnableFullscreen == 1;
        }

        set
        {
            _EnableFullscreen = value ? 1 : 0;
            PlayerPrefs.SetInt("EnableFullscreen", _EnableFullscreen);
            Screen.fullScreen = value;
        }
    }

    int _CurrentResolution = -1;
    public int CurrentResolution
    {
        get
        {
            if (_CurrentResolution == -1)
            {
                if (PlayerPrefs.HasKey("CurrentResolution"))
                {
                    _CurrentResolution = PlayerPrefs.GetInt("CurrentResolution");
                }
                else
                {
                    _CurrentResolution = GetResolutionIndex(Screen.currentResolution);
                    PlayerPrefs.SetInt("CurrentResolution", _CurrentResolution);
                }
            }

            return _CurrentResolution;
        }

        set
        {
            _CurrentResolution = value;
            PlayerPrefs.SetInt("CurrentResolution", _CurrentResolution);

            Resolution newRes = GetResolutionByIndex(_CurrentResolution);
            Screen.SetResolution(newRes.width, newRes.height, EnableFullScreen, newRes.refreshRate);
        }
    }

    Resolution[] resArray = Screen.resolutions;

    int GetResolutionIndex(Resolution res)
    {
        int size = resArray.Length;

        for (int i = 0; i < size; i++)
        {
            if (resArray[i].width == res.width && resArray[i].height == res.height && resArray[i].refreshRate == res.refreshRate)
                return i;
        }

        return 0;
    }

    Resolution GetResolutionByIndex(int index) => resArray[index];
}

public class GameManager : MonoBehaviour
{
    public static GameManager INS;

    [SerializeField] NetManager netManager;
    [SerializeField] UILoadingScreen loadingScreen;
    [SerializeField] GameObject[] mainMenuBackgrounds;

    string _PlayerName = "";
    public string PlayerName 
    { 
        get
        {
            if (_PlayerName == "")
            {
                if (PlayerPrefs.HasKey("PlayerName"))
                {
                    _PlayerName = PlayerPrefs.GetString("PlayerName");
                }
                else
                {
                    _PlayerName = "AnonPlayer";
                    PlayerPrefs.SetString("PlayerName", _PlayerName);
                }
            }

            return _PlayerName;
        }

        private set
        {
            _PlayerName = value;
            PlayerPrefs.SetString("PlayerName", _PlayerName);
        }
    }

    public VideoOptions videoOptions { get; private set; }

    void Awake()
    {
        if (INS == null)
        {
            INS = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += (_, __) => OnLoadedScene();
        
        videoOptions = new VideoOptions();
    }

    void Start()
    {
        if (videoOptions.EnableMainMenuBackgrounds) 
            Instantiate(mainMenuBackgrounds[Random.Range(0, mainMenuBackgrounds.Length)]);
    }

    public void QuitGame() => Application.Quit();

    public void CreateMatch()
    {
        ShowLoadingScreen(netManager.StartHost);
    }

    public void ConnectToServerByIP(string serverIP)
    {
        netManager.networkAddress = serverIP;
        ShowLoadingScreen(netManager.StartClient);
    }

    public void StopServer()
    {
        ShowLoadingScreen(netManager.StopHost);
    }

    public void DisconnectFromServer()
    {
        ShowLoadingScreen(netManager.StopClient);
    }

    public void OnLoadedScene()
    {
        if (loadingScreen != null) loadingScreen.HideLoadingScreen();
    }

    void ShowLoadingScreen(System.Action OnLoadingScreenShown)
    {
        if (loadingScreen != null) loadingScreen.ShowLoadingScreen();
        LeanTween.value(0, 1, .5f).setOnComplete(() => { OnLoadingScreenShown?.Invoke(); });
    }

    public void ServerChangeLevel()
    {
        ShowLoadingScreen(() => { netManager.ServerChangeScene(netManager.onlineScene); });
    }

    public string SetPlayerName(string newName) => PlayerName = newName;
}
