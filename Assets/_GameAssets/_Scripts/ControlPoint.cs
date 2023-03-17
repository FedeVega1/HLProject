using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(BoxCollider))]
public class ControlPoint : NetworkBehaviour
{
    [SerializeField] protected bool debugShowBounds;

    [Range(0, TeamManager.MAXTEAMS)]
    [SerializeField] protected int startTeam;

    [SerializeField] protected float captureStep = .03f, bleedSpeed = .025f, notifyInterval = .05f;
    [SerializeField] protected bool uncapturablePoint;

    public bool IsBlocked { get; private set; }
    public int PointOrder { get; private set; }

    protected bool canCapture, onDraw, canDeplete;
    protected int currentTeam, defyingTeam, newTeamOnDeplete;
    protected float captureProgress;
    protected List<Player> playersInCP;

    public System.Action<int, int> OnControllPointCaptured;

    protected virtual void Start()
    {
        playersInCP = new List<Player>();
        IsBlocked = uncapturablePoint;
        enabled = !IsBlocked;
        currentTeam = Mathf.Clamp(startTeam, 0, TeamManager.MAXTEAMS);
        newTeamOnDeplete = -1;
    }

    protected virtual void Update()
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

    protected virtual void OnTriggerEnter(Collider other)
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

    protected virtual void OnTriggerStay(Collider other)
    {
        if (IsBlocked || !canCapture || !isServer || !other.CompareTag("Player")) return;
        CaptureControlPoint();
    }

    protected virtual void OnTriggerExit(Collider other)
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

    protected virtual void CaptureControlPoint()
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

    protected virtual void CheckPlayerCount()
    {
        canCapture = GameModeManager.INS.CanCaptureControlPoint_Server(ref playersInCP, out int defyingTeam, currentTeam, PointOrder);
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

    protected virtual void NotifyProgress()
    {
        float progress = captureProgress % notifyInterval;
        if (progress >= -.01f && progress <= .01f)
        {
            int size = playersInCP.Count;
            for (int i = 0; i < size; i++)
                playersInCP[i].RpcUpdateCPProgress(playersInCP[i].connectionToClient, captureProgress);
        }
    }

    protected virtual void NotifyPointCapture()
    {
        GameModeManager.INS.DoActionPerPlayer_Server((player) => 
        {
            player.UpdatePlayerScore_Server(50);
            player.ControlPointCaptured_ClientRpc(defyingTeam, currentTeam, name);
        });

        OnControllPointCaptured?.Invoke(defyingTeam, currentTeam);
    }

    protected virtual void OnProgressDepleted()
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

    protected virtual int GetPlayersInTeam(int team)
    {
        if (team != 0) 
            return GameModeManager.INS.TeamManagerInstance.SeparatePlayersPerTeam_Server(ref playersInCP)[team - 1];
        return 1;
    }

    protected virtual void OnDrawGizmos()
    {
        if (!debugShowBounds) return;
        Gizmos.color = startTeam == 0 ? Color.grey : TeamManager.FactionColors[startTeam - 1];
        Gizmos.DrawWireCube(transform.position, transform.localScale);    
    }
}
