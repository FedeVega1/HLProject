using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayerInfo : MonoBehaviour
{
    [SerializeField] Sprite[] team1ClassSprites, team2ClassSprites;
    [SerializeField] Image imgPlayerClass;
    [SerializeField] TMP_Text lblplayerName, lblPlayerScore, lblPlayerRevives, lblPlayerDeaths;

    public PlayerScoreboardInfo PlayerInfoData { get; private set; }

    public void UpdateInfo(PlayerScoreboardInfo playerInfo)
    {
        PlayerInfoData = playerInfo;

        if (imgPlayerClass != null) imgPlayerClass.sprite = PlayerInfoData.playerTeam == 0 ? team1ClassSprites[PlayerInfoData.playerClass] : team2ClassSprites[PlayerInfoData.playerClass];
        if (lblplayerName != null) lblplayerName.text = PlayerInfoData.playerName;
        if (lblPlayerScore != null) lblPlayerScore.text = PlayerInfoData.playerScore.ToString("00");
        if (lblPlayerRevives != null) lblPlayerRevives.text = PlayerInfoData.playerRevives.ToString("00");
        if (lblPlayerDeaths != null) lblPlayerDeaths.text = PlayerInfoData.playerDeaths.ToString("00");
    }
}
