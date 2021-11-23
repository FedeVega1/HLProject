using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class GameManager : MonoBehaviour
{
    public static GameManager INS;

    [SerializeField] NetManager netManager;

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
        netManager.StartHost();
    }

    public void ConnectToServerByIP(string serverIP)
    {
        netManager.networkAddress = serverIP;
        netManager.StartClient();
    }

    public void StopServer()
    {
        netManager.StopHost();
        SceneManager.LoadScene(netManager.offlineScene);
    }

    public void DisconnectFromServer()
    {
        netManager.StopClient();
        SceneManager.LoadScene(netManager.offlineScene);
    }

    public string SetPlayerName(string newName) => PlayerName = newName;
}
