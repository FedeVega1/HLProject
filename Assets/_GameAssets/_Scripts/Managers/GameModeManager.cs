using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
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

    [SerializeField] TeamManager teamManager;
    [SerializeField] Transform[] spectatorPoints;
    [SerializeField] ControlPointNode[] controlPoints;
    [SerializeField] TeamBase[] teamBases;
    [SerializeField] Transform clientCamera;
    [SerializeField] float scoreBoardUpdateTime = 2;
    [SerializeField] GameObject dummyPlayerPrefab;
    [SerializeField] Camera clientVCamera;

    public TeamManager TeamManagerInstance => teamManager;

    public float timeToChangeLevel = 15;

    bool matchEnded, enableScoreboardUpdate;
    double scoreBoardUpdateTimer;
    GameModeData currentGameModeData;
    TeamClassData[] classData;
    List<Player> connectedPlayers;
    List<PlayerTimer> playerTimerQueue;
    int currentDisconnPlayerIndex, currentConnPlayerIndex;

    public System.Action<int> OnMatchEnded;

    void Awake()
    {
        if (INS == null) INS = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        classData = Resources.LoadAll<TeamClassData>("TeamClasses");
    }

    void Update()
    {
        if (!isServer) return;

        if (Input.GetKeyDown(KeyCode.O)) AddDummyPlayer();
            
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

        if (!enableScoreboardUpdate || NetworkTime.time < scoreBoardUpdateTimer) return;
        DoActionPerPlayer(UpdateScoreboard);
        scoreBoardUpdateTimer = NetworkTime.time + scoreBoardUpdateTime;
    }

    public override void OnStartServer()
    {
        connectedPlayers = new List<Player>();
        playerTimerQueue = new List<PlayerTimer>();

        currentGameModeData = Resources.Load<GameModeData>(Path.Combine("GameModes", "AttackDefend"));

        int size = controlPoints.Length;
        for (int i = 0; i < size; i++)
        {
            controlPoints[i].controlPoint.SetPointOrder(controlPoints[i].order);
            controlPoints[i].controlPoint.OnControllPointCaptured += TeamManagerInstance.OnCapturedControlPoint;
        }

        //controlPoints[0].controlPoint.UnlockCP();
        //controlPoints[size - 1].controlPoint.UnlockCP();

        TeamManagerInstance.GetGameModeData(ref currentGameModeData, size);
        NetManager.INS.OnClientDisconnects += OnPlayerDisconnects;
        enableScoreboardUpdate = true;
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
    public GameObject SpawnPlayerObject(GameObject playerPrefab, string playerName)
    {
        GameObject playerObject = Instantiate(playerPrefab, spectatorPoints[0].position, spectatorPoints[0].rotation);
        Player playerScript = playerObject.GetComponent<Player>();
        
        if (playerScript == null)
        {
            Debug.LogError("Player prefab doesn't have a Player Component!");
            return null;
        }

        playerScript.SetPlayerName(CheckPlayerName(playerName));
        playerScript.Init();
        //connectedPlayers.Add(playerScript);

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

        connectedPlayers.Add(playerScript);
        currentConnPlayerIndex = connectedPlayers.Count - 1;
        DoActionPerPlayer(PlayerConnectedEvent);
        
        if (!matchEnded)
        {
            if (playerScript.connectionToClient != null) 
                playerScript.RpcShowGreetings(playerScript.connectionToClient, TeamManagerInstance.GetTicketsFromTeam(1), TeamManagerInstance.GetTicketsFromTeam(2));
        }
    }

    [Server]
    public void OnPlayerDisconnects(NetworkConnection conn)
    {
        int size = connectedPlayers.Count;
        for (int i = 0; i < size; i++)
        {
            if (connectedPlayers[i].connectionToClient.connectionId == conn.connectionId)
            {
                teamManager.OnClientDisconnects(connectedPlayers[i]);
                currentDisconnPlayerIndex = i;
                DoActionPerPlayer(PlayerDisconnectedEvent);
                connectedPlayers.RemoveAt(i);
                return;
            }
        }

        Debug.LogError($"Client {conn.connectionId} disconnected but was not found in the connected players list");
    }

    [Server]
    public void SpawnPlayerByTeam(Player playerToSpawn)
    {
        if (matchEnded) return;
        int playerTeam = playerToSpawn.GetPlayerTeam() - 1;
        Transform randomSpawnPoint = teamBases[playerTeam].GetFreeSpawnPoint(playerToSpawn.gameObject);
        playerToSpawn.SpawnPlayer(randomSpawnPoint.position, randomSpawnPoint.rotation, currentGameModeData.playerRespawnTime);
    }

    [Server]
    public bool CanCaptureControlPoint(ref List<Player> playersInCP, out int mayorTeam, int currentTeamHolder, int pointOrder)
    {
        mayorTeam = 0;
        if (matchEnded) return false;

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
        if (matchEnded) return;
        playerTimerQueue.Add(new PlayerTimer(ref player, currentGameModeData.timeToReturnToBattlefield, KillPlayer));
        player.RpcPlayerOutOfBounds(player.connectionToClient, currentGameModeData.timeToReturnToBattlefield);
    }

    [Server]
    public void PlayerLeftEnemyBase(Player player)
    {
        if (matchEnded) return;
        RemovePlayerFromTimerQueue(ref player);
        player.RpcPlayerReturned(player.connectionToClient);
    }

    [Server]
    public void KillPlayer(Player playerToKill)
    {
        if (matchEnded) return;
        playerToKill.TakeDamage(99999999);
        playerToKill.MaxRespawnTime += currentGameModeData.killSelfTime;
        playerToKill.BonusWoundTime = 0;
    }

    [Server]
    void RemovePlayerFromTimerQueue(ref Player playerToRemove)
    {
        if (matchEnded) return;

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
        if (matchEnded) return;
        SpawnPlayerByTeam(playerToRespawn);
    }

    [Server]
    public void EndMatch()
    {
        print("GameOver");
        matchEnded = true;
        enabled = false;

        int size = playerTimerQueue.Count;
        for (int i = 0; i < size; i++) playerTimerQueue[i] = null;
        playerTimerQueue.Clear();

        int loosingTeam = TeamManagerInstance.GetLoosingTeam();
        OnMatchEnded?.Invoke(loosingTeam);
        LeanTween.value(0, 1, timeToChangeLevel).setOnComplete(() => { GameManager.INS.ServerChangeLevel(); });
    }

    [Server]
    public void PlayerChangeClass(Player playerScript, int classIndex)
    {
        if (classIndex < 0 || classIndex >= classData.Length)
        {
            Debug.LogError("Class index out of bounds!");
            if (playerScript.connectionToClient != null) playerScript.RpcClassSelectionError(playerScript.connectionToClient, 0x00);
            return;
        }

        if (classData[classIndex].teamSpecific != 0)
        {
            if (classData[classIndex].teamSpecific != playerScript.GetPlayerTeam())
            {
                Debug.LogError($"{playerScript.GetPlayerName()} tried to use {classData[classIndex].className} from {TeamManager.FactionNames[classData[classIndex].teamSpecific - 1]}");
                if (playerScript.connectionToClient != null) playerScript.RpcClassSelectionError(playerScript.connectionToClient, 0x01);
                return;
            }
        }

        playerScript.SetPlayerClass(classData[classIndex], classIndex);
        if (playerScript.connectionToClient != null) playerScript.RpcClassSelectionSuccess(playerScript.connectionToClient, classIndex);
    }

    public Transform GetSpectatePointByIndex(int index) => spectatorPoints[index];

    public TeamClassData[] GetClassData() => classData;
    public Transform GetClientCamera() => clientCamera;

    public int GetPlayerIndex(Player playerScript)
    {
        if (playerScript == null) return -1;
        int size = connectedPlayers.Count;
        for (int i = 0; i < size; i++)
        {
            if (connectedPlayers[i] == playerScript)
                return i;
        }

        return -1;
    }

    [Server]
    public PlayerScoreboardInfo[] GetScoreboardInfo(int playerTeam)
    {
        PlayerScoreboardInfo[] scoreboardInfo = new PlayerScoreboardInfo[connectedPlayers.Count];
        for (int i = 0, n = 0; i < TeamManager.MAXTEAMS; i++)
        {
            List<Player> players = teamManager.GetPlayersOnTeam(i);
            int size = players.Count;
            for (int j = 0; j < size; j++, n++)
            {
                PlayerScoreboardInfo info = players[j].GetPlayerScoreboardInfo(playerTeam);
                scoreboardInfo[n] = info;
            }
        }

        return scoreboardInfo;
    }

    public Sprite[] GetClassesSprites()
    {
        int size = classData.Length;
        Sprite[] classesSprites = new Sprite[size];
        for (int i = 0; i < size; i++)
            classesSprites[i] = classData[i].classSprite;
        
        return classesSprites;
    }

    public void OnPlayerSelectedTeam(Player player, int selectedTeam)
    {
        currentConnPlayerIndex = GetPlayerIndex(player);
        DoActionPerPlayer(_ => PlayerSelectedTeamEvent(_, selectedTeam));
    }

    void PlayerDisconnectedEvent(Player player)
    {
        if (player.connectionToClient != null) 
            player.RpcPlayerDisconnected(player.connectionToClient, connectedPlayers[currentDisconnPlayerIndex].GetPlayerName());
    }

    void PlayerConnectedEvent(Player player)
    {
        int playerTeam = player.GetPlayerTeam() - 1;
        if (player.connectionToClient != null) player.RpcPlayerConnected(player.connectionToClient, connectedPlayers[currentConnPlayerIndex].GetPlayerScoreboardInfo(playerTeam));
    }

    void PlayerSelectedTeamEvent(Player player, int selectedTeam)
    {
        //int playerTeam = player.GetPlayerTeam();
        if (player.connectionToClient != null) player.RpcPlayerSelectedTeam(player.connectionToClient, connectedPlayers[currentConnPlayerIndex].GetPlayerScoreboardInfo(selectedTeam, true));
    }

    void UpdateScoreboard(Player player)
    {
        PlayerScoreboardInfo[] info = GetScoreboardInfo(player.GetPlayerTeam());
        if (player.connectionToClient != null) player.RpcShowScoreboard(player.connectionToClient, info);
    }

    public void AddDummyPlayer()
    {
        GameObject playerObject = SpawnPlayerObject(dummyPlayerPrefab, "DummyPlayer");
        OnPlayerConnection(playerObject);
        NetworkServer.Spawn(playerObject);
    }

    [Client]
    public void SetClientVCameraFOV(float fov) => clientVCamera.fieldOfView = Mathf.Clamp(fov, 40, 120);
}
