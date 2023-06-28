using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HLProject
{
    public class UIControlPointNotice : MonoBehaviour
    {
        [SerializeField] Image imgCPNotice;
        [SerializeField] TMP_Text lblNotice;
        [SerializeField] CanvasGroup capturedCPNoticeCanvasGroup;

        Sprite[] factionSprites;

        public void Init(ref Sprite[] factionSprites) => this.factionSprites = factionSprites;

        public void NewCapturedControlPoint(int cpTeam, string pointName)
        {
            LeanTween.cancel(capturedCPNoticeCanvasGroup.gameObject);
            LeanTween.alphaCanvas(capturedCPNoticeCanvasGroup, 1, .5f).setEaseInSine();
            LeanTween.alphaCanvas(capturedCPNoticeCanvasGroup, 0, .5f).setDelay(2.5f).setEaseOutSine();

            imgCPNotice.sprite = factionSprites[cpTeam];
            lblNotice.text = pointName + " captured";
        }
    }
}
