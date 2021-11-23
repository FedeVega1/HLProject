using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerCanvas : MonoBehaviour
{
    [SerializeField] CanvasGroup teamSelectionCanvasGroup;
    [SerializeField] Sprite[] factionSprites;
    [SerializeField] Image imgPointFaction;

    [SerializeField] TMP_Text[] lblTeams;

    Player playerScript;

    public void Init(Player _player)
    {
        playerScript = _player;

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        for (int i = 0; i < TeamManager.MAXTEAMS; i++) lblTeams[i].text = TeamManager.FactionNames[i];
    }

    public void OnControlPoint(int pointTeam)
    {
        if (pointTeam == 0) imgPointFaction.sprite = null;
        else imgPointFaction.sprite = factionSprites[pointTeam];
        imgPointFaction.enabled = true;
    }

    public void OnExitControlPoint() => imgPointFaction.enabled = false;

    public void SelectTeam(int team)
    {
        playerScript.TrySelectTeam(team);
    }

    public void ToggleTeamSelection(bool toggle)
    {
        teamSelectionCanvasGroup.alpha = toggle ? 1 : 0;
        teamSelectionCanvasGroup.interactable = teamSelectionCanvasGroup.blocksRaycasts = toggle;
    }
}
