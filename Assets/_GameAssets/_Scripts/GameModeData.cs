using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameModeData", menuName = "Data/GameMode")]
public class GameModeData : ScriptableObject
{
    const int MAXTEAMS = 2;

    public bool allowRespawn;
    public int[] ticketsPerTeam;
    public int minNumbPlayersToCaputre;

    void OnValidate()
    {
        if (ticketsPerTeam == null || ticketsPerTeam.Length < MAXTEAMS || ticketsPerTeam.Length > MAXTEAMS)
            ticketsPerTeam = new int[MAXTEAMS];
    }
}
