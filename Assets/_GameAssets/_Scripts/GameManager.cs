using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameManager : MonoBehaviour
{
    public static GameManager INS;

    [SerializeField] NetworkManager netManager;

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
}
