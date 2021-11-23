using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameModeManager : NetworkBehaviour
{
    public static GameModeManager INS;

    [System.Serializable]
    class TeamSpawnPoints
    {
        public Transform[] spawnPoints;
        public int GetQuanity() => spawnPoints.Length;
    }

    [SerializeField] TeamManager teamManager;
    [SerializeField] TeamSpawnPoints[] spawnPointsByTeam;
    [SerializeField] Transform[] spectatorPoints;

    public TeamManager TeamManagerInstance => teamManager;

    bool matchEnded;

    void Awake()
    {
        if (INS == null) INS = this;
        else Destroy(gameObject);
    }

    [Server]
    public GameObject SpawnPlayerObject(GameObject playerPrefab, PlayerInfo playerInfo)
    {
        GameObject playerObject = Instantiate(playerPrefab, spectatorPoints[0].position, spectatorPoints[0].rotation);
        Player playerScript = playerObject.GetComponent<Player>();

        if (playerScript == null)
        {
            Debug.LogError("Player prefab doesn't have a Player Component!");
            return null;
        }

        playerScript.SetPlayerName(playerInfo.playerName);
       
        return playerObject;
    }

    [Server]
    public void OnPlayerConnection(GameObject playerObject)
    {
        Player playerScript = playerObject.GetComponent<Player>();

        if (playerScript == null)
        {
            Debug.LogError("Player prefab doesn't have a Player Component!");
            return;
        }

        if (!matchEnded) playerScript.RpcShowGreetings(playerScript.connectionToClient);
    }

    [Server]
    public void SpawnPlayerByTeam(Player playerToSpawn)
    {
        int playerTeam = playerToSpawn.GetPlayerTeam() - 1;
        Transform randomSpawnPoint = spawnPointsByTeam[playerTeam].spawnPoints[Random.Range(0, spawnPointsByTeam[playerTeam].GetQuanity())];
        playerToSpawn.SpawnPlayer(randomSpawnPoint.position, randomSpawnPoint.rotation);
    }
}
