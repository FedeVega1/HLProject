using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using Mirror;
using UnityEngine.ResourceManagement.AsyncOperations;

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

    int _FullScreenMode = -1;
    public FullScreenMode CurrentFullScreenMode
    {
        get
        {
            if (_FullScreenMode == -1)
            {
                if (PlayerPrefs.HasKey("EnableFullscreen"))
                {
                    _FullScreenMode = PlayerPrefs.GetInt("EnableFullscreen");
                }
                else
                {
                    _FullScreenMode = 1;
                    PlayerPrefs.SetInt("EnableFullscreen", _FullScreenMode);
                }
            }

            return (FullScreenMode) _FullScreenMode;
        }

        set
        {
            _FullScreenMode = (int) value;
            PlayerPrefs.SetInt("EnableFullscreen", _FullScreenMode);
            Screen.fullScreenMode = value;
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
            Screen.SetResolution(newRes.width, newRes.height, CurrentFullScreenMode, newRes.refreshRateRatio);
        }
    }

    int _CurrentQualityPreset = -1;
    public int CurrentQualityPreset
    {
        get
        {
            if (_CurrentQualityPreset == -1)
            {
                if (PlayerPrefs.HasKey("CurrentQualityPreset"))
                {
                    _CurrentQualityPreset = PlayerPrefs.GetInt("CurrentQualityPreset");
                }
                else
                {
                    _CurrentQualityPreset = QualitySettings.GetQualityLevel();
                    PlayerPrefs.SetInt("CurrentQualityPreset", _CurrentQualityPreset);
                }
            }

            return _CurrentQualityPreset;
        }

        set
        {
            _CurrentQualityPreset = value;
            PlayerPrefs.SetInt("CurrentQualityPreset", _CurrentQualityPreset);

            QualitySettings.SetQualityLevel(_CurrentQualityPreset, true);
        }
    }

    Resolution[] resArray = Screen.resolutions;

    int GetResolutionIndex(Resolution res)
    {
        int size = resArray.Length;

        for (int i = 0; i < size; i++)
        {
            if (resArray[i].width == res.width && resArray[i].height == res.height && resArray[i].refreshRateRatio.value == res.refreshRateRatio.value)
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

    public VideoOptions VideoOptions { get; private set; }

    bool returningtoMainMenu;
    AsyncOperationHandle<IList<GameObject>> backgroundsLoadHandle;

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

        VideoOptions = new VideoOptions();
    }

    void Start() => LoadAssets();

    void OnDestroy()
    {
        Addressables.Release(backgroundsLoadHandle);
    }

    void LoadAssets()
    {
        backgroundsLoadHandle = Addressables.LoadAssetsAsync<GameObject>("MainMenuBackgrounds", null);
        backgroundsLoadHandle.Completed += OnBackgroundLoaded;
    }

    void OnBackgroundLoaded(AsyncOperationHandle<IList<GameObject>> operation)
    {
        if (operation.Status == AsyncOperationStatus.Failed)
        {
            Debug.LogErrorFormat("Couldn't load MainMenu backgrounds: {0}", operation.OperationException);
            return;
        }

        InitMainMenu();
    }

    void InitMainMenu()
    {
        if (VideoOptions.EnableMainMenuBackgrounds) 
            Instantiate(backgroundsLoadHandle.Result[Random.Range(0, backgroundsLoadHandle.Result.Count)]);
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
        ShowLoadingScreen(() => 
        { 
            netManager.StopHost();
            returningtoMainMenu = true;
        });
    }

    public void DisconnectFromServer()
    {
        ShowLoadingScreen(() =>
        {
            netManager.StopClient(); 
            returningtoMainMenu = true;
        });
    }

    public void OnLoadedScene()
    {
        if (loadingScreen != null) loadingScreen.HideLoadingScreen();
        if (returningtoMainMenu) InitMainMenu();
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
