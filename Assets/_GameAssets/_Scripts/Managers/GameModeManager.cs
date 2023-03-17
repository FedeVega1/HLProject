using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Netcode;

public class GameModeManager : CommonNetworkBehaviour
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
        if (!IsServer) return;

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

        if (!enableScoreboardUpdate || NetTime < scoreBoardUpdateTimer) return;
        DoActionPerPlayer_Server(UpdateScoreboard);
        scoreBoardUpdateTimer = NetTime + scoreBoardUpdateTime;
    }

    protected override void OnServerSpawn()
    {
        connectedPlayers = new List<Player>();
        playerTimerQueue = new List<PlayerTimer>();

        currentGameModeData = Resources.Load<GameModeData>(Path.Combine("GameModes", "AttackDefend"));

        int size = controlPoints.Length;
        for (int i = 0; i < size; i++)
        {
            controlPoints[i].controlPoint.SetPointOrder(controlPoints[i].order);
            controlPoints[i].controlPoint.OnControllPointCaptured += TeamManagerInstance.OnCapturedControlPoint_Server;
        }

        //controlPoints[0].controlPoint.UnlockCP();
        //controlPoints[size - 1].controlPoint.UnlockCP();

        TeamManagerInstance.GetGameModeData_Server(ref currentGameModeData, size);
        //NetManager.INS.OnClientDisconnects += OnPlayerDisconnects;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnects_Server;
        enableScoreboardUpdate = true;
    }

    protected override void OnServerDespawn()
    {
        int size = controlPoints.Length;
        for (int i = 0; i < size; i++)
        {
            controlPoints[i].controlPoint.OnControllPointCaptured -= TeamManagerInstance.OnCapturedControlPoint_Server;
        }
    }

    public GameObject SpawnPlayerObject_Server(GameObject playerPrefab, string playerName)
    {
        GameObject playerObject = Instantiate(playerPrefab, spectatorPoints[0].position, spectatorPoints[0].rotation);
        Player playerScript = playerObject.GetComponent<Player>();
        
        if (playerScript == null)
        {
            Debug.LogError("Player prefab doesn't have a Player Component!");
            return null;
        }

        playerScript.SetPlayerName(CheckPlayerName_Server(playerName));
        playerScript.Init_Server();
        //connectedPlayers.Add(playerScript);

        return playerObject;
    }

    public void OnPlayerConnection_Server(GameObject playerObject)
    {
        Player playerScript = playerObject.GetComponent<Player>();

        if (playerScript == null)
        {
            Debug.LogError("Player prefab doesn't have a Player Component!");
            return;
        }

        connectedPlayers.Add(playerScript);
        currentConnPlayerIndex = connectedPlayers.Count - 1;
        DoActionPerPlayer_Server(PlayerConnectedEvent);
        
        if (!matchEnded)
        {
            if (playerScript.IsSpawned)
            {
                ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { playerScript.NetworkBehaviourId } } };
                playerScript.ShowGreetings_ClientRpc(TeamManagerInstance.GetTicketsFromTeam_Server(1), TeamManagerInstance.GetTicketsFromTeam_Server(2));
            }
        }
    }

    public void OnPlayerDisconnects_Server(ulong clientID)
    {
        int size = connectedPlayers.Count;
        for (int i = 0; i < size; i++)
        {
            if (connectedPlayers[i].NetworkBehaviourId == clientID)
            {
                teamManager.OnClientDisconnects(connectedPlayers[i]);
                currentDisconnPlayerIndex = i;
                DoActionPerPlayer_Server(PlayerDisconnectedEvent);
                connectedPlayers.RemoveAt(i);
                return;
            }
        }

        Debug.LogErrorFormat("Client {0} disconnected but was not found in the connected players list", clientID);
    }

    public void SpawnPlayerByTeam_Server(Player playerToSpawn)
    {
        if (matchEnded) return;
        int playerTeam = playerToSpawn.GetPlayerTeam() - 1;
        Transform randomSpawnPoint = teamBases[playerTeam].GetFreeSpawnPoint(playerToSpawn.gameObject);
        playerToSpawn.SpawnPlayer_Server(randomSpawnPoint.position, randomSpawnPoint.rotation, currentGameModeData.playerRespawnTime);
    }

    public bool CanCaptureControlPoint_Server(ref List<Player> playersInCP, out int mayorTeam, int currentTeamHolder, int pointOrder)
    {
        mayorTeam = 0;
        if (matchEnded) return false;

        int[] playersPerTeam = TeamManagerInstance.SeparatePlayersPerTeam_Server(ref playersInCP);

        switch (currentTeamHolder)
        {
            case 0:
                //print($"Point in dispiute: {pointOrder}");
                //print($"Team 1 point to capture: {TeamManagerInstance.GetTeamControlledPoints(0)}");
                //print($"Team 2 point to capture: {TeamManagerInstance.GetTeamControlledPoints(1)}");

                if (playersPerTeam[0] >= currentGameModeData.minNumbPlayersToCaputre && TeamManagerInstance.GetTeamControlledPoints_Server(0) < pointOrder) return false;
                else if (playersPerTeam[1] >= currentGameModeData.minNumbPlayersToCaputre  && TeamManagerInstance.GetTeamControlledPoints_Server(1) > pointOrder) return false;
                break;

            case 1:
                if (TeamManagerInstance.GetTeamControlledPoints_Server(1) > pointOrder) return false;
                break;

            case 2:
                if (TeamManagerInstance.GetTeamControlledPoints_Server(0) < pointOrder) return false;
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

    string CheckPlayerName_Server(string playerName)
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

    public void DoActionPerPlayer_Server(System.Action<Player> actionToDo)
    {
        int size = connectedPlayers.Count;
        for (int i = 0; i < size; i++) actionToDo?.Invoke(connectedPlayers[i]);
    }

    public void PlayerOnEnemyBase_Server(Player player)
    {
        if (matchEnded) return;
        playerTimerQueue.Add(new PlayerTimer(ref player, currentGameModeData.timeToReturnToBattlefield, KillPlayer_Server));
        player.PlayerOutOfBounds_ClientRpc(currentGameModeData.timeToReturnToBattlefield);
    }

    public void PlayerLeftEnemyBase_Server(Player player)
    {
        if (matchEnded) return;
        RemovePlayerFromTimerQueue_Server(ref player);
        ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { player.NetworkBehaviourId } } };
        player.PlayerReturned_ClientRpc(rpcParams);
    }

    public void KillPlayer_Server(Player playerToKill)
    {
        if (matchEnded) return;
        playerToKill.TakeDamage_Server(99999999);
        playerToKill.MaxRespawnTime += currentGameModeData.killSelfTime;
        playerToKill.BonusWoundTime = 0;
    }

    void RemovePlayerFromTimerQueue_Server(ref Player playerToRemove)
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

    public void RespawnPlayer_Server(Player playerToRespawn)
    {
        if (matchEnded) return;
        SpawnPlayerByTeam_Server(playerToRespawn);
    }

    public void EndMatch_Server()
    {
        print("GameOver");
        matchEnded = true;
        enabled = false;

        int size = playerTimerQueue.Count;
        for (int i = 0; i < size; i++) playerTimerQueue[i] = null;
        playerTimerQueue.Clear();

        int loosingTeam = TeamManagerInstance.GetLoosingTeam_Server();
        OnMatchEnded?.Invoke(loosingTeam);
        LeanTween.value(0, 1, timeToChangeLevel).setOnComplete(() => { GameManager.INS.ServerChangeLevel(); });
    }

    public void PlayerChangeClass_Server(Player playerScript, int classIndex)
    {
        ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { playerScript.NetworkBehaviourId } } };
        if (classIndex < 0 || classIndex >= classData.Length)
        {
            Debug.LogError("Class index out of bounds!");
            if (playerScript.IsSpawned) playerScript.ClassSelectionError_ClientRpc(0x00, rpcParams);
            return;
        }

        if (classData[classIndex].teamSpecific != 0)
        {
            if (classData[classIndex].teamSpecific != playerScript.GetPlayerTeam())
            {
                Debug.LogErrorFormat("{0} tried to use {1} from {2}", playerScript.GetPlayerName(), classData[classIndex].className, TeamManager.FactionNames[classData[classIndex].teamSpecific - 1]);
                if (playerScript.IsSpawned) playerScript.ClassSelectionError_ClientRpc(0x01, rpcParams);
                return;
            }
        }

        playerScript.SetPlayerClass_Server(classData[classIndex], classIndex);
        if (playerScript.IsSpawned) playerScript.ClassSelectionSuccess_ClientRpc(classIndex, rpcParams);
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

    public PlayerScoreboardInfo[] GetScoreboardInfo_Server(int playerTeam)
    {
        PlayerScoreboardInfo[] scoreboardInfo = new PlayerScoreboardInfo[connectedPlayers.Count];
        for (int i = 0, n = 0; i < TeamManager.MAXTEAMS; i++)
        {
            List<Player> players = teamManager.GetPlayersOnTeam_Server(i);
            int size = players.Count;
            for (int j = 0; j < size; j++, n++)
            {
                PlayerScoreboardInfo info = players[j].GetPlayerScoreboardInfo_Server(playerTeam);
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
        DoActionPerPlayer_Server(_ => PlayerSelectedTeamEvent(_, selectedTeam));
    }

    void PlayerDisconnectedEvent(Player player)
    {
        if (!player.IsSpawned) return;
        ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { player.NetworkBehaviourId } } };
        player.PlayerDisconnected_ClientRpc(connectedPlayers[currentDisconnPlayerIndex].GetPlayerName(), rpcParams);
    }

    void PlayerConnectedEvent(Player player)
    {
        int playerTeam = player.GetPlayerTeam() - 1;
        if (!player.IsSpawned)
        {
            ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { player.NetworkBehaviourId } } };
            player.PlayerConnected_ClientRpc(connectedPlayers[currentConnPlayerIndex].GetPlayerScoreboardInfo_Server(playerTeam), rpcParams);
        }
    }

    void PlayerSelectedTeamEvent(Player player, int selectedTeam)
    {
        //int playerTeam = player.GetPlayerTeam();
        if (!player.IsSpawned)
        {
            ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { player.NetworkBehaviourId } } };
            player.PlayerSelectedTeam_ClientRpc(connectedPlayers[currentConnPlayerIndex].GetPlayerScoreboardInfo_Server(selectedTeam, true), rpcParams);
        }
    }

    void UpdateScoreboard(Player player)
    {
        PlayerScoreboardInfo[] info = GetScoreboardInfo_Server(player.GetPlayerTeam());
        if (player.IsSpawned)
        {
            ClientRpcParams rpcParams = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { player.NetworkBehaviourId } } };
            player.ShowScoreboard_ClientRpc(info, rpcParams);
        }
    }

    public void AddDummyPlayer()
    {
        GameObject playerObject = SpawnPlayerObject_Server(dummyPlayerPrefab, "DummyPlayer");
        OnPlayerConnection_Server(playerObject);
        playerObject.GetComponent<NetworkObject>().Spawn();
    }
}
