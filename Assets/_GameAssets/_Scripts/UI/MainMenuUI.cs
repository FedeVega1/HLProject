using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.HighDefinition;

namespace HLProject
{
    public class MainMenuUI : MonoBehaviour
    {
        enum UIState { MainMenu, ConnectToServer }

        [SerializeField] CanvasGroup rayCastBlocker;
        [SerializeField] RectTransform connectToServerPanel;
        [SerializeField] TMP_InputField txtConnectToServer;
        [SerializeField] HDAdditionalCameraData mainMenuCamera;
        [SerializeField] UIOptions optionsMenu;
        [SerializeField] RectTransform errorPanel;

        UIState currentState;

        void Start()
        {
            txtConnectToServer.text = "localhost";
            currentState = UIState.MainMenu;

            optionsMenu.Init();
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

        public void StartGameServer() => GameManager.INS.CreateMatch();

        public void CheckServerIP()
        {

        }

        public void ConnectToServer() => GameManager.INS.ConnectToServerByIP(txtConnectToServer.text);

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
    }
}
