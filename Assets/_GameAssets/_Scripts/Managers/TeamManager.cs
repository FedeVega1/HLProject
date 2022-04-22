using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TeamManager : NetworkBehaviour
{
    [System.Serializable]
    class Team
    {
        public readonly int teamIndex;
        public readonly List<Player> playersInTeam;

        public bool CapturedFirstPoint { get; private set; }
        public int Tickets { get; private set; }

        public int nextPointToCapture, controlledPoints;

        bool canBleed;
        float bleedSpeed, bleedProgress;

        public System.Action<int> UpdateTickets;
        public System.Action OnLostAllTickets;

        public Team(int index)
        {
            teamIndex = index;
            playersInTeam = new List<Player>();
        }

        public void SetTickets(int startingTickets)
        {
            Tickets = startingTickets;
            UpdateTickets?.Invoke(Tickets);
        }

        public void Update()
        {
            if (!canBleed) return;
            bleedProgress += bleedSpeed * Time.deltaTime;
            if (bleedProgress >= 1)
            {
                RemoveTicket(1);
                bleedProgress = 0;
            }
        }

        public void RemoveTicket(int quanity)
        {
            Tickets -= quanity;
            if (Tickets <= 0)
            {
                Tickets = 0;
                OnLostAllTickets?.Invoke();
            }

            UpdateTickets?.Invoke(Tickets);
        }

        public void StartBleeding(float bleedSpeed)
        {
            canBleed = true;
            this.bleedSpeed = bleedSpeed;
        }

        public void StopBleeding()
        {
            canBleed = false;
            bleedProgress = 0;
        }

        public bool HasMorePoints(int points) => controlledPoints > points;

        public void TeamCapturedFirstPoint() => CapturedFirstPoint = true;
    }

    public const int MAXTEAMS = 2;
    public static readonly string[] FactionNames = new string[MAXTEAMS] { "Combine", "Rebels" };
    public static readonly Color32[] FactionColors = new Color32[MAXTEAMS]
    {
        new Color32(0xE9, 0xD1, 0x1E, 0xFF),
        new Color32(0xCD, 0x71, 0x33, 0xFF)
    };

    int totalCP;
    Team[] teamData;
    GameModeData currentGameModeData;
    List<Player> spectators;

    public System.Action<int, int> OnTicketChange;

    public override void OnStartServer()
    {
        spectators = new List<Player>();
        //playersByTeam = new List<Player>[MAXTEAMS];

        teamData = new Team[MAXTEAMS];
        for (int i = 0; i < MAXTEAMS; i++)
        {
            teamData[i] = new Team(i + 1);
            teamData[i].SetTickets(currentGameModeData.ticketsPerTeam[i]);
            teamData[i].OnLostAllTickets += GameModeManager.INS.EndMatch;
        }

        teamData[0].nextPointToCapture = 0;
        teamData[1].nextPointToCapture = totalCP - 1;

        teamData[0].UpdateTickets += (tickets) => { OnTicketChange?.Invoke(1, tickets); };
        teamData[1].UpdateTickets += (tickets) => { OnTicketChange?.Invoke(2, tickets); };

        GameModeManager.INS.OnMatchEnded += MatchEnded;
        //for (int i = 0; i < MAXTEAMS; i++) playersByTeam[i] = new List<Player>();
    }

    void Update()
    {
        if (!isServer) return;
        for (int i = 0; i < MAXTEAMS; i++) teamData[i].Update();
    }

    [Server]
    public void GetGameModeData(ref GameModeData data, int totalCP)
    {
        currentGameModeData = data;
        this.totalCP = totalCP;
    }

    [Server]
    public void PlayerSelectedTeam(Player playerScript, int selectedTeam)
    {
        if (playerScript == null)
        {
            Debug.LogError($"TeamSelection Player component is null");
            playerScript.RpcTeamSelectionError(playerScript.connectionToClient, 0x00);
            return;
        }

        switch (selectedTeam)
        {
            case -1:
                selectedTeam = SelectUnBalancedTeam();
                break;

            case 0:
                SetAsSpectator(ref playerScript);
                break;

            default:
                if (selectedTeam > MAXTEAMS) Debug.LogError($"{playerScript.GetPlayerName()} selected a team out of range!");
                selectedTeam = Mathf.Clamp(selectedTeam, 0, MAXTEAMS);
                break;
        }

        int oldPlayerTeam = playerScript.GetPlayerTeam();
        if (oldPlayerTeam > 0) teamData[oldPlayerTeam - 1].playersInTeam.Remove(playerScript);

        playerScript.SetPlayerTeam(selectedTeam);
        if (playerScript.connectionToClient != null) playerScript.RpcTeamSelectionSuccess(playerScript.connectionToClient, selectedTeam);
        teamData[selectedTeam - 1].playersInTeam.Add(playerScript);
        GameModeManager.INS.OnPlayerSelectedTeam(playerScript);
        //GameModeManager.INS.SpawnPlayerByTeam(playerScript);
    }

    [Server]
    int SelectUnBalancedTeam()
    {
        int unbalancedTeam = -1, quantityOfPlayers = 9999;
        for (int i = 0; i < MAXTEAMS; i++)
        {
            if (teamData[i].playersInTeam.Count < quantityOfPlayers)
            {
                unbalancedTeam = i + 1;
                quantityOfPlayers = teamData[i].playersInTeam.Count;
            }
        }

        if (unbalancedTeam == -1) unbalancedTeam = Random.Range(1, MAXTEAMS + 1);
        return unbalancedTeam;
    }

    [Server]
    void SetAsSpectator(ref Player playerScript)
    {
        spectators.Add(playerScript);
    }

    [Server]
    public int[] SeparatePlayersPerTeam(ref List<Player> players)
    {
        int[] playersPerTeam = new int[MAXTEAMS];

        int size = players.Count;
        for (int i = 0; i < size; i++)
        {
            int playerTeam = players[i].GetPlayerTeam() - 1;
            if (playerTeam < 0) continue; // Ignore Spectators in the calculation
            playersPerTeam[playerTeam]++;
        }

        return playersPerTeam;
    }

    [Server]
    public void OnCapturedControlPoint(int team, int oldTeam)
    {
        switch (team)
        {
            case 1:
                teamData[0].nextPointToCapture++;
                teamData[0].controlledPoints++;

                if (oldTeam != 0)
                {
                    teamData[1].nextPointToCapture++;
                    teamData[1].controlledPoints--;
                }

                if (!teamData[0].CapturedFirstPoint) teamData[0].TeamCapturedFirstPoint();
                break;

            case 2:
                teamData[1].nextPointToCapture--;
                teamData[1].controlledPoints++;

                if (oldTeam != 0)
                {
                    teamData[0].nextPointToCapture--;
                    teamData[0].controlledPoints--;
                }

                if (!teamData[1].CapturedFirstPoint) teamData[1].TeamCapturedFirstPoint();
                break;

            default:
                Debug.LogError("The spectator team has captured a control point... This should be worrying");
                break;
        }

        if (teamData[0].CapturedFirstPoint && teamData[1].CapturedFirstPoint)
        {
            if (teamData[0].HasMorePoints(teamData[1].controlledPoints))
            {
                teamData[1].StartBleeding(currentGameModeData.cpHandicap);
                teamData[0].StopBleeding();
            }
            else if (teamData[1].HasMorePoints(teamData[0].controlledPoints))
            {
                teamData[0].StartBleeding(currentGameModeData.cpHandicap);
                teamData[1].StopBleeding();
            }
            else
            {
                for (int i = 0; i < MAXTEAMS; i++) 
                 teamData[i].StopBleeding();
            }
        }
    }

    [Server]
    public int GetTeamControlledPoints(int teamIndex)
    {
        return teamData[Mathf.Clamp(teamIndex, 0, MAXTEAMS)].nextPointToCapture;
    }

    [Server]
    public void RemoveTicket(int team, int quanity)
    {
        teamData[team - 1].RemoveTicket(quanity);
    }

    [Server]
    public int GetLoosingTeam()
    {
        if (teamData[0].Tickets == teamData[1].Tickets) return -1;
        return teamData[0].Tickets > 0 ? 1 : 2;
    }

    [Server]
    public void MatchEnded(int loosingTeam)
    {
        enabled = false;
        for (int i = 0; i < MAXTEAMS; i++) teamData[i].StopBleeding();
    }

    [Server]
    public int GetTicketsFromTeam(int team) => teamData[team - 1].Tickets;
    
    [Server]
    public List<Player> GetPlayersOnTeam(int teamIndex)
    {
        List<Player> playersOnTeam = new List<Player>();
        teamIndex = Mathf.Clamp(teamIndex, 0, MAXTEAMS);

        int size = teamData[teamIndex].playersInTeam.Count;
        for (int j = 0; j < size; j++)
            playersOnTeam.Add(teamData[teamIndex].playersInTeam[j]);
        
        return playersOnTeam;
    }

    public void OnClientDisconnects(Player playerDisconnected)
    {
        int playerTeam = playerDisconnected.GetPlayerTeam();
        if (playerTeam == 0)
        {
            Debug.LogWarning("Spectators not implemented");
            return;
        }

        teamData[playerTeam - 1].playersInTeam.Remove(playerDisconnected);
    }
}
