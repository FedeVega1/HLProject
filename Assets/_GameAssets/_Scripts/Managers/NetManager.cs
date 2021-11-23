using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetManager : NetworkManager
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<PlayerInfo>(OnPlayerConnection);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        //if (GameModeManager.INS == null) Debug.LogError("Couldn't find a GameModeManager in scene");

        PlayerInfo message = new PlayerInfo
        {
            playerName = GameManager.INS.PlayerName
        };

        conn.Send(message);
    }

    void OnPlayerConnection(NetworkConnection conn, PlayerInfo message)
    {
        GameObject playerObject = GameModeManager.INS.SpawnPlayerObject(playerPrefab, message);
        NetworkServer.AddPlayerForConnection(conn, playerObject);
        GameModeManager.INS.OnPlayerConnection(playerObject);
    }
}
