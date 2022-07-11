using System.Collections.Generic;
using UnityEngine;

public readonly struct PlayerScoreboardInfo
{
    public readonly int playerTeam;
    public readonly int playerClass;
    public readonly string playerName;
    public readonly int playerScore;
    public readonly int playerRevives;
    public readonly int playerDeaths;
    public readonly bool isPlayerDead;
    public readonly bool isLocalPlayer;

    public PlayerScoreboardInfo(int team, int _playerClass, string name, int score, int revives, int deaths, bool dead, bool _isLocalPlayer)
    {
        playerTeam = team;
        playerClass = _playerClass;
        playerName = name;
        playerScore = score;
        playerRevives = revives;
        playerDeaths = deaths;
        isPlayerDead = dead;
        isLocalPlayer = _isLocalPlayer;
    }

    public PlayerScoreboardInfo(int team, string name, int score)
    {
        playerTeam = team;
        playerName = name;
        playerScore = score;

        playerClass = playerRevives = playerDeaths = 0;
        isLocalPlayer = isPlayerDead = false;
    }
}

public class UIScoreBoard : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] RectTransform[] teamsPivot;
    [SerializeField] GameObject teamPrefabs;

    int playerTeam;
    List<UIPlayerInfo> playersInScore;

    void Start()
    {
        playersInScore = new List<UIPlayerInfo>();
    }

    public void Init(PlayerScoreboardInfo[] playersInfo, int _playerTeam)
    {
        playerTeam = _playerTeam;

        int size = playersInfo.Length;
        for (int i = 0; i < size; i++)
        {
            if (playersInfo[i].playerTeam == 0 || playersInfo[i].playerName == "") continue;

            bool updateList = false;
            UIPlayerInfo info;
            int teamIndex = playersInfo[i].playerTeam != playerTeam ? 1 : 0;

            if (playersInScore.Count > 0 && i < playersInScore.Count)
            {
                info = playersInScore[i];
                info.SetTeamLayout(teamIndex);
                info.transform.parent = teamsPivot[teamIndex];
            }
            else
            {
                info = Instantiate(teamPrefabs, teamsPivot[teamIndex]).GetComponent<UIPlayerInfo>();
                info.SetTeamLayout(teamIndex);
                updateList = true;
            }
            
            if (info == null) continue;
            
            info.UpdateInfo(playersInfo[i]);
            if (updateList) playersInScore.Add(info);
        }
    }

    public void AddNewPlayer(PlayerScoreboardInfo playerInfo)
    {
        int teamIndex = playerInfo.playerTeam != playerTeam ? 1 : 0;
        UIPlayerInfo info = Instantiate(teamPrefabs, teamsPivot[teamIndex]).GetComponent<UIPlayerInfo>();
        if (info == null) return;
        playersInScore.Add(info);
        info.SetTeamLayout(teamIndex);
        info.UpdateInfo(playerInfo);
    }

    public void RemovePlayer(string playerName)
    {
        int size = playersInScore.Count;
        for (int i = 0; i < size; i++)
        {
            UIPlayerInfo info = playersInScore[playerTeam].GetComponent<UIPlayerInfo>();
            if (info == null) continue;
            if (info.PlayerInfoData.playerName == playerName)
            {
                Destroy(info.gameObject);
                playersInScore.Remove(info);
                break;
            }
        }
    }

    public void Toggle(bool toggle) => canvasGroup.alpha = toggle ? 1 : 0;
}
