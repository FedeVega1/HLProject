using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using Unity.Netcode;

public class Player : Character
{
    [Header("Player")]

    [SerializeField] GameObject playerCamera, playerCanvasPrefab;
    [SerializeField] protected MeshRenderer playerMesh;
    [SerializeField] protected PlayerMovement movementScript;
    [SerializeField] protected float woundedMaxTime;

    protected NetworkVariable<int> playerTeam = new NetworkVariable<int>();
    protected NetworkVariable<bool> isWounded = new NetworkVariable<bool>(), firstSpawn = new NetworkVariable<bool>();
    protected NetworkVariable<string> playerName = new NetworkVariable<string>();
    protected NetworkVariable<double> timeToRespawn = new NetworkVariable<double>();

    public float MaxRespawnTime { get; set; }
    public float BonusWoundTime { get; set; }
    public PlayerCanvas PlayerCanvasScript { get; private set; }

    protected bool onControlPoint;
    protected int currentClassIndex, kills, deaths, revives, score;
    protected double woundedTime;
    protected PlayerInventory inventory;
    protected TeamClassData classData;

    #region Hooks

    void OnTeamChange(int oldTeam, int newTeam) => Debug.LogFormat("{0} Team is {1}", playerName, newTeam);

    #endregion

    protected override void OnServerSpawn()
    {
        base.OnServerSpawn();
        movementScript.freezePlayer.Value = true;
        firstSpawn.Value = true;
    }

    protected override void OnClientSpawn()
    {
        base.OnClientSpawn();
        playerTeam.OnValueChanged += OnTeamChange;
    }

    protected override void OnLocalPlayerSpawn()
    {
        base.OnLocalPlayerSpawn();
        PlayerCanvasScript = Instantiate(playerCanvasPrefab).GetComponent<PlayerCanvas>();
        PlayerCanvasScript.Init(this);
        movementScript.FreezeInputs = true;
        GameModeManager.INS.TeamManagerInstance.OnTicketChange += UpdateMatchTickets_Client;
    }

    //public override void OnStartClient()
    //{
    //    if (isLocalPlayer || IsDead || firstSpawn) return;
    //    playerMesh.enabled = true;
    //}

    protected virtual void Start()
    {
        if (!IsLocalPlayer) Destroy(playerCamera);
        if (!IsDead && firstSpawn.Value) playerMesh.enabled = false;

        inventory = GetComponent<PlayerInventory>();
        inventory.DisablePlayerInputs = true;
    }

    protected override void Update()
    {
        base.Update();

        if (IsLocalPlayer) CheckInputs_Client();
        if (IsServer) PlayerWoundedUpdate();
    }

    void OnDisable()
    {
        GameModeManager.INS.TeamManagerInstance.OnTicketChange -= UpdateMatchTickets_Client;
    }

    #region Get/Set

    public string SetPlayerName(string newName) => playerName.Value = newName;
    public string GetPlayerName() => playerName.Value;

    public int GetPlayerTeam() => playerTeam.Value;
    public void SetPlayerTeam(int newPlayerTeam) => playerTeam.Value = newPlayerTeam;

    public bool PlayerIsMoving() => movementScript.PlayerIsMoving;
    public bool PlayerIsRunning() => movementScript.PlayerIsRunning;

    #endregion

    #region Client

    protected virtual void CheckInputs_Client()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Escape)) GameManager.INS.StopServer();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsServer && IsClient) GameManager.INS.StopServer();
            else GameManager.INS.DisconnectFromServer();
        }

        if (Input.GetKeyDown(KeyCode.Delete)) GameModeManager.INS.KillPlayer_Server(this);
        if (Input.GetKeyDown(KeyCode.F1)) GameModeManager.INS.EndMatch_Server();

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (PlayerCanvasScript.IsClassSelectionMenuOpen || PlayerCanvasScript.IsTeamSelectionMenuOpen) return;
            if (PlayerCanvasScript.IsScoreboardMenuOpen)
            {
                PlayerCanvasScript.ToggleScoreboard(false);
                return;
            }

            TryGetPlayerInfo_Client();
        }

        if (IsDead || firstSpawn.Value) return;

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (PlayerCanvasScript.IsScoreboardMenuOpen) return;
            if (PlayerCanvasScript.IsClassSelectionMenuOpen)
            {
                movementScript.FreezeInputs = false;
                inventory.DisablePlayerInputs = false;
                PlayerCanvasScript.ToggleClassSelection(false);
            }
            else
            {
                movementScript.FreezeInputs = true;
                inventory.DisablePlayerInputs = true;
                PlayerCanvasScript.ToggleTeamSelection(false);
                PlayerCanvasScript.ToggleClassSelection(true);
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            if (PlayerCanvasScript.IsScoreboardMenuOpen) return;
            if (PlayerCanvasScript.IsTeamSelectionMenuOpen)
            {
                movementScript.FreezeInputs = false;
                inventory.DisablePlayerInputs = false;
                PlayerCanvasScript.ToggleTeamSelection(false);
            }
            else
            {
                movementScript.FreezeInputs = true;
                inventory.DisablePlayerInputs = true;
                PlayerCanvasScript.ToggleClassSelection(false);
                PlayerCanvasScript.ToggleTeamSelection(true);
            }

            return;
        }
    }

    public void TrySelectTeam_Client(int team)
    {
        if (!IsLocalPlayer) return;
        RequestPlayerChangeTeam_ServerRpc(team);
    }

    public void TryGetPlayerInfo_Client()
    {
        if (!IsLocalPlayer) return;
        RequestPlayerInfo_ServerRpc();
        PlayerCanvasScript.ToggleScoreboard(true);
    }

    public void UpdateMatchTickets_Client(int team, int tickets)
    {
        if (IsLocalPlayer) PlayerCanvasScript.SetTeamTickets(team - 1, tickets);
        Debug.LogFormat("{0} tickets: {1}", TeamManager.FactionNames[team - 1], tickets);
    }

    public void TrySelectClass_Client(int classIndex)
    {
        if (!IsLocalPlayer) return;
        RequestPlayerChangeClass_ServerRpc(classIndex);
    }

    public void TryPlayerSpawn_Client()
    {
        if (!IsLocalPlayer) return;
        RequestPlayerRespawn_ServerRpc();
    }

    public void TryWoundedGiveUp_Client()
    {
        if (!IsLocalPlayer) return;
        RequestWoundedGiveUp_ServerRpc();
    }

    #endregion

    #region Server

    public override void TakeDamage_Server(float ammount, DamageType damageType = DamageType.Base)
    {
        if (isWounded.Value) return;
        base.TakeDamage_Server(ammount, damageType);
    }

    public void PlayerWoundedUpdate()
    {
        if (IsDead || isInvencible.Value || !isWounded.Value || NetTime < woundedTime) return;
        isWounded.Value = false;
        isDead.Value = true;

        Transform specPoint = GameModeManager.INS.GetSpectatePointByIndex(0);
        MyTransform.position = specPoint.position;
        MyTransform.rotation = specPoint.rotation;

        WoundedCanGiveUp_ClientRpc(SendRpcToPlayer);
        CharacterDied_ClientRpc();
    }

    public void SetAsSpectator_Server() 
    {
        isInvencible.Value = true;

        Transform specPoint = GameModeManager.INS.GetSpectatePointByIndex(0);
        MyTransform.position = specPoint.position;
        MyTransform.rotation = specPoint.rotation;

        movementScript.freezePlayer.Value = false;
        movementScript.spectatorMov.Value = true;
        movementScript.FreezeInputs = false;
    }

    public void SpawnPlayer_Server(Vector3 spawnPosition, Quaternion spawnRotation, float spawnTime)
    {
        //print($"NewPos: {MyTransform.position}");
        InitCharacter_Server();
        MaxRespawnTime = spawnTime;
        movementScript.ForceMoveCharacter_Server(spawnPosition, spawnRotation);
        movementScript.freezePlayer.Value = false;
        //movementScript.RpcToggleFreezePlayer(connectionToClient, false);
        Debug.LogFormat("Server: Setup weapon inventory for {0} player - ClassName: {1}", playerName, classData.className);
        inventory.SetupWeaponInventory_Server(classData.classWeapons, 0);
        PlayerSpawns_ClientRpc();
    }

    protected override void CharacterDies_Server(bool criticalHit)
    {
        if (isDead.Value || isInvencible.Value) return;

        isBleeding.Value = false;
        timeToRespawn.Value = NetTime + MaxRespawnTime;
        woundedTime = criticalHit ? 0 : NetTime + (woundedMaxTime - BonusWoundTime);

        deaths++;
        isWounded.Value = true;
        ShowWoundedHUD_ClientRpc(woundedTime, timeToRespawn.Value, SendRpcToPlayer);
        movementScript.freezePlayer.Value = true;
        OnPlayerDead?.Invoke();
        //movementScript.RpcToggleFreezePlayer(connectionToClient, true);
        //base.CharacterDies();
    }

    public void Init_Server()
    {
        GameModeManager.INS.OnMatchEnded += MatchEnded_Server;
    }

    void MatchEnded_Server(int loosingTeam)
    {
        movementScript.freezePlayer.Value = true;
        movementScript.ToggleCharacterController_Server(false);

        Transform spectPoint = GameModeManager.INS.GetSpectatePointByIndex(0);
        movementScript.MyTransform.position = spectPoint.position;
        movementScript.MyTransform.rotation = spectPoint.rotation;

        MatchEndPlayerSetup_ClientRpc(loosingTeam, GameModeManager.INS.timeToChangeLevel);
    }

    public void SetPlayerClass_Server(TeamClassData classData, int classIndex)
    {
        currentClassIndex = classIndex;
        this.classData = classData;
        movementScript.spectatorMov.Value = false;
        print($"Server: CurrentClass {classData.className}");
    }

    public float GetPlayerCameraXAxis_Server() => movementScript.CameraXAxis;

    public PlayerScoreboardInfo GetPlayerScoreboardInfo_Server(int requesterTeam, bool ignoreTeamFilter = false)
    {
        if (ignoreTeamFilter || requesterTeam == playerTeam.Value)
        {
            return new PlayerScoreboardInfo(playerTeam.Value, currentClassIndex, playerName.Value, score, revives, deaths, IsDead, IsLocalPlayer);
        }

        return new PlayerScoreboardInfo(playerTeam.Value, playerName.Value, score);
    }

    public void UpdatePlayerScore_Server(int ammount) => score += ammount;

    #endregion

    #region ServerCommands

    [ServerRpc]
    void RequestPlayerInfo_ServerRpc()
    {
        PlayerScoreboardInfo[] info = GameModeManager.INS.GetScoreboardInfo_Server(playerTeam.Value);
        ShowScoreboard_ClientRpc(info, SendRpcToPlayer);
    }

    [ServerRpc]
    void RequestPlayerChangeTeam_ServerRpc(int team)
    {
        movementScript.freezePlayer.Value = true;
        if (!isDead.Value && !firstSpawn.Value) GameModeManager.INS.KillPlayer_Server(this);
        classData = null;
        GameModeManager.INS.TeamManagerInstance.PlayerSelectedTeam_Server(this, team);
    }

    [ServerRpc]
    void RequestPlayerChangeClass_ServerRpc(int teamClass)
    {
        GameModeManager.INS.PlayerChangeClass_Server(this, teamClass);
    }

    [ServerRpc]
    void RequestPlayerRespawn_ServerRpc()
    {
        if ((!isDead .Value && !firstSpawn.Value) || NetTime < timeToRespawn.Value || classData == null) return;
        GameModeManager.INS.RespawnPlayer_Server(this);
        firstSpawn.Value = false;
    }

    [ServerRpc]
    void RequestWoundedGiveUp_ServerRpc()
    {
        if (isDead.Value || !isWounded.Value) return;
        woundedTime = 0;
        //isWounded = false;
        //RpcCharacterDied();
        //RpcWoundedCanGiveUp(connectionToClient);
    }

    #endregion

    #region TargetRpc

    [ClientRpc]
    public void ShowScoreboard_ClientRpc(PlayerScoreboardInfo[] scoreboardInfo, ClientRpcParams rpcParams = default)
    {
        if (IsOwner || !PlayerCanvasScript.IsScoreboardMenuOpen) return;
        PlayerCanvasScript.InitScoreboard(scoreboardInfo, playerTeam.Value);
    }

    [ClientRpc]
    void WoundedCanGiveUp_ClientRpc(ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        PlayerCanvasScript.PlayerNotWounded();
        PlayerCanvasScript.ToggleClassSelection(true);
    }

    [ClientRpc]
    public void ShowGreetings_ClientRpc(int team1Tickets, int team2Tickets, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;

        PlayerCanvasScript.ToggleTeamSelection(true);
        PlayerCanvasScript.SetTeamTickets(0, team1Tickets);
        PlayerCanvasScript.SetTeamTickets(1, team2Tickets);
    }

    [ClientRpc]
    public void TeamSelectionError_ClientRpc(int error, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        Debug.LogErrorFormat("Client: TeamSelection error code: {0}", error);
    }

    [ClientRpc]
    public void TeamSelectionSuccess_ClientRpc(int team, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        PlayerCanvasScript.ToggleTeamSelection(false);

        if (team > 0)
        {
            PlayerCanvasScript.OnTeamSelection(team);
            PlayerCanvasScript.ToggleClassSelection(true);
        }
    }

    [ClientRpc]
    public void ClassSelectionError_ClientRpc(int error, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        Debug.LogErrorFormat("Client: ClassSelection error code: {0}", error);
        PlayerCanvasScript.ToggleSpawnButton(true);
    }

    [ClientRpc]
    public void ClassSelectionSuccess_ClientRpc(int classIndex, ClientRpcParams rpcParams = default)
    { 
        if (IsOwner) return;
        if (isDead.Value || firstSpawn.Value) PlayerCanvasScript.ToggleSpawnButton(true);
        PlayerCanvasScript.OnClassSelection(classIndex);
        Debug.LogFormat("Client: CurrentClass {0}", classIndex);
    }

    [ClientRpc]
    public void OnControlPoint_ClientRpc(int currentCPController, float cpCaptureProgress, int defyingTeam, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        PlayerCanvasScript.OnControlPoint(currentCPController, defyingTeam, cpCaptureProgress);
        onControlPoint = true;
    }

    [ClientRpc]
    public void ExitControlPoint_ClientRpc(ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        PlayerCanvasScript.OnExitControlPoint();
        onControlPoint = false;
    }

    [ClientRpc]
    public void UpdateCPProgress_ClientRpc(float progress, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        PlayerCanvasScript.UpdateCPProgress(progress);
    }

    [ClientRpc]
    public void PlayerOutOfBounds_ClientRpc(float timeToReturn, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        PlayerCanvasScript.PlayerOutOfBounds(timeToReturn);
    }

    [ClientRpc]
    public void PlayerReturned_ClientRpc(ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        PlayerCanvasScript.PlayerInBounds();
    }

    [ClientRpc]
    void ShowWoundedHUD_ClientRpc(double _woundedTime, double _respawnTime, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        movementScript.FreezeInputs = true;
        inventory.DisablePlayerInputs = true;
        PlayerCanvasScript.PlayerIsWounded(_woundedTime);
        PlayerCanvasScript.ShowRespawnTimer(_respawnTime);
    }

    [ClientRpc]
    public void PlayerConnected_ClientRpc(PlayerScoreboardInfo playerInfo, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        Debug.LogFormat("Client: Player {0} connected", playerInfo.playerName);
        //if (!PlayerCanvasScript.IsScoreboardMenuOpen) return;
        //PlayerCanvasScript.AddPlayerToScoreboard(playerInfo);
    }

    [ClientRpc]
    public void PlayerSelectedTeam_ClientRpc(PlayerScoreboardInfo playerInfo, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        Debug.LogFormat("Client: Player {0} changes his team to {1}", playerInfo.playerName, playerInfo.playerTeam);
        if (!PlayerCanvasScript.IsScoreboardMenuOpen) return;
        PlayerCanvasScript.AddPlayerToScoreboard(playerInfo);
    }

    [ClientRpc]
    public void PlayerDisconnected_ClientRpc(string playerName, ClientRpcParams rpcParams = default)
    {
        if (IsOwner) return;
        Debug.LogFormat("Client: Player {0} disconnected", playerName);
        PlayerCanvasScript.RemovePlayerFromScoreboard(playerName);
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    protected override void CharacterDied_ClientRpc()
    {
        if (IsLocalPlayer)
        {
            movementScript.FreezeInputs = true;
            inventory.DisablePlayerInputs = true;
        }

        playerMesh.enabled = false;
    }

    [ClientRpc]
    public void ControlPointCaptured_ClientRpc(int cpTeam, int oldTeam, string cpName)
    {
        if (PlayerCanvasScript == null) return;
        if (onControlPoint) PlayerCanvasScript.OnPointCaptured(cpTeam, oldTeam != 0 ? oldTeam : 1);
        PlayerCanvasScript.NewCapturedControlPoint(cpTeam, cpName);
    }

    [ClientRpc]
    public void PlayerSpawns_ClientRpc()
    {
        if (IsLocalPlayer)
        {
            PlayerCanvasScript.PlayerRespawn();
            PlayerCanvasScript.ToggleWeaponInfo(true);
            movementScript.FreezeInputs = false;
            inventory.DisablePlayerInputs = false;
            //inventory.SetupWeaponInventory(classData.classWeapons, 0);
        }

        playerMesh.enabled = true;
    }

    [ClientRpc]
    public void MatchEndPlayerSetup_ClientRpc(int loosingTeam, float timeToChangeLevel)
    {
        if (IsLocalPlayer)
        {
            PlayerCanvasScript.ShowGameOverScreen(loosingTeam, timeToChangeLevel);
        }

        playerMesh.enabled = false;
    }

    #endregion
}
