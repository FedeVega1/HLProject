using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Player : Character
{
    [SerializeField] GameObject playerCamera, playerCanvasPrefab;
    [SerializeField] MeshRenderer playerMesh;
    [SerializeField] PlayerMovement movementScript;

    [SyncVar(hook = nameof(OnTeamChange))] int playerTeam;
    [SyncVar] string playerName;
    [SyncVar] double timeToRespawn;

    bool classSelectionMenuOpen, teamSelectionMenuOpen;
    bool onControlPoint, firstSpawn;
    float maxRespawnTime;
    PlayerCanvas playerCanvas;
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
        GameModeManager.INS.TeamManagerInstance.OnTicketChange += UpdateMatchTickets;
    }

    void Start() 
    { 
        if (!isLocalPlayer) Destroy(playerCamera);
        playerMesh.enabled = false;
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (Input.GetKeyDown(KeyCode.Escape)) GameManager.INS.DisconnectFromServer();
        if (Input.GetKeyDown(KeyCode.Escape) && Input.GetKeyDown(KeyCode.LeftShift)) GameManager.INS.StopServer();
        if (Input.GetKeyDown(KeyCode.Delete)) GameModeManager.INS.KillPlayer(this);
        if (Input.GetKeyDown(KeyCode.F1)) GameModeManager.INS.EndMatch();

        if (!firstSpawn && Input.GetKeyDown(KeyCode.Return))
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
                playerCanvas.ToggleClassSelection(true);
                classSelectionMenuOpen = true;
            }
        }

        if (!firstSpawn && Input.GetKeyDown(KeyCode.M))
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
        }
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

    public float GetPlayerRespawnTime() => maxRespawnTime;
    public void SetRespawnTime(float newRespawnTime) => maxRespawnTime = newRespawnTime;

    #endregion

    #region ClientOnly

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

    #endregion

    #region ServerOnly

    [Server]
    public void SetAsSpectator()
    {

    }

    [Server]
    public void SpawnPlayer(Vector3 spawnPosition, Quaternion spawnRotation, float spawnTime)
    {
        //print($"NewPos: {MyTransform.position}");
        isDead = false;
        maxRespawnTime = spawnTime;
        movementScript.ForceMoveCharacter(spawnPosition, spawnRotation);
        movementScript.freezePlayer = false;
        //movementScript.RpcToggleFreezePlayer(connectionToClient, false);
        RpcPlayerSpawns();
    }

    [Server]
    protected override void CharacterDies()
    {
        timeToRespawn = NetworkTime.time + maxRespawnTime;
        RpcShowDeadHUD(connectionToClient, timeToRespawn);
        //movementScript.RpcToggleFreezePlayer(connectionToClient, true);
        movementScript.freezePlayer = true;
        base.CharacterDies();
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

        RpcMatchEndPlayerSetup(loosingTeam);
    }

    [Server]
    public void SetPlayerClass(TeamClassData classData)
    {
        this.classData = classData;
    }

    #endregion

    #region ServerCommands

    [Command]
    void CmdRequestPlayerChangeTeam(int team)
    {
        movementScript.freezePlayer = true;
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

    #endregion

    #region TargetRpc

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
    public void RpcTeamSelectionSuccess(NetworkConnection target)
    {
        if (target.connectionId != connectionToServer.connectionId) return;

        if (!isDead && !firstSpawn) GameModeManager.INS.KillPlayer(this);

        playerCanvas.ToggleTeamSelection(false);
        playerCanvas.OnTeamSelection(playerTeam);
        playerCanvas.ToggleClassSelection(true);
    }

    [TargetRpc]
    public void RpcClassSelectionError(NetworkConnection target, int error)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        Debug.LogError($"ClassSelection error code: {error}");
        playerCanvas.ToggleSpawnButton(false);
    }

    [TargetRpc]
    public void RpcClassSelectionSuccess(NetworkConnection target, int classIndex)
    { 
        if (target.connectionId != connectionToServer.connectionId) return;
        playerCanvas.ToggleSpawnButton(true);
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
    void RpcShowDeadHUD(NetworkConnection target, double respawnTime)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        movementScript.FreezeInputs = true;
        playerCanvas.PlayerDied(respawnTime);
    }

    #endregion

    #region ClientRpc

    [ClientRpc]
    protected override void RpcCharacterDied()
    {
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
        }

        playerMesh.enabled = true;
    }

    [ClientRpc]
    public void RpcMatchEndPlayerSetup(int loosingTeam)
    {
        if (isLocalPlayer)
        {
            playerCanvas.ShowGameOverScreen(loosingTeam);
        }

        playerMesh.enabled = false;
    }

    #endregion
}
