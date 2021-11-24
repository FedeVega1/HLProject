using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public struct PlayerInfo : NetworkMessage
{
    public string playerName;
}

public class Player : Character
{
    [SerializeField] GameObject playerCamera, playerCanvasPrefab;
    [SerializeField] MeshRenderer playerMesh;
    [SerializeField] PlayerMovement movementScript;

    [SyncVar(hook = nameof(OnTeamChange))] int playerTeam;
    [SyncVar] string playerName;

    bool onControlPoint;
    PlayerCanvas playerCanvas;

    void OnTeamChange(int oldTeam, int newTeam)
    {
        print($"{playerName} Team is {newTeam}");
    }

    public override void OnStartLocalPlayer()
    {
        playerCanvas = Instantiate(playerCanvasPrefab).GetComponent<PlayerCanvas>();
        playerCanvas.Init(this);
        movementScript.FreezePlayer = true;
    }

    void Start()
    {
        if (!isLocalPlayer)
        {
            Destroy(playerCamera);
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (Input.GetKeyDown(KeyCode.Escape)) GameManager.INS.DisconnectFromServer();
        if (Input.GetKeyDown(KeyCode.Escape) && Input.GetKeyDown(KeyCode.LeftShift)) GameManager.INS.StopServer();
    }

    [ClientRpc]
    protected override void RpcCharacterDied()
    {
        playerMesh.enabled = false;
    }

    [Client]
    public void TrySelectTeam(int team)
    {
        if (!isLocalPlayer) return;
        CmdRequestPlayerChangeTeam(team);
    }

    [Command]
    void CmdRequestPlayerChangeTeam(int team) => GameModeManager.INS.TeamManagerInstance.PlayerSelectedTeam(this, team);

    public string SetPlayerName(string newName) => playerName = newName;
    public string GetPlayerName() => playerName;

    public int GetPlayerTeam() => playerTeam;
    public void SetPlayerTeam(int newPlayerTeam) => playerTeam = newPlayerTeam;

    [Server]
    public void SetAsSpectator()
    {

    }

    [Server]
    public void SpawnPlayer(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        movementScript.ForceMoveCharacter(spawnPosition, spawnRotation);
        //print($"NewPos: {MyTransform.position}");
        movementScript.RpcToggleFreezePlayer(connectionToClient, false);
    }

    [TargetRpc]
    public void RpcShowGreetings(NetworkConnection target)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        playerCanvas.ToggleTeamSelection(true);
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

    [ClientRpc]
    public void RpcControlPointCaptured(int cpTeam, int oldTeam, string cpName)
    {
        if (playerCanvas == null) return;
        if (onControlPoint) playerCanvas.OnPointCaptured(cpTeam, oldTeam != 0 ? oldTeam : 1);
        playerCanvas.NewCapturedControlPoint(cpTeam, cpName);
    }
}
