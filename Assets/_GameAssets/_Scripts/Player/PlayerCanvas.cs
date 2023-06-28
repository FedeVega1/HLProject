using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Mirror;

namespace HLProject
{
    public class PlayerCanvas : MonoBehaviour
    {
        [SerializeField] Sprite[] factionSprites;

        [SerializeField] UIControlPointCapture cpCapture;
        [SerializeField] UIControlPointNotice cpNotice;
        [SerializeField] UITeamSelection teamSelection;
        [SerializeField] UIGameOverScreen gameOverScreen;
        [SerializeField] UITeamClassSelection teamClassSelection;
        [SerializeField] UIWoundedScreen woundedScreen;
        [SerializeField] UIAmmoCounter ammoCounter;
        [SerializeField] UIScoreBoard scoreBoard;

        public bool IsTeamSelectionMenuOpen { get; private set; }
        public bool IsClassSelectionMenuOpen { get; private set; }
        public bool IsScoreboardMenuOpen { get; private set; }

        Player playerScript;
        int[] teamTickets;

        public void Init(Player _player)
        {
            playerScript = _player;

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            teamSelection.Init();
            cpCapture.Init(ref factionSprites);
            cpNotice.Init(ref factionSprites);
            gameOverScreen.Init(ref factionSprites);
            teamClassSelection.ToggleSpawnButton(false);

            teamTickets = new int[TeamManager.MAXTEAMS];
            ToggleCursor(false);
        }

        public void PlayerNotWounded()
        {
            ResetHUD();
            woundedScreen.PlayerNotWounded();
        }

        public void PlayerRespawn()
        {
            ResetHUD();
        }

        public void PlayerIsWounded(double woundTime)
        {
            ResetHUD();
            woundedScreen.PlayerIsWounded(woundTime);
            ToggleCursor(true);
        }

        void ResetHUD()
        {
            PlayerInBounds();
            Cursor.lockState = CursorLockMode.Locked;

            teamSelection.ToggleTeamSelection(false);
            teamClassSelection.ToggleClassSelection(false);
            scoreBoard.Toggle(false);
            ToggleWeaponInfo(false);

            IsTeamSelectionMenuOpen = IsClassSelectionMenuOpen = IsScoreboardMenuOpen = false;

            woundedScreen.Hide();
            cpCapture.OnExitControlPoint();
        }

        public void ShowGameOverScreen(int loosingTeam, double levelChangeTime)
        {
            ResetHUD();
            woundedScreen.SetObscurerColor(new Color32(0x0, 0x0, 0x0, 0x43));
            gameOverScreen.ShowGameOverScreen(loosingTeam, teamTickets, levelChangeTime);
            ToggleCursor(true);
        }

        public void SetTeamTickets(int team, int tickets) => teamTickets[team] = tickets;

        #region Buttons

        public void SelectTeam(int team) => playerScript.TrySelectTeam(team);
        public void SelectClass(int classIndex) => playerScript.TrySelectClass(classIndex);
        public void Respawn() => playerScript.TryPlayerSpawn();
        public void GiveUp() => playerScript.TryWoundedGiveUp();

        #endregion

        public void ToggleTeamSelection(bool toggle)
        {
            teamSelection.ToggleTeamSelection(toggle);
            ToggleCursor(toggle);
            IsTeamSelectionMenuOpen = toggle;
        }

        public void ToggleClassSelection(bool toggle)
        {
            teamClassSelection.ToggleClassSelection(toggle);
            ToggleCursor(toggle);
            IsClassSelectionMenuOpen = toggle;
        }

        public void ToggleScoreboard(bool toggle)
        {
            scoreBoard.Toggle(toggle);
            IsScoreboardMenuOpen = toggle;
            //Cursor.lockState = toggle ? CursorLockMode.Confined : CursorLockMode.Locked;
        }

        void ToggleCursor(bool toggle)
        {
            Cursor.lockState = toggle ? CursorLockMode.Confined : CursorLockMode.Locked;
            Cursor.visible = toggle;
        }

        #region Redirections

        public void OnPointCaptured(int newTeam, int newDefyingTeam) => cpCapture.OnPointCaptured(newTeam, newDefyingTeam);
        public void UpdateCPProgress(float progress) => cpCapture.UpdateCPProgress(progress);
        public void OnControlPoint(int pointTeam, int defyingTeam, float currentProgress) => cpCapture.OnControlPoint(pointTeam, defyingTeam, currentProgress);
        public void OnExitControlPoint() => cpCapture.OnExitControlPoint();
        public void NewCapturedControlPoint(int cpTeam, string pointName) => cpNotice.NewCapturedControlPoint(cpTeam, pointName);
        public void OnTeamSelection(int team) => teamClassSelection.Init(this, GameModeManager.INS.GetClassData(), team, ref playerScript);
        public void ToggleSpawnButton(bool toggle) => teamClassSelection.ToggleSpawnButton(toggle);
        public void OnClassSelection(int index) => teamClassSelection.ClassSelected(index);
        public void PlayerOutOfBounds(float timeToDie) => woundedScreen.PlayerOutOfBounds(timeToDie);
        public void PlayerInBounds() => woundedScreen.PlayerInBounds();
        public void ShowRespawnTimer(double time) => teamClassSelection.ShowRespawnTimer(time);
        public void SetCurrentWeapon(string weaponName) => ammoCounter.SetCurrentWeapon(weaponName);
        public void SetCurrentAmmo(int bullets, int mags) => ammoCounter.SetCurrentAmmo(bullets, mags);
        public void ToggleWeaponInfo(bool toggle) => ammoCounter.Toggle(toggle);
        public void InitScoreboard(PlayerScoreboardInfo[] info, int playerTeam) => scoreBoard.Init(info, playerTeam);
        public void AddPlayerToScoreboard(PlayerScoreboardInfo playerInfo) => scoreBoard.AddNewPlayer(playerInfo);
        public void RemovePlayerFromScoreboard(string playerName) => scoreBoard.RemovePlayer(playerName);

        #endregion
    }
}
