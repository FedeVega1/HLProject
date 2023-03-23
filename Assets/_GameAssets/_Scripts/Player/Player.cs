using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using Mirror;

public class Player : Character
{
    [Header("Player")]

    [SerializeField] GameObject playerCamera, playerCanvasPrefab;
    [SerializeField] protected MeshRenderer playerMesh;
    [SerializeField] protected PlayerMovement movementScript;
    [SerializeField] protected float woundedMaxTime;

    [SyncVar(hook = nameof(OnTeamChange))] protected int playerTeam;
    [SyncVar] protected bool isWounded, firstSpawn;
    [SyncVar] protected string playerName;
    [SyncVar] protected double timeToRespawn;

    public float MaxRespawnTime { get; set; }
    public float BonusWoundTime { get; set; }
    public PlayerCanvas PlayerCanvasScript { get; private set; }

    protected bool onControlPoint;
    protected int currentClassIndex, kills, deaths, revives, score;
    protected double woundedTime;
    protected PlayerInventory inventory;
    protected TeamClassData classData;

    #region Hooks

    void OnTeamChange(int oldTeam, int newTeam) => print($"{playerName} Team is {newTeam}");

    #endregion

    public override void OnStartServer()
    {
        base.OnStartServer();
        movementScript.freezePlayer = true;
        firstSpawn = true;
    }

    public override void OnStartLocalPlayer()
    {
        PlayerCanvasScript = Instantiate(playerCanvasPrefab).GetComponent<PlayerCanvas>();
        PlayerCanvasScript.Init(this);
        movementScript.FreezeInputs = true;
        GameModeManager.INS.TeamManagerInstance.OnTicketChange += UpdateMatchTickets;
    }

    //public override void OnStartClient()
    //{
    //    if (isLocalPlayer || IsDead || firstSpawn) return;
    //    playerMesh.enabled = true;
    //}

    protected virtual void Start()
    {
        if (!isLocalPlayer) Destroy(playerCamera);
        if (!IsDead && firstSpawn) playerMesh.enabled = false;

        inventory = GetComponent<PlayerInventory>();
        inventory.DisablePlayerInputs = true;
    }

    protected override void Update()
    {
        base.Update();

        if (isLocalPlayer) CheckInputs();
        if (isServer) PlayerWoundedUpdate();
    }

    void OnDisable()
    {
        GameModeManager.INS.TeamManagerInstance.OnTicketChange -= UpdateMatchTickets;
    }

    #region Get/Set

    public string SetPlayerName(string newName) => playerName = newName;
    public string GetPlayerName() => playerName;

    public int GetPlayerTeam() => playerTeam;
    public void SetPlayerTeam(int newPlayerTeam) => playerTeam = newPlayerTeam;

    public bool PlayerIsMoving() => movementScript.PlayerIsMoving;
    public bool PlayerIsRunning() => movementScript.PlayerIsRunning;

    #endregion

    #region Client

    [Client]
    protected virtual void CheckInputs()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isServer && isClient) GameManager.INS.StopServer();
            else GameManager.INS.DisconnectFromServer();
        }

        if (Input.GetKeyDown(KeyCode.Delete)) GameModeManager.INS.KillPlayer(this);
        if (Input.GetKeyDown(KeyCode.F1)) GameModeManager.INS.EndMatch();

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (PlayerCanvasScript.IsClassSelectionMenuOpen || PlayerCanvasScript.IsTeamSelectionMenuOpen) return;
            if (PlayerCanvasScript.IsScoreboardMenuOpen)
            {
                PlayerCanvasScript.ToggleScoreboard(false);
                return;
            }

            TryGetPlayerInfo();
        }

        if (IsDead || firstSpawn) return;

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

    [Client]
    public void TrySelectTeam(int team)
    {
        if (!isLocalPlayer) return;
        CmdRequestPlayerChangeTeam(team);
    }

    public void TryGetPlayerInfo()
    {
        if (!isLocalPlayer) return;
        CmdRequestPlayerInfo();
        PlayerCanvasScript.ToggleScoreboard(true);
    }

    [Client]
    public void UpdateMatchTickets(int team, int tickets)
    {
        if (isLocalPlayer) PlayerCanvasScript.SetTeamTickets(team - 1, tickets);
        print($"{TeamManager.FactionNames[team - 1]} tickets: {tickets}");
    }

    [Client]
    public void TrySelectClass(int classIndex)
    {
        if (!isLocalPlayer) return;
        CmdRequestPlayerChangeClass(classIndex);
    }

    [Client]
    public void TryPlayerSpawn()
    {
        if (!isLocalPlayer) return;
        CmdRequestPlayerRespawn();
    }

    [Client]
    public void TryWoundedGiveUp()
    {
        if (!isLocalPlayer) return;
        CmdRequestWoundedGiveUp();
    }

    #endregion

    #region Server

    [Server]
    public override void TakeDamage(float ammount, DamageType damageType = DamageType.Base)
    {
        if (isWounded) return;
        base.TakeDamage(ammount, damageType);
    }

    [Server]
    public void PlayerWoundedUpdate()
    {
        if (isDead || isInvencible || !isWounded || NetworkTime.time < woundedTime) return;
        isWounded = false;
        isDead = true;

        Transform specPoint = GameModeManager.INS.GetSpectatePointByIndex(0);
        MyTransform.position = specPoint.position;
        MyTransform.rotation = specPoint.rotation;

        RpcWoundedCanGiveUp(connectionToClient);
        RpcCharacterDied();
    }

    [Server]
    public void SetAsSpectator() 
    {
        isInvencible = true;

        Transform specPoint = GameModeManager.INS.GetSpectatePointByIndex(0);
        MyTransform.position = specPoint.position;
        MyTransform.rotation = specPoint.rotation;

        movementScript.freezePlayer = false;
        movementScript.spectatorMov = true;
        movementScript.FreezeInputs = false;
    }

    [Server]
    public void SpawnPlayer(Vector3 spawnPosition, Quaternion spawnRotation, float spawnTime)
    {
        //print($"NewPos: {MyTransform.position}");
        InitCharacter();
        MaxRespawnTime = spawnTime;
        movementScript.ForceMoveCharacter(spawnPosition, spawnRotation);
        movementScript.freezePlayer = false;
        //movementScript.RpcToggleFreezePlayer(connectionToClient, false);
        print($"Server: Setup weapon inventory for {playerName} player - ClassName: {classData.className}");
        inventory.SetupWeaponInventory(classData.classWeapons, 0);
        RpcPlayerSpawns();
    }

    [Server]
    protected override void CharacterDies(bool criticalHit)
    {
        if (isDead || isInvencible) return;

        isBleeding = false;
        timeToRespawn = NetworkTime.time + MaxRespawnTime;
        woundedTime = criticalHit ? 0 : NetworkTime.time + (woundedMaxTime - BonusWoundTime);

        deaths++;
        isWounded = true;
        RpcShowWoundedHUD(connectionToClient, woundedTime, timeToRespawn);
        movementScript.freezePlayer = true;
        OnPlayerDead?.Invoke();
        //movementScript.RpcToggleFreezePlayer(connectionToClient, true);
        //base.CharacterDies();
    }

    [Server]
    public void Init()
    {
        GameModeManager.INS.OnMatchEnded += MatchEnded;
    }

    [Server]
    void MatchEnded(int loosingTeam)
    {
        movementScript.freezePlayer = true;
        movementScript.ToggleCharacterController(false);

        Transform spectPoint = GameModeManager.INS.GetSpectatePointByIndex(0);
        movementScript.MyTransform.position = spectPoint.position;
        movementScript.MyTransform.rotation = spectPoint.rotation;

        RpcMatchEndPlayerSetup(loosingTeam, GameModeManager.INS.timeToChangeLevel);
    }

    [Server]
    public void SetPlayerClass(TeamClassData classData, int classIndex)
    {
        currentClassIndex = classIndex;
        this.classData = classData;
        movementScript.spectatorMov = false;
        print($"Server: CurrentClass {classData.className}");
    }

    [Server]
    public float GetPlayerCameraXAxis() => movementScript.CameraXAxis;


    public PlayerScoreboardInfo GetPlayerScoreboardInfo(int requesterTeam, bool ignoreTeamFilter = false)
    {
        if (ignoreTeamFilter || requesterTeam == playerTeam)
        {
            return new PlayerScoreboardInfo(playerTeam, currentClassIndex, playerName, score, revives, deaths, IsDead, isLocalPlayer);
        }

        return new PlayerScoreboardInfo(playerTeam, playerName, score);
    }

    public void UpdatePlayerScore(int ammount) => score += ammount;

    #endregion

    #region ServerCommands

    [Command]
    void CmdRequestPlayerInfo()
    {
        PlayerScoreboardInfo[] info = GameModeManager.INS.GetScoreboardInfo(playerTeam);
        RpcShowScoreboard(connectionToClient, info);
    }

    [Command]
    void CmdRequestPlayerChangeTeam(int team)
    {
        movementScript.freezePlayer = true;
        if (!isDead && !firstSpawn) GameModeManager.INS.KillPlayer(this);
        classData = null;
        GameModeManager.INS.TeamManagerInstance.PlayerSelectedTeam(this, team);
    }

    [Command]
    void CmdRequestPlayerChangeClass(int teamClass)
    {
        GameModeManager.INS.PlayerChangeClass(this, teamClass);
    }

    [Command]
    void CmdRequestPlayerRespawn()
    {
        if ((!isDead && !firstSpawn) || NetworkTime.time < timeToRespawn || classData == null) return;
        GameModeManager.INS.RespawnPlayer(this);
        firstSpawn = false;
    }

    [Command]
    void CmdRequestWoundedGiveUp()
    {
        if (isDead || !isWounded) return;
        woundedTime = 0;
        //isWounded = false;
        //RpcCharacterDied();
        //RpcWoundedCanGiveUp(connectionToClient);
    }

    #endregion

    #region TargetRpc

    [TargetRpc]
    public void RpcShowScoreboard(NetworkConnection target, PlayerScoreboardInfo[] scoreboardInfo)
    {
        if (target.connectionId != connectionToServer.connectionId || !PlayerCanvasScript.IsScoreboardMenuOpen) return;
        PlayerCanvasScript.InitScoreboard(scoreboardInfo, playerTeam);
    }

    [TargetRpc]
    void RpcWoundedCanGiveUp(NetworkConnection target)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvasScript.PlayerNotWounded();
        PlayerCanvasScript.ToggleClassSelection(true);
    }

    [TargetRpc]
    public void RpcShowGreetings(NetworkConnection target, int team1Tickets, int team2Tickets)
    {
        if (target.connectionId != connectionToServer.connectionId) return;

        PlayerCanvasScript.ToggleTeamSelection(true);
        PlayerCanvasScript.SetTeamTickets(0, team1Tickets);
        PlayerCanvasScript.SetTeamTickets(1, team2Tickets);
    }

    [TargetRpc]
    public void RpcTeamSelectionError(NetworkConnection target, int error)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        Debug.LogError($"Client: TeamSelection error code: {error}");
    }

    [TargetRpc]
    public void RpcTeamSelectionSuccess(NetworkConnection target, int team)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvasScript.ToggleTeamSelection(false);

        if (team > 0)
        {
            PlayerCanvasScript.OnTeamSelection(team);
            PlayerCanvasScript.ToggleClassSelection(true);
        }
    }

    [TargetRpc]
    public void RpcClassSelectionError(NetworkConnection target, int error)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        Debug.LogError($"Client: ClassSelection error code: {error}");
        PlayerCanvasScript.ToggleSpawnButton(true);
    }

    [TargetRpc]
    public void RpcClassSelectionSuccess(NetworkConnection target, int classIndex)
    { 
        if (target.connectionId != connectionToServer.connectionId) return;
        if (isDead || firstSpawn) PlayerCanvasScript.ToggleSpawnButton(true);
        PlayerCanvasScript.OnClassSelection(classIndex);
        print($"Client: CurrentClass {classIndex}");
    }

    [TargetRpc]
    public void RpcOnControlPoint(NetworkConnection target, int currentCPController, float cpCaptureProgress, int defyingTeam)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvasScript.OnControlPoint(currentCPController, defyingTeam, cpCaptureProgress);
        onControlPoint = true;
    }

    [TargetRpc]
    public void RpcExitControlPoint(NetworkConnection target)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvasScript.OnExitControlPoint();
        onControlPoint = false;
    }

    [TargetRpc]
    public void RpcUpdateCPProgress(NetworkConnection target, float progress)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvasScript.UpdateCPProgress(progress);
    }

    [TargetRpc]
    public void RpcPlayerOutOfBounds(NetworkConnection target, float timeToReturn)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvasScript.PlayerOutOfBounds(timeToReturn);
    }

    [TargetRpc]
    public void RpcPlayerReturned(NetworkConnection target)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvasScript.PlayerInBounds();
    }

    [TargetRpc]
    void RpcShowWoundedHUD(NetworkConnection target, double _woundedTime, double _respawnTime)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        movementScript.FreezeInputs = true;
        inventory.DisablePlayerInputs = true;
        PlayerCanvasScript.PlayerIsWounded(_woundedTime);
        PlayerCanvasScript.ShowRespawnTimer(_respawnTime);
    }

    [TargetRpc]
    public void RpcPlayerConnected(NetworkConnection target, PlayerScoreboardInfo playerInfo)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        Debug.Log($"Client: Player {playerInfo.playerName} connected");
        //if (!PlayerCanvasScript.IsScoreboardMenuOpen) return;
        //PlayerCanvasScript.AddPlayerToScoreboard(playerInfo);
    }

    [TargetRpc]
    public void RpcPlayerSelectedTeam(NetworkConnection target, PlayerScoreboardInfo playerInfo)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        Debug.Log($"Client: Player {playerInfo.playerName} changes his team to {playerInfo.playerTeam}");
        if (!PlayerCanvasScript.IsScoreboardMenuOpen) return;
        PlayerCanvasScript.AddPlayerToScoreboard(playerInfo);
    }

    [TargetRpc]
    public void RpcPlayerDisconnected(NetworkConnection target, string playerName)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        Debug.Log($"Client: Player {playerName} disconnected");
        PlayerCanvasScript.RemovePlayerFromScoreboard(playerName);
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    protected override void RpcCharacterDied()
    {
        if (isLocalPlayer)
        {
            movementScript.FreezeInputs = true;
            inventory.DisablePlayerInputs = true;
        }

        playerMesh.enabled = false;
    }

    [ClientRpc]
    public void RpcControlPointCaptured(int cpTeam, int oldTeam, string cpName)
    {
        if (PlayerCanvasScript == null) return;
        if (onControlPoint) PlayerCanvasScript.OnPointCaptured(cpTeam, oldTeam != 0 ? oldTeam : 1);
        PlayerCanvasScript.NewCapturedControlPoint(cpTeam, cpName);
    }

    [ClientRpc]
    public void RpcPlayerSpawns()
    {
        if (isLocalPlayer)
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
    public void RpcMatchEndPlayerSetup(int loosingTeam, float timeToChangeLevel)
    {
        if (isLocalPlayer)
        {
            PlayerCanvasScript.ShowGameOverScreen(loosingTeam, timeToChangeLevel);
        }

        playerMesh.enabled = false;
    }

    #endregion
}
