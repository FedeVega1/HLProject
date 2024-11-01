using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using Mirror;
using UnityEngine.ResourceManagement.AsyncOperations;
using HLProject.UI;

namespace HLProject.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager INS;

        [SerializeField] UILoadingScreen loadingScreen;
        [SerializeField] MainMenuUI mainMenu;

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

        NetManager _NetManager;
        NetManager NetManager
        {
            get
            {
                if (_NetManager == null) _NetManager = FindObjectOfType<NetManager>();
                return _NetManager;
            }
        }

        public VideoOptions VideoOptions { get; private set; }
        public AudioOptions AudioOptions { get; private set; }

        public bool IsMainMenuEnabled => mainMenu.IsMainMenuEnabled;

        bool returningtoMainMenu, showErrorScreen, localPlayerIsHost;
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
            mainMenu.OnPlayerDisconnects += () =>
            {
                if (localPlayerIsHost) StopServer();
                else DisconnectFromServer();
            };

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
                mainMenu.ToggleErrorPanel(true);
                showErrorScreen = false;
            }
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(); 
#endif
        }

        public void CreateMatch()
        {
            ShowLoadingScreen(NetManager.StartHost);
            localPlayerIsHost = true;
            returningtoMainMenu = false;
        }

        public void ConnectToServerByIP(string serverIP)
        {
            NetManager.networkAddress = serverIP;
            ShowLoadingScreen(NetManager.StartClient);
            returningtoMainMenu = false;
        }

        public void StopServer()
        {
            mainMenu.LocalPlayerExitsMatch();
            ShowLoadingScreen(() =>
            {
                NetManager.StopHost();
                localPlayerIsHost = false;
                returningtoMainMenu = true;
                mainMenu.ToggleMainMenu(true);
            });
        }

        public void DisconnectFromServer()
        {
            mainMenu.LocalPlayerExitsMatch();
            ShowLoadingScreen(() =>
            {
                NetManager.StopClient();
                returningtoMainMenu = true;
                mainMenu.ToggleMainMenu(true);
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
            mainMenu.ToggleMainMenu(false);
            if (loadingScreen != null) loadingScreen.ShowLoadingScreen();
            LeanTween.value(0, 1, .5f).setOnComplete(() => { OnLoadingScreenShown?.Invoke(); });
        }

        public void ServerChangeLevel()
        {
            ShowLoadingScreen(() => { NetManager.ServerChangeScene(NetManager.onlineScene); });
        }

        public string SetPlayerName(string newName) => PlayerName = newName;

        public void ToggleMainMenu() => mainMenu.ToggleMainMenu(!IsMainMenuEnabled);
    }
}
