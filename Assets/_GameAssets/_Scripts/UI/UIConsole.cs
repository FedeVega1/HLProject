using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HLProject.UI
{
    public struct LogInfo
    {
        public Vector2 localSpace;
        public string logMessage, logStacktrace;
        public LogType logType;
    }

    public class UIConsole : CachedRectTransform
    {
        [SerializeField] TMP_Text lblDebugText;
        [SerializeField] TMP_InputField txtDebugCommands;
        [SerializeField] int maxLogHistory;
        [SerializeField] Color logColor, warningColor, errorColor;

        public bool IsEnabled => MyRectTransform.anchoredPosition.y == 0;

        float playerAdjustedSizeY;
        List<LogInfo> currentLogs;
        System.Text.StringBuilder stringBuilder;

        void Awake()
        {
            currentLogs = new List<LogInfo>();
            stringBuilder = new System.Text.StringBuilder();
        }

        void Start()
        {
            if (PlayerPrefs.HasKey("Console Prefered Size"))
            {
                playerAdjustedSizeY = PlayerPrefs.GetFloat("Console Prefered Size");
            }
            else
            {
                playerAdjustedSizeY = Screen.currentResolution.height;
                PlayerPrefs.SetFloat("Console Prefered Size", playerAdjustedSizeY);
            }

            MyRectTransform.anchoredPosition = Vector2.up * playerAdjustedSizeY;
            MyRectTransform.sizeDelta = new Vector2(MyRectTransform.sizeDelta.x, playerAdjustedSizeY);
        }

        public void ToggleConsole()
        {
            if (!IsEnabled)
            {
                LeanTween.moveY(MyRectTransform, 0, .25f).setEaseOutSine();
                return;
            }
            
            LeanTween.moveY(MyRectTransform, playerAdjustedSizeY, .15f);
        }

        public void WriteToConsole(LogInfo info)
        {
            stringBuilder.AppendFormat("<color={0}>{1}</color>\n", GetDebugColor(info.logType), info.logMessage);
            lblDebugText.text = stringBuilder.ToString();
            info.localSpace = new Vector2(1, 0);

            if (currentLogs.Count > 0)
                info.localSpace.y = currentLogs[currentLogs.Count - 1].localSpace.y;

            info.localSpace.y -= 15 + (lblDebugText.fontSize * (lblDebugText.textInfo.lineCount + 1));
            currentLogs.Add(info);
        }

        string GetDebugColor(LogType type) => type switch
        {
            LogType.Warning => ColorToHex(warningColor),
            LogType.Log => ColorToHex(logColor),
            _ => ColorToHex(errorColor),
        };

        string ColorToHex(Color32 colorToConvert)
        {
            string convertedString = "#";
            convertedString += colorToConvert.r.ToString("X2");
            convertedString += colorToConvert.g.ToString("X2");
            convertedString += colorToConvert.b.ToString("X2");
            return convertedString;
        }
    }
}
