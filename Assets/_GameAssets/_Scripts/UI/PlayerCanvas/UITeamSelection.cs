using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HLProject
{
    public class UITeamSelection : MonoBehaviour
    {
        [SerializeField] TMP_Text[] lblTeams;
        [SerializeField] CanvasGroup teamSelectionCanvasGroup;

        public void Init()
        {
            for (int i = 0; i < TeamManager.MAXTEAMS; i++) lblTeams[i].text = TeamManager.FactionNames[i];
        }

        public void ToggleTeamSelection(bool toggle)
        {
            teamSelectionCanvasGroup.alpha = toggle ? 1 : 0;
            teamSelectionCanvasGroup.interactable = teamSelectionCanvasGroup.blocksRaycasts = toggle;
        }
    }
}
