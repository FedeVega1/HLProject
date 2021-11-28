using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetManager : NetworkManager
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        //NetworkServer.RegisterHandler<PlayerInfo>(OnPlayerConnection);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        conn.Send(new AddPlayerMessage());
        //if (GameModeManager.INS == null) Debug.LogError("Couldn't find a GameModeManager in scene");

        //PlayerInfo message = new PlayerInfo
        //{
        //    playerName = GameManager.INS.PlayerName
        //};

        //conn.Send(message);
    }

    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        base.OnClientSceneChanged(conn);
        GameManager.INS.OnLoadedScene();
        conn.Send(new AddPlayerMessage());

        //PlayerInfo message = new PlayerInfo
        //{
        //    playerName = GameManager.INS.PlayerName
        //};

        //conn.Send(message);

        //PlayerInfo message = new PlayerInfo
        //{
        //    playerName = GameManager.INS.PlayerName
        //};

        //conn.Send(message);
    }

    public override void OnServerSceneChanged(string serverName)
    {
        //GameModeManager.INS.OnSceneChanged();
        GameManager.INS.OnLoadedScene();
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        GameObject playerObject = GameModeManager.INS.SpawnPlayerObject(playerPrefab, GameManager.INS.PlayerName);
        NetworkServer.AddPlayerForConnection(conn, playerObject);
        GameModeManager.INS.OnPlayerConnection(playerObject);
    }

    //void OnPlayerConnection(NetworkConnection conn, PlayerInfo message)
    //{
    //    GameObject playerObject = GameModeManager.INS.SpawnPlayerObject(playerPrefab, message);
    //    NetworkServer.AddPlayerForConnection(conn, playerObject);
    //    GameModeManager.INS.OnPlayerConnection(playerObject);
    //}
}
