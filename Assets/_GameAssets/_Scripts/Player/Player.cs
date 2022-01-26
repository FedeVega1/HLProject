using System.Collections;
using System.Collections.Generic;
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

    bool classSelectionMenuOpen, teamSelectionMenuOpen;
    bool onControlPoint;
    double woundedTime;
    PlayerCanvas playerCanvas;
    PlayerInventory inventory;
    TeamClassData classData;

    #region Hooks

    void OnTeamChange(int oldTeam, int newTeam) => print($"{playerName} Team is {newTeam}");

    #endregion

    public override void OnStartServer()
    {
        movementScript.freezePlayer = true;
        firstSpawn = true;
    }

    public override void OnStartLocalPlayer()
    {
        playerCanvas = Instantiate(playerCanvasPrefab).GetComponent<PlayerCanvas>();
        playerCanvas.Init(this);
        movementScript.FreezeInputs = true;
        GameModeManager.INS.TeamManagerInstance.OnTicketChange += UpdateMatchTickets;
    }

    void Start()
    {
        if (!isLocalPlayer) Destroy(playerCamera);
        if (!IsDead || firstSpawn) playerMesh.enabled = false;

        inventory = GetComponent<PlayerInventory>();
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

    #region ClientOnly

    [Client]
    void CheckInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) GameManager.INS.DisconnectFromServer();
        if (Input.GetKeyDown(KeyCode.Escape) && Input.GetKeyDown(KeyCode.LeftShift)) GameManager.INS.StopServer();
        if (Input.GetKeyDown(KeyCode.Delete)) GameModeManager.INS.KillPlayer(this);
        if (Input.GetKeyDown(KeyCode.F1)) GameModeManager.INS.EndMatch();

        if (!isDead && !firstSpawn && Input.GetKeyDown(KeyCode.Return))
        {
            if (classSelectionMenuOpen)
            {
                movementScript.FreezeInputs = false;
                playerCanvas.ToggleClassSelection(false);
                classSelectionMenuOpen = false;
            }
            else
            {
                movementScript.FreezeInputs = true;
                playerCanvas.ToggleTeamSelection(false);
                playerCanvas.ToggleClassSelection(true);
                classSelectionMenuOpen = true;
            }

            return;
        }

        if (!isDead && !firstSpawn && Input.GetKeyDown(KeyCode.M))
        {
            if (teamSelectionMenuOpen)
            {
                movementScript.FreezeInputs = false;
                playerCanvas.ToggleTeamSelection(false);
                teamSelectionMenuOpen = false;
            }
            else
            {
                movementScript.FreezeInputs = true;
                playerCanvas.ToggleClassSelection(false);
                playerCanvas.ToggleTeamSelection(true);
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
        if (isLocalPlayer) playerCanvas.SetTeamTickets(team - 1, tickets);
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

    #region ServerOnly

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
        isDead = false;
        MaxRespawnTime = spawnTime;
        movementScript.ForceMoveCharacter(spawnPosition, spawnRotation);
        movementScript.freezePlayer = false;
        //movementScript.RpcToggleFreezePlayer(connectionToClient, false);
        inventory.SetupWeaponInventory(classData.classWeapons, 0);
        RpcPlayerSpawns();
    }

    [Server]
    protected override void CharacterDies()
    {
        if (isDead || isInvencible) return;

        timeToRespawn = NetworkTime.time + MaxRespawnTime;

        isWounded = true;
        woundedTime = NetworkTime.time + (woundedMaxTime - BonusWoundTime);

        RpcShowWoundedHUD(connectionToClient, woundedTime, timeToRespawn);
        movementScript.freezePlayer = true;
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
        if ((!isDead && !firstSpawn) || NetworkTime.time < timeToRespawn) return;
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
        playerCanvas.PlayerNotWounded();
        playerCanvas.ToggleClassSelection(true);
    }

    [TargetRpc]
    public void RpcShowGreetings(NetworkConnection target, int team1Tickets, int team2Tickets)
    {
        if (target.connectionId != connectionToServer.connectionId) return;

        playerCanvas.ToggleTeamSelection(true);
        playerCanvas.SetTeamTickets(0, team1Tickets);
        playerCanvas.SetTeamTickets(1, team2Tickets);
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
        playerCanvas.ToggleTeamSelection(false);
        playerCanvas.OnTeamSelection(team);
        playerCanvas.ToggleClassSelection(true);
    }

    [TargetRpc]
    public void RpcClassSelectionError(NetworkConnection target, int error)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        Debug.LogError($"ClassSelection error code: {error}");
        playerCanvas.ToggleSpawnButton(true);
    }

    [TargetRpc]
    public void RpcClassSelectionSuccess(NetworkConnection target, int classIndex)
    { 
        if (target.connectionId != connectionToServer.connectionId) return;
        if (isDead || firstSpawn) playerCanvas.ToggleSpawnButton(true);
        playerCanvas.OnClassSelection(classIndex);
    }

    [TargetRpc]
    public void RpcOnControlPoint(NetworkConnection target, int currentCPController, float cpCaptureProgress, int defyingTeam)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        playerCanvas.OnControlPoint(currentCPController, defyingTeam, cpCaptureProgress);
        onControlPoint = true;
    }

    [TargetRpc]
    public void RpcExitControlPoint(NetworkConnection target)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        playerCanvas.OnExitControlPoint();
        onControlPoint = false;
    }

    [TargetRpc]
    public void RpcUpdateCPProgress(NetworkConnection target, float progress)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        playerCanvas.UpdateCPProgress(progress);
    }

    [TargetRpc]
    public void RpcPlayerOutOfBounds(NetworkConnection target, float timeToReturn)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        playerCanvas.PlayerOutOfBounds(timeToReturn);
    }

    [TargetRpc]
    public void RpcPlayerReturned(NetworkConnection target)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        playerCanvas.PlayerInBounds();
    }

    [TargetRpc]
    void RpcShowWoundedHUD(NetworkConnection target, double _woundedTime, double _respawnTime)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        movementScript.FreezeInputs = true;
        playerCanvas.PlayerIsWounded(_woundedTime);
        playerCanvas.ShowRespawnTimer(_respawnTime);
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    protected override void RpcCharacterDied()
    {
        if (isLocalPlayer) movementScript.FreezeInputs = true;
        playerMesh.enabled = false;
    }

    [ClientRpc]
    public void RpcControlPointCaptured(int cpTeam, int oldTeam, string cpName)
    {
        if (playerCanvas == null) return;
        if (onControlPoint) playerCanvas.OnPointCaptured(cpTeam, oldTeam != 0 ? oldTeam : 1);
        playerCanvas.NewCapturedControlPoint(cpTeam, cpName);
    }

    [ClientRpc]
    public void RpcPlayerSpawns()
    {
        if (isLocalPlayer)
        {
            playerCanvas.PlayerRespawn();
            movementScript.FreezeInputs = false;
            //inventory.SetupWeaponInventory(classData.classWeapons, 0);
        }

        playerMesh.enabled = true;
    }

    [ClientRpc]
    public void RpcMatchEndPlayerSetup(int loosingTeam, float timeToChangeLevel)
    {
        if (isLocalPlayer)
        {
            playerCanvas.ShowGameOverScreen(loosingTeam, timeToChangeLevel);
        }

        playerMesh.enabled = false;
    }

    #endregion
}
