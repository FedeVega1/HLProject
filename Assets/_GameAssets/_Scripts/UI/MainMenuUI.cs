using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.HighDefinition;
using HLProject.Managers;

namespace HLProject.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        enum UIState { MainMenu, ConnectToServer }

        [SerializeField] CanvasGroup rayCastBlocker, mainCanvasGroup, onMatchBackground;
        [SerializeField] RectTransform connectToServerPanel;
        [SerializeField] TMP_InputField txtConnectToServer;
        [SerializeField] HDAdditionalCameraData mainMenuCamera;
        [SerializeField] UIOptions optionsMenu;
        [SerializeField] RectTransform errorPanel;
        [SerializeField] TMP_Text lblPlay;
        [SerializeField] Button btnPlay;

        public bool IsMainMenuEnabled { get; private set; }

        bool onMatch;
        UIState currentState;

        public System.Action OnPlayerDisconnects;

        void Start()
        {
            txtConnectToServer.text = "localhost";
            currentState = UIState.MainMenu;
        }

        void Update()
        {
            switch (currentState)
            {
                case UIState.ConnectToServer:
                    if (Input.GetKeyDown(KeyCode.Return))
                        ConnectToServer();
                    break;
            }

            Cursor.lockState = CursorLockMode.Confined;
        }

        public void StartGameServer()
        {
            GameManager.INS.CreateMatch();
            ToggleConnectPanel(false);
            LocalPlayerOnMatch();
        }

        public void CheckServerIP()
        {

        }

        public void ConnectToServer()
        {
            GameManager.INS.ConnectToServerByIP(txtConnectToServer.text);
            ToggleConnectPanel(false);
            LocalPlayerOnMatch();
        }

        public void ToggleConnectPanel(bool toggle)
        {
            connectToServerPanel.localScale = toggle ? Vector3.one : Vector3.zero;
            rayCastBlocker.alpha = toggle ? 1 : 0;
            rayCastBlocker.blocksRaycasts = rayCastBlocker.interactable = toggle;
        }

        public void QuitGame() => GameManager.INS.QuitGame();

        public void DisableCameraBackground()
        {
            mainMenuCamera.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            mainMenuCamera.backgroundColorHDR = new Color(0x11, 0x11, 0x11, 0xFF);
        }

        public void ToggleErrorPanel(bool toggle)
        {
            errorPanel.localScale = toggle ? Vector3.one : Vector3.zero;
            rayCastBlocker.alpha = toggle ? 1 : 0;
            rayCastBlocker.blocksRaycasts = rayCastBlocker.interactable = toggle;
        }

        public void ToggleOptionsPanel(bool toggle)
        {
            optionsMenu.TogglePanel(toggle);
            ToggleConnectPanel(false);
            rayCastBlocker.alpha = toggle ? 1 : 0;
            rayCastBlocker.blocksRaycasts = rayCastBlocker.interactable = toggle;
        }

        public void ToggleMainMenu(bool toggle)
        {
            mainCanvasGroup.alpha = toggle ? 1 : 0;
            mainCanvasGroup.blocksRaycasts = mainCanvasGroup.interactable = toggle;
            IsMainMenuEnabled = toggle;
        }

        public void LocalPlayerOnMatch()
        {
            btnPlay.onClick.RemoveAllListeners();
            btnPlay.onClick.AddListener(() => OnPlayerDisconnects?.Invoke());
            btnPlay.interactable = true;
            lblPlay.text = "Disconnect";
            onMatch = true;

            onMatchBackground.alpha = 1;
            onMatchBackground.interactable = onMatchBackground.blocksRaycasts = true;
        }

        public void LocalPlayerExitsMatch()
        {
            lblPlay.text = "Play";
            btnPlay.onClick.RemoveAllListeners();
            btnPlay.onClick.AddListener(() => ToggleConnectPanel(true));
            btnPlay.interactable = true;
            ToggleConnectPanel(false);
            onMatch = false;

            onMatchBackground.alpha = 0;
            onMatchBackground.interactable = onMatchBackground.blocksRaycasts = false;
        }
    }
}
