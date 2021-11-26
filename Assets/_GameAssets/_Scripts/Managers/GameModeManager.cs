using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameModeManager : NetworkBehaviour
{
    public static GameModeManager INS;

    [System.Serializable]
    class ControlPointNode
    {
        public ControlPoint controlPoint;
        public int order;
    }

    class PlayerTimer
    {
        readonly Player affectedPlayer;
        readonly double timeToAction;
        readonly System.Action<Player> actionToPerform;

        public PlayerTimer(ref Player _Player, float time, System.Action<Player> action)
        {
            affectedPlayer = _Player;
            timeToAction = NetworkTime.time + time;
            actionToPerform = action;
        }

        public bool IsAffectedPlayer(ref Player playerToCheck) => playerToCheck == affectedPlayer;
        public bool OnTime() => NetworkTime.time >= timeToAction;
        public void PerformAction() => actionToPerform?.Invoke(affectedPlayer);
    }

    [SerializeField] TeamManager teamManager;
    [SerializeField] Transform[] spectatorPoints;
    [SerializeField] ControlPointNode[] controlPoints;
    [SerializeField] TeamBase[] teamBases;

    public TeamManager TeamManagerInstance => teamManager;

    bool matchEnded;
    GameModeData currentGameModeData;
    List<Player> connectedPlayers;
    List<PlayerTimer> playerTimerQueue;

    void Awake()
    {
        if (INS == null) INS = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (!isServer) return;
        if (playerTimerQueue.Count > 0)
        {
            int size = playerTimerQueue.Count;
            for (int i = 0; i < size; i++)
            {
                if (playerTimerQueue[i].OnTime())
                {
                    playerTimerQueue[i].PerformAction();
                    playerTimerQueue[i] = null;
                    playerTimerQueue.RemoveAt(i);
                }
            }
        }
    }

    public override void OnStartServer()
    {
        connectedPlayers = new List<Player>();
        playerTimerQueue = new List<PlayerTimer>();
        currentGameModeData = Resources.Load<GameModeData>("GameModes/AttackDefend");

        int size = controlPoints.Length;
        for (int i = 0; i < size; i++)
        {
            controlPoints[i].controlPoint.SetPointOrder(controlPoints[i].order);
            controlPoints[i].controlPoint.OnControllPointCaptured += TeamManagerInstance.OnCapturedControlPoint;
        }

        //controlPoints[0].controlPoint.UnlockCP();
        //controlPoints[size - 1].controlPoint.UnlockCP();

        TeamManagerInstance.GetGameModeData(ref currentGameModeData, size);
    }

    public override void OnStopServer()
    {
        int size = controlPoints.Length;
        for (int i = 0; i < size; i++)
        {
            controlPoints[i].controlPoint.OnControllPointCaptured -= TeamManagerInstance.OnCapturedControlPoint;
        }
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
        Transform randomSpawnPoint = teamBases[playerTeam].GetFreeSpawnPoint(playerToSpawn.gameObject);
        playerToSpawn.SpawnPlayer(randomSpawnPoint.position, randomSpawnPoint.rotation, currentGameModeData.playerRespawnTime);
    }

    [Server]
    public bool CanCaptureControlPoint(ref List<Player> playersInCP, out int mayorTeam, int currentTeamHolder, int pointOrder)
    {
        mayorTeam = 0;
        int[] playersPerTeam = TeamManagerInstance.SeparatePlayersPerTeam(ref playersInCP);

        switch (currentTeamHolder)
        {
            case 0:
                //print($"Point in dispiute: {pointOrder}");
                //print($"Team 1 point to capture: {TeamManagerInstance.GetTeamControlledPoints(0)}");
                //print($"Team 2 point to capture: {TeamManagerInstance.GetTeamControlledPoints(1)}");

                if (playersPerTeam[0] >= currentGameModeData.minNumbPlayersToCaputre && TeamManagerInstance.GetTeamControlledPoints(0) < pointOrder) return false;
                else if (playersPerTeam[1] >= currentGameModeData.minNumbPlayersToCaputre  && TeamManagerInstance.GetTeamControlledPoints(1) > pointOrder) return false;
                break;

            case 1:
                if (TeamManagerInstance.GetTeamControlledPoints(1) > pointOrder) return false;
                break;

            case 2:
                if (TeamManagerInstance.GetTeamControlledPoints(0) < pointOrder) return false;
                break;
        }

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

    [Server]
    string CheckPlayerName(string playerName)
    {
        string newPlayerName = playerName;
        int sameNumberPlayers = 0;

        int size = connectedPlayers.Count;
        for (int i = 0; i < size; i++)
        {
            if (connectedPlayers[i].GetPlayerName().RemovePlayerNumber() == playerName)
                sameNumberPlayers++;
        }

        if (sameNumberPlayers > 0) newPlayerName += $"#{sameNumberPlayers:000}";
        return newPlayerName;
    }

    [Server]
    public void DoActionPerPlayer(System.Action<Player> actionToDo)
    {
        int size = connectedPlayers.Count;
        for (int i = 0; i < size; i++) actionToDo?.Invoke(connectedPlayers[i]);
    }

    [Server]
    public void PlayerOnEnemyBase(Player player)
    {
        playerTimerQueue.Add(new PlayerTimer(ref player, currentGameModeData.timeToReturnToBattlefield, KillPlayer));
        player.RpcPlayerOutOfBounds(player.connectionToClient, currentGameModeData.timeToReturnToBattlefield);
    }

    [Server]
    public void PlayerLeftEnemyBase(Player player)
    {
        RemovePlayerFromTimerQueue(ref player);
        player.RpcPlayerReturned(player.connectionToClient);
    }

    [Server]
    public void KillPlayer(Player playerToKill)
    {
        playerToKill.TakeDamage(99999999);
        playerToKill.SetRespawnTime(playerToKill.GetPlayerRespawnTime() + currentGameModeData.killSelfTime);
    }

    [Server]
    void RemovePlayerFromTimerQueue(ref Player playerToRemove)
    {
        int size = playerTimerQueue.Count;
        for (int i = 0; i < size; i++)
        {
            if (playerTimerQueue[i].IsAffectedPlayer(ref playerToRemove))
            {
                playerTimerQueue[i] = null;
                playerTimerQueue.RemoveAt(i);
                break;
            }
        }
    }

    [Server]
    public void RespawnPlayer(Player playerToRespawn)
    {
        SpawnPlayerByTeam(playerToRespawn);
    }

    [Server]
    public void EndMatch()
    {

    }
}
