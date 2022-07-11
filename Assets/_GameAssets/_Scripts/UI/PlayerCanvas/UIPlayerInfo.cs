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

    [SerializeField] Sprite playerDeadSprite;
    [SerializeField] Image imgPlayerClass;
    [SerializeField] TMP_Text lblplayerName, lblPlayerScore, lblPlayerRevives, lblPlayerDeaths;

    public PlayerScoreboardInfo PlayerInfoData { get; private set; }

    public void UpdateInfo(PlayerScoreboardInfo playerInfo)
    {
        PlayerInfoData = playerInfo;

        if (imgPlayerClass != null)
            imgPlayerClass.sprite = PlayerInfoData.isPlayerDead ? playerDeadSprite : ClassesSprites[PlayerInfoData.playerClass];

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

    public void SetTeamLayout(int team)
    {
        if (team == 0)
        {
            imgPlayerClass.rectTransform.localScale = Vector3.one;
            lblPlayerDeaths.rectTransform.localScale = Vector3.one;
            lblPlayerRevives.rectTransform.localScale = Vector3.one;

            LeanTween.moveX(lblplayerName.rectTransform, -135, 0);
            LeanTween.moveX(lblPlayerScore.rectTransform, 2, 0);
            return;
        }

        imgPlayerClass.rectTransform.localScale = Vector3.zero;
        lblPlayerDeaths.rectTransform.localScale = Vector3.zero;
        lblPlayerRevives.rectTransform.localScale = Vector3.zero;

        LeanTween.moveX(lblplayerName.rectTransform, -180, 0);
        LeanTween.moveX(lblPlayerScore.rectTransform, 185, 0);
    }
}
