using System.Collections.Generic;
using UnityEngine;

public struct PlayerScoreboardInfo
{
    public int playerTeam;
    public int playerClass;
    public string playerName;
    public int playerScore;
    public int playerRevives;
    public int playerDeaths;
    public bool isLocalPlayer;
}

public class UIScoreBoard : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] RectTransform[] teamsPivot;
    [SerializeField] GameObject[] teamPrefabs;

    int playerTeam;
    List<UIPlayerInfo> playersInScore;

    void Start()
    {
        playersInScore = new List<UIPlayerInfo>();
    }

    public void Init(List<PlayerScoreboardInfo> playersInfo, int _playerTeam)
    {
        playerTeam = _playerTeam;
        int teamIndex = playerTeam == 0 ? 1 : 0;

        int size = playersInfo.Count;
        for (int i = 0; i < size; i++)
        {
            UIPlayerInfo info = Instantiate(teamPrefabs[teamIndex], teamsPivot[teamIndex]).GetComponent<UIPlayerInfo>();
            if (info == null) continue;
            info.UpdateInfo(playersInfo[i]);
            playersInScore.Add(info);
        }
    }

    public void AddNewPlayer(PlayerScoreboardInfo playerInfo)
    {
        int teamIndex = playerTeam == 0 ? 1 : 0;
        UIPlayerInfo info = Instantiate(teamPrefabs[teamIndex], teamsPivot[teamIndex]).GetComponent<UIPlayerInfo>();
        if (info == null) return;
        playersInScore.Add(info);
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
                playersInScore.Add(info);
                break;
            }
        }
    }
}
