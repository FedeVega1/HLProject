using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TeamManager : NetworkBehaviour
{
    public const int MAXTEAMS = 2;
    public static readonly string[] FactionNames = new string[MAXTEAMS] { "Combine", "Rebels" };
    public static readonly Color32[] FactionColors = new Color32[MAXTEAMS]
    {
        new Color32(0xE9, 0xD1, 0x1E, 0xFF),
        new Color32(0xCD, 0x71, 0x33, 0xFF)
    };

    int[] ticketsPerTeam;
    List<Player>[] playersByTeam;
    List<Player> spectators;

    public override void OnStartServer()
    {
        spectators = new List<Player>();
        playersByTeam = new List<Player>[MAXTEAMS];
        for (int i = 0; i < MAXTEAMS; i++) playersByTeam[i] = new List<Player>();
    }

    [Server]
    public void GetGameModeData(ref int[] ticketsPerTeam)
    {
        this.ticketsPerTeam = ticketsPerTeam;
    }

    [Server]
    public void PlayerSelectedTeam(Player playerScript, int selectedTeam)
    {
        if (playerScript == null)
        {
            Debug.LogError($"TeamSelection Player component is null");
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

        playerScript.SetPlayerTeam(selectedTeam);
        playersByTeam[selectedTeam - 1].Add(playerScript);
        GameModeManager.INS.SpawnPlayerByTeam(playerScript);
    }

    [Server]
    int SelectUnBalancedTeam()
    {
        int unbalancedTeam = -1, quantityOfPlayers = -9999;
        for (int i = 0; i < MAXTEAMS; i++)
        {
            if (playersByTeam[i].Count > quantityOfPlayers)
            {
                unbalancedTeam = i + 1;
                quantityOfPlayers = playersByTeam[i].Count;
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
}
