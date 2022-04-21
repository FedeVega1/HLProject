using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct AddCustomPlayerMessage : NetworkMessage
{

}

public class NetManager : NetworkManager
{
    public System.Action<NetworkConnection> OnClientDisconnects;

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<AddCustomPlayerMessage>(OnCustomPlayer);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        OnClientDisconnects?.Invoke(conn);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        //base.OnClientConnect(conn);
        if (clientLoadedScene) return;

        if (!NetworkClient.ready) NetworkClient.Ready();
        if (conn.identity == null || !conn.isReady)
        {
            conn.Send(new AddCustomPlayerMessage());
        }
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

        
        if (conn.identity == null || !conn.isReady)
        {
            conn.Send(new AddCustomPlayerMessage());
        }

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

    void OnCustomPlayer(NetworkConnection conn, AddCustomPlayerMessage msg) { OnServerAddPlayer(conn); }

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
