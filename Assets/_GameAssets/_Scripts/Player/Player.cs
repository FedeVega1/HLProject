using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using Mirror;

public class Player : Character
{
    [Header("Player")]

    [SerializeField] GameObject playerCamera, playerCanvasPrefab;
    [SerializeField] MeshRenderer playerMesh;
    [SerializeField] PlayerMovement movementScript;
    [SerializeField] float woundedMaxTime;

    [SyncVar(hook = nameof(OnTeamChange))] int playerTeam;
    [SyncVar] bool isWounded, firstSpawn;
    [SyncVar] string playerName;
    [SyncVar] double timeToRespawn;

    public float MaxRespawnTime { get; set; }
    public float BonusWoundTime { get; set; }
    public PlayerCanvas PlayerCanvas { get; private set; }

    bool classSelectionMenuOpen, teamSelectionMenuOpen;
    bool onControlPoint;
    double woundedTime;
    PlayerInventory inventory;
    TeamClassData classData;

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
        PlayerCanvas = Instantiate(playerCanvasPrefab).GetComponent<PlayerCanvas>();
        PlayerCanvas.Init(this);
        movementScript.FreezeInputs = true;
        GameModeManager.INS.TeamManagerInstance.OnTicketChange += UpdateMatchTickets;
    }

    //public override void OnStartClient()
    //{
    //    if (isLocalPlayer || IsDead || firstSpawn) return;
    //    playerMesh.enabled = true;
    //}

    void Start()
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

    #endregion

    #region Client

    [Client]
    void CheckInputs()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Escape)) GameManager.INS.StopServer();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isServer && isClient) GameManager.INS.StopServer();
            else GameManager.INS.DisconnectFromServer();
        }

        if (Input.GetKeyDown(KeyCode.Delete)) GameModeManager.INS.KillPlayer(this);
        if (Input.GetKeyDown(KeyCode.F1)) GameModeManager.INS.EndMatch();

        if (!isDead && !firstSpawn && Input.GetKeyDown(KeyCode.Return))
        {
            if (classSelectionMenuOpen)
            {
                movementScript.FreezeInputs = false;
                inventory.DisablePlayerInputs = false;
                PlayerCanvas.ToggleClassSelection(false);
                classSelectionMenuOpen = false;
            }
            else
            {
                movementScript.FreezeInputs = true;
            inventory.DisablePlayerInputs = true;
                PlayerCanvas.ToggleTeamSelection(false);
                PlayerCanvas.ToggleClassSelection(true);
                classSelectionMenuOpen = true;
            }

            return;
        }

        if (!isDead && !firstSpawn && Input.GetKeyDown(KeyCode.M))
        {
            if (teamSelectionMenuOpen)
            {
                movementScript.FreezeInputs = false;
                inventory.DisablePlayerInputs = false;
                PlayerCanvas.ToggleTeamSelection(false);
                teamSelectionMenuOpen = false;
            }
            else
            {
                movementScript.FreezeInputs = true;
                inventory.DisablePlayerInputs = true;
                PlayerCanvas.ToggleClassSelection(false);
                PlayerCanvas.ToggleTeamSelection(true);
                teamSelectionMenuOpen = true;
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

    [Client]
    public void UpdateMatchTickets(int team, int tickets)
    {
        if (isLocalPlayer) PlayerCanvas.SetTeamTickets(team - 1, tickets);
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
    public void SetAsSpectator() { }

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
    public void SetPlayerClass(TeamClassData classData)
    {
        this.classData = classData;
        print($"Server: CurrentClass {classData.className}");
    }

    [Server]
    public float GetPlayerCameraXAxis() => movementScript.CameraXAxis;

    #endregion

    #region ServerCommands

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
    void RpcWoundedCanGiveUp(NetworkConnection target)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvas.PlayerNotWounded();
        PlayerCanvas.ToggleClassSelection(true);
    }

    [TargetRpc]
    public void RpcShowGreetings(NetworkConnection target, int team1Tickets, int team2Tickets)
    {
        if (target.connectionId != connectionToServer.connectionId) return;

        PlayerCanvas.ToggleTeamSelection(true);
        PlayerCanvas.SetTeamTickets(0, team1Tickets);
        PlayerCanvas.SetTeamTickets(1, team2Tickets);
    }

    [TargetRpc]
    public void RpcTeamSelectionError(NetworkConnection target, int error)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        Debug.LogError($"TeamSelection error code: {error}");
    }

    [TargetRpc]
    public void RpcTeamSelectionSuccess(NetworkConnection target, int team)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvas.ToggleTeamSelection(false);
        PlayerCanvas.OnTeamSelection(team);
        PlayerCanvas.ToggleClassSelection(true);
    }

    [TargetRpc]
    public void RpcClassSelectionError(NetworkConnection target, int error)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        Debug.LogError($"ClassSelection error code: {error}");
        PlayerCanvas.ToggleSpawnButton(true);
    }

    [TargetRpc]
    public void RpcClassSelectionSuccess(NetworkConnection target, int classIndex)
    { 
        if (target.connectionId != connectionToServer.connectionId) return;
        if (isDead || firstSpawn) PlayerCanvas.ToggleSpawnButton(true);
        PlayerCanvas.OnClassSelection(classIndex);
        print($"Client: CurrentClass {classIndex}");
    }

    [TargetRpc]
    public void RpcOnControlPoint(NetworkConnection target, int currentCPController, float cpCaptureProgress, int defyingTeam)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvas.OnControlPoint(currentCPController, defyingTeam, cpCaptureProgress);
        onControlPoint = true;
    }

    [TargetRpc]
    public void RpcExitControlPoint(NetworkConnection target)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvas.OnExitControlPoint();
        onControlPoint = false;
    }

    [TargetRpc]
    public void RpcUpdateCPProgress(NetworkConnection target, float progress)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvas.UpdateCPProgress(progress);
    }

    [TargetRpc]
    public void RpcPlayerOutOfBounds(NetworkConnection target, float timeToReturn)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvas.PlayerOutOfBounds(timeToReturn);
    }

    [TargetRpc]
    public void RpcPlayerReturned(NetworkConnection target)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        PlayerCanvas.PlayerInBounds();
    }

    [TargetRpc]
    void RpcShowWoundedHUD(NetworkConnection target, double _woundedTime, double _respawnTime)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        movementScript.FreezeInputs = true;
        inventory.DisablePlayerInputs = true;
        PlayerCanvas.PlayerIsWounded(_woundedTime);
        PlayerCanvas.ShowRespawnTimer(_respawnTime);
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
        if (PlayerCanvas == null) return;
        if (onControlPoint) PlayerCanvas.OnPointCaptured(cpTeam, oldTeam != 0 ? oldTeam : 1);
        PlayerCanvas.NewCapturedControlPoint(cpTeam, cpName);
    }

    [ClientRpc]
    public void RpcPlayerSpawns()
    {
        if (isLocalPlayer)
        {
            PlayerCanvas.PlayerRespawn();
            PlayerCanvas.ToggleWeaponInfo(true);
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
            PlayerCanvas.ShowGameOverScreen(loosingTeam, timeToChangeLevel);
        }

        playerMesh.enabled = false;
    }

    #endregion
}
