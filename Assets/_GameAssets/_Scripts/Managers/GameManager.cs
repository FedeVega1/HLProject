using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

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
