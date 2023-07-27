using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using Mirror;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEditor;

namespace HLProject
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager INS;

        [SerializeField] NetManager netManager;
        [SerializeField] UILoadingScreen loadingScreen;

        MainMenuUI _MainMenuUI;
        MainMenuUI MainMenuUI
        {
            get
            {
                if (_MainMenuUI == null) _MainMenuUI = FindObjectOfType<MainMenuUI>();
                return _MainMenuUI;
            }
        }

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
        public AudioOptions AudioOptions { get; private set; }

        bool returningtoMainMenu, showErrorScreen;
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
            AudioOptions = new AudioOptions();
        }

        void Start() => LoadAssets();

        void OnDestroy()
        {
            if (backgroundsLoadHandle.IsValid()) 
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

            if (showErrorScreen)
            {
                MainMenuUI.ToggleErrorPanel(true);
                showErrorScreen = false;
            }
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

        public void DisconnectFromServerByError(bool isServer)
        {
            if (isServer) StopServer();
            else DisconnectFromServer();
            showErrorScreen = true;
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
}
