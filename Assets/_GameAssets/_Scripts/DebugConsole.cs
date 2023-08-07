using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HLProject.UI;

namespace HLProject
{
    public class DebugConsole : MonoBehaviour
    {
        [SerializeField] MainMenuUI mainMenuUI;

        void Awake()
        {
            Application.logMessageReceived += OnLogReceived;
        }

        void Start()
        {
            Debug.LogWarning("Test Warning");
            Debug.LogError("Test Error");
        }

        void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            mainMenuUI.MainUIConsole.WriteToConsole(new LogInfo { logMessage = condition, logStacktrace = stackTrace, logType = type });
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.PageUp)) mainMenuUI.ToggleDebugConsole();
        }
    }
}
