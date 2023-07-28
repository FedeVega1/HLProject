using UnityEngine;
using UnityEngine.UI;
using HLProject.Managers;

namespace HLProject.UI.HUD
{
    public class UIControlPointCapture : MonoBehaviour
    {
        [SerializeField] Image imgPointFaction, imgCPProgressBackground, imgCPProgressSlide;
        [SerializeField] Slider cpCaptureProgress;

        bool onCapturePoint;
        float controlPointProgress;
        Sprite[] factionSprites;

        void Update()
        {
            if (onCapturePoint)
                cpCaptureProgress.value = Mathf.Lerp(cpCaptureProgress.value, controlPointProgress, Time.deltaTime);
        }

        public void Init(ref Sprite[] factionSprites)
        {
            this.factionSprites = factionSprites;
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
    }
}
