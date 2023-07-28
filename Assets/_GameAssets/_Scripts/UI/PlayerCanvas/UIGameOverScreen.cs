using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using HLProject.Managers;

namespace HLProject.UI.HUD
{
    public class UIGameOverScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup gameOverCanvasGroup;
        [SerializeField] TMP_Text[] lblTeamTickets;
        [SerializeField] Image[] imgTeamIcons;
        [SerializeField] TMP_Text lblLoosingTeam, lblTimeToChangeLevel;

        bool onScreen;
        double timeToChangeLevel;

        void Update()
        {
            if (!onScreen) return;

            double time = timeToChangeLevel - NetworkTime.time;
            if (time <= 0) return;

            lblTimeToChangeLevel.text = $"Time to change level: {System.Math.Round(time, 0)}";
        }

        public void Init(ref Sprite[] teamSprites)
        {
            int size = teamSprites.Length;
            for (int i = 1; i < size; i++) imgTeamIcons[i - 1].sprite = teamSprites[i];
        }

        public void ShowGameOverScreen(int loosingTeam, int[] tickets, double time)
        {
            timeToChangeLevel = time;
            lblTimeToChangeLevel.text = $"Time to change level: {time}";

            lblLoosingTeam.text = loosingTeam != -1 ? $"{TeamManager.FactionNames[loosingTeam]} lost" : "Draw";

            int size = TeamManager.MAXTEAMS;
            for (int i = 0; i < size; i++) lblTeamTickets[i].text = tickets[i].ToString();
            LeanTween.alphaCanvas(gameOverCanvasGroup, 1, .15f);

            onScreen = true;
        }
    }
}
