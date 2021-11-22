using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] CanvasGroup rayCastBlocker;
    [SerializeField] RectTransform connectToServerPanel;
    [SerializeField] TMP_InputField txtConnectToServer;

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
}
