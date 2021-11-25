using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(BoxCollider))]
public class ControlPoint : NetworkBehaviour
{
    [Range(0, TeamManager.MAXTEAMS)]
    [SerializeField] int startTeam;

    [SerializeField] float captureStep = .03f, bleedSpeed = .025f, notifyInterval = .05f;
    [SerializeField] bool uncapturablePoint;

    List<Player> playersInCP;

    public bool IsBlocked { get; private set; }
    public int PointOrder { get; private set; }

    bool canCapture, onDraw, canDeplete;
    int currentTeam, defyingTeam, newTeamOnDeplete;
    float captureProgress;

    public System.Action<int, int> OnControllPointCaptured;

    void Start()
    {
        playersInCP = new List<Player>();
        IsBlocked = uncapturablePoint;
        enabled = !IsBlocked;
        currentTeam = Mathf.Clamp(startTeam, 0, TeamManager.MAXTEAMS);
        newTeamOnDeplete = -1;
    }

    void Update()
    {
        if (IsBlocked) return;
        if (captureProgress > 0 && (!canCapture || canDeplete))
        {
            captureProgress -= bleedSpeed * GetPlayersInTeam(currentTeam) * Time.deltaTime;

            if (captureProgress < 0)
            {
                captureProgress = 0;
                if (canDeplete) OnProgressDepleted();
            }

            NotifyProgress();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsBlocked || !isServer || !other.CompareTag("Player")) return;    
        Player playerScript = other.GetComponent<Player>();

        if (playerScript != null)
        {
            if (playersInCP.Contains(playerScript))
            {
                Debug.LogError($"{playerScript.GetPlayerName()} is on {name} control point bounds, but he was already inside!");
                return;
            }

            playersInCP.Add(playerScript);
            CheckPlayerCount();

            playerScript.RpcOnControlPoint(playerScript.connectionToClient, currentTeam, captureProgress, defyingTeam);
        }
        else
        {
            Debug.LogError($"{name} control point detected a player object without a Player component!");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (IsBlocked || !canCapture || !isServer || !other.CompareTag("Player")) return;
        CaptureControlPoint();
    }

    void OnTriggerExit(Collider other)
    {
        if (IsBlocked || !isServer || !other.CompareTag("Player")) return;

        int size = playersInCP.Count;
        for (int i = 0; i < size; i++)
        {
            if (playersInCP[i].gameObject == other.gameObject)
            {
                playersInCP[i].RpcExitControlPoint(playersInCP[i].connectionToClient);
                playersInCP.RemoveAt(i);
                break;
            }
        }

        CheckPlayerCount();
    }

    void CaptureControlPoint()
    {
        if (onDraw || defyingTeam == 0) return;

        captureProgress += captureStep * GetPlayersInTeam(defyingTeam) * Time.deltaTime;
        NotifyProgress();

        if (captureProgress >= 1)
        {
            NotifyPointCapture();

            captureProgress = 0;
            currentTeam = defyingTeam;
            defyingTeam = 0;
            canCapture = false;
        }
    }

    void CheckPlayerCount()
    {
        canCapture = GameModeManager.INS.CanCaptureControlPoint(ref playersInCP, out int defyingTeam, currentTeam, PointOrder);
        if (defyingTeam != 0)
        {
            if (captureProgress > 0 && this.defyingTeam != defyingTeam)
            {
                newTeamOnDeplete = defyingTeam;
                canDeplete = true;
                return;
            }
            else
            {
                canDeplete = false;
                newTeamOnDeplete = -1;
            }

            this.defyingTeam = defyingTeam;
            onDraw = false;
        }
        else
        {
            onDraw = true;
        }
    }

    void NotifyProgress()
    {
        float progress = captureProgress % notifyInterval;
        if (progress >= -.01f && progress <= .01f)
        {
            int size = playersInCP.Count;
            for (int i = 0; i < size; i++)
                playersInCP[i].RpcUpdateCPProgress(playersInCP[i].connectionToClient, captureProgress);
        }
    }

    void NotifyPointCapture()
    {
        GameModeManager.INS.DoActionPerPlayer((player) => { player.RpcControlPointCaptured(defyingTeam, currentTeam, name); });
        OnControllPointCaptured?.Invoke(defyingTeam, currentTeam);
    }

    void OnProgressDepleted()
    {
        canDeplete = false;
        defyingTeam = newTeamOnDeplete;
        newTeamOnDeplete = -1;
        CheckPlayerCount();

        int size = playersInCP.Count;
        for (int i = 0; i < size; i++)
            playersInCP[i].RpcOnControlPoint(playersInCP[i].connectionToClient, currentTeam, captureProgress, defyingTeam);
    }

    public void SetPointOrder(int newOrder)
    {
        PointOrder = newOrder;
        //BlockCP();
    }

    public void BlockCP()
    {
        IsBlocked = true;
        canCapture = false;
        enabled = false;
        defyingTeam = -1;
        playersInCP.Clear();
    }

    public void UnlockCP()
    {
        if (uncapturablePoint) return;
        IsBlocked = false;
        enabled = true;
    }

    int GetPlayersInTeam(int team)
    {
        if (team != 0) 
            return GameModeManager.INS.TeamManagerInstance.SeparatePlayersPerTeam(ref playersInCP)[team - 1];
        return 1;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = startTeam == 0 ? Color.grey : TeamManager.FactionColors[startTeam];
        Gizmos.DrawWireCube(transform.position, transform.localScale);    
    }
}
