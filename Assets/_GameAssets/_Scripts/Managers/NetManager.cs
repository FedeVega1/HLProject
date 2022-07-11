using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct AddCustomPlayerMessage : NetworkMessage
{

}

public class NetManager : NetworkManager
{
    public static NetManager INS;
    public System.Action<NetworkConnection> OnClientDisconnects;

    public override void Awake()
    {
        base.Awake();
        if (INS == null) INS = this;
        else Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<AddCustomPlayerMessage>(OnCustomPlayer);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        OnClientDisconnects?.Invoke(conn);
    }

    public override void OnClientConnect()
    {
        //base.OnClientConnect(conn);
        if (clientLoadedScene) return;

        if (!NetworkClient.ready) NetworkClient.Ready();
        if (NetworkClient.connection.identity == null || !NetworkClient.connection.isReady)
        {
            NetworkClient.connection.Send(new AddCustomPlayerMessage());
        }
        //if (GameModeManager.INS == null) Debug.LogError("Couldn't find a GameModeManager in scene");

        //PlayerInfo message = new PlayerInfo
        //{
        //    playerName = GameManager.INS.PlayerName
        //};

        //conn.Send(message);
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        GameManager.INS.OnLoadedScene();
        
        if (NetworkClient.connection.identity == null || !NetworkClient.connection.isReady)
        {
            NetworkClient.connection.Send(new AddCustomPlayerMessage());
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

    void OnCustomPlayer(NetworkConnectionToClient conn, AddCustomPlayerMessage msg) { OnServerAddPlayer(conn); }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
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
