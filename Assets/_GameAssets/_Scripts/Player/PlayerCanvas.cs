using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerCanvas : MonoBehaviour
{
    [SerializeField] CanvasGroup teamSelectionCanvasGroup, capturedCPNoticeCanvasGroup;
    [SerializeField] Sprite[] factionSprites;
    [SerializeField] Image imgPointFaction, imgCPProgressBackground, imgCPProgressSlide, imgCPNotice;
    [SerializeField] Slider cpCaptureProgress;
    [SerializeField] TMP_Text lblNotice;

    [SerializeField] TMP_Text[] lblTeams;

    bool onCapturePoint;
    float controlPointProgress;
    Player playerScript;

    void Update()
    {
        if (onCapturePoint) cpCaptureProgress.value = Mathf.Lerp(cpCaptureProgress.value, controlPointProgress, Time.deltaTime);
    }

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

    public void NewCapturedControlPoint(int cpTeam, string pointName)
    {
        LeanTween.cancel(capturedCPNoticeCanvasGroup.gameObject);
        LeanTween.alphaCanvas(capturedCPNoticeCanvasGroup, 1, .5f).setEaseInSine();
        LeanTween.alphaCanvas(capturedCPNoticeCanvasGroup, 0, .5f).setDelay(2.5f).setEaseOutSine();

        imgCPNotice.sprite = factionSprites[cpTeam];
        lblNotice.text = pointName + " captured";
    }

    public void OnPointCaptured(int newTeam, int newDefyingTeam)
    {
        if (newTeam == 0)
        {
            imgPointFaction.sprite = null;
            imgCPProgressBackground.color = Color.white;
        }
        else
        {
            imgPointFaction.sprite = factionSprites[newTeam];
            imgCPProgressBackground.color = TeamManager.FactionColors[newTeam - 1];
        }

        imgCPProgressSlide.color = newDefyingTeam == 0 ? Color.white : TeamManager.FactionColors[newDefyingTeam - 1];
        cpCaptureProgress.value = controlPointProgress = 0;
    }

    public void UpdateCPProgress(float progress) => controlPointProgress = progress;

    public void OnControlPoint(int pointTeam, int defyingTeam, float currentProgress)
    {
        OnPointCaptured(pointTeam, defyingTeam);

        imgPointFaction.color = Color.white;
        cpCaptureProgress.value = controlPointProgress = currentProgress;
        onCapturePoint = true;
    }

    public void OnExitControlPoint()
    {
        imgCPProgressSlide.color = imgPointFaction.color = imgCPProgressBackground.color = new Color(1, 1, 1, 0);
        onCapturePoint = false;
    }

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
