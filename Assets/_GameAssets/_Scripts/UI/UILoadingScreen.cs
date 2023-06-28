using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HLProject
{
    public class UILoadingScreen : MonoBehaviour
    {
        [SerializeField] CanvasGroup loadingScreenCanvasGroup;
        [SerializeField] Image imgLoadingScreen;

        public void ShowLoadingScreen()
        {
            loadingScreenCanvasGroup.blocksRaycasts = true;
            loadingScreenCanvasGroup.alpha = 1;
            //LeanTween.alphaCanvas(loadingScreenCanvasGroup, 1, .15f);
        }

        public void HideLoadingScreen()
        {
            LeanTween.alphaCanvas(loadingScreenCanvasGroup, 0, .15f).setOnComplete(() =>
            {
                loadingScreenCanvasGroup.blocksRaycasts = false;
            });
        }
    }
}
