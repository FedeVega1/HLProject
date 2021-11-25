using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Mirror;

public class PlayerCanvas : MonoBehaviour
{
    [SerializeField] Sprite[] factionSprites;

    [SerializeField] UIControlPointCapture cpCapture;
    [SerializeField] UIControlPointNotice cpNotice;
    [SerializeField] UITeamSelection teamSelection;

    [SerializeField] Image obscurer;
    [SerializeField] RectTransform lblPlayerOutBounds;
    [SerializeField] TMP_Text lblRespawnTime;

    bool playerisDead;
    int outOfBoundsTween;
    double timeToRespawn;
    Coroutine OutOfBoundsRoutine;
    Player playerScript;

    void Update()
    {
        if (playerisDead)
        {
            double time = timeToRespawn - NetworkTime.time;

            if (time <= 0)
            {
                time = 0;
                playerisDead = false;
                lblRespawnTime.text = "You can Respawn now";
                return;
            }

            lblRespawnTime.text = $"You can Respawn in {System.Math.Round(time, 0)}";
        }
    }

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
    }

    public void PlayerOutOfBounds(float timeToDie)
    {
        Color obsColor = new Color32(0x0, 0x0, 0x0, 0x28);
        obscurer.color = obsColor;
        lblPlayerOutBounds.localScale = Vector3.one;

        OutOfBoundsRoutine = StartCoroutine(OutOfBoundsAnimation());

        outOfBoundsTween = LeanTween.value(.16f, 1, timeToDie).setOnUpdate((value) =>
        {
            obsColor.a = value;
            obscurer.color = obsColor;
        }).uniqueId;
    }

    public void PlayerInBounds()
    {
        LeanTween.cancel(outOfBoundsTween);
        if (OutOfBoundsRoutine != null)
        {
            StopCoroutine(OutOfBoundsRoutine);
            OutOfBoundsRoutine = null;
        }

        obscurer.color = new Color(0, 0, 0, 0);
        lblPlayerOutBounds.localScale = Vector3.zero;
    }

    public void PlayerRespawn()
    {
        playerisDead = false;
        lblRespawnTime.rectTransform.localScale = Vector3.zero;
    }

    public void PlayerDied(double timeToRespawn)
    {
        PlayerInBounds();
        playerisDead = true;
        lblRespawnTime.rectTransform.localScale = Vector3.one;
        this.timeToRespawn = timeToRespawn;
    }

    #region Buttons

    public void SelectTeam(int team) => playerScript.TrySelectTeam(team);

    #endregion

    #region Redirections

    public void OnPointCaptured(int newTeam, int newDefyingTeam) => cpCapture.OnPointCaptured(newTeam, newDefyingTeam);
    public void UpdateCPProgress(float progress) => cpCapture.UpdateCPProgress(progress);
    public void OnControlPoint(int pointTeam, int defyingTeam, float currentProgress) => cpCapture.OnControlPoint(pointTeam, defyingTeam, currentProgress);
    public void OnExitControlPoint() => cpCapture.OnExitControlPoint();
    public void NewCapturedControlPoint(int cpTeam, string pointName) => cpNotice.NewCapturedControlPoint(cpTeam, pointName);
    public void ToggleTeamSelection(bool toggle) => teamSelection.ToggleTeamSelection(toggle);

    #endregion

    IEnumerator OutOfBoundsAnimation()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(.8f, 1.25f));
            for (int i = 0; i < 2; i++)
            {
                LeanTween.scale(lblPlayerOutBounds.gameObject, Vector3.zero, 0).setLoopPingPong(1);
                yield return new WaitForSeconds(.1f);
            }
        }
    }
}
