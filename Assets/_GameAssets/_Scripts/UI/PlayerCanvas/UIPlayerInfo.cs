using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPlayerInfo : MonoBehaviour
{
    static Sprite[] _ClassesSprites;
    Sprite[] ClassesSprites
    {
        get
        {
            if (_ClassesSprites == null) _ClassesSprites = GameModeManager.INS.GetClassesSprites();
            return _ClassesSprites;
        }
    }

    [SerializeField] Sprite[] team1ClassSprites, team2ClassSprites;
    [SerializeField] Image imgPlayerClass;
    [SerializeField] TMP_Text lblplayerName, lblPlayerScore, lblPlayerRevives, lblPlayerDeaths;

    public PlayerScoreboardInfo PlayerInfoData { get; private set; }

    public void UpdateInfo(PlayerScoreboardInfo playerInfo)
    {
        PlayerInfoData = playerInfo;
        if (imgPlayerClass != null) imgPlayerClass.sprite = ClassesSprites[PlayerInfoData.playerClass];

        if (lblplayerName != null)
        {
            lblplayerName.text = PlayerInfoData.playerName;
            lblplayerName.fontStyle = PlayerInfoData.isLocalPlayer ? FontStyles.Bold : FontStyles.Normal;
        }

        if (lblPlayerScore != null)
        {
            lblPlayerScore.text = PlayerInfoData.playerScore.ToString("00");
            lblplayerName.fontStyle = PlayerInfoData.isLocalPlayer ? FontStyles.Bold : FontStyles.Normal;
        }

        if (lblPlayerRevives != null)
        {
            lblPlayerRevives.text = PlayerInfoData.playerRevives.ToString("00");
            lblplayerName.fontStyle = PlayerInfoData.isLocalPlayer ? FontStyles.Bold : FontStyles.Normal;
        }

        if (lblPlayerDeaths != null)
        {
            lblPlayerDeaths.text = PlayerInfoData.playerDeaths.ToString("00");
            lblplayerName.fontStyle = PlayerInfoData.isLocalPlayer ? FontStyles.Bold : FontStyles.Normal;
        }
    }
}
