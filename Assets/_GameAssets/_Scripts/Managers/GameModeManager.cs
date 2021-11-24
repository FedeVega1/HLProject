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
    GameModeData currentGameModeData;
    List<Player> connectedPlayers;

    void Awake()
    {
        if (INS == null) INS = this;
        else Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        connectedPlayers = new List<Player>();
        currentGameModeData = Resources.Load<GameModeData>("GameModes/AttackDefend");
        TeamManagerInstance.GetGameModeData(ref currentGameModeData.ticketsPerTeam);
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

        playerScript.SetPlayerName(CheckPlayerName(playerInfo.playerName));
        connectedPlayers.Add(playerScript);

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

    [Server]
    public bool CanCaptureControlPoint(ref List<Player> playersInCP, out int mayorTeam, int currentTeamHolder)
    {
        int[] playersPerTeam = TeamManagerInstance.SeparatePlayersPerTeam(ref playersInCP);

        mayorTeam = 0;
        int bestPlayerQuantity = -9999;

        if (playersPerTeam[0] == playersPerTeam[1] && playersInCP.Count >= currentGameModeData.minNumbPlayersToCaputre) return true;

        int size = TeamManager.MAXTEAMS;
        for (int i = 0; i < size; i++)
        {
            if (playersPerTeam[i] > bestPlayerQuantity)
            {
                mayorTeam = i + 1;
                bestPlayerQuantity = playersPerTeam[i];
            }
        }

        return playersPerTeam[mayorTeam - 1] >= currentGameModeData.minNumbPlayersToCaputre && mayorTeam != currentTeamHolder;
    }

    string CheckPlayerName(string playerName)
    {
        string newPlayerName = playerName;
        int sameNumberPlayers = 0;

        int size = connectedPlayers.Count;
        for (int i = 0; i < size; i++)
        {
            if (connectedPlayers[i].GetPlayerName() == playerName)
                sameNumberPlayers++;
        }

        if (sameNumberPlayers > 0) newPlayerName += $"#{sameNumberPlayers:000}";
        return newPlayerName;
    }
}
