using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

namespace HLProject
{
    public class UIWoundedScreen : MonoBehaviour
    {
        [SerializeField] TMP_Text lblWoundTime;
        [SerializeField] Image obscurer;
        [SerializeField] RectTransform lblPlayerOutBounds, btnPlayerGiveUp;

        bool playerisWounded;
        int outOfBoundsTween;
        double woundTime;
        Coroutine OutOfBoundsRoutine;

        void Update()
        {
            if (!playerisWounded) return;

            double time = woundTime - NetworkTime.time;

            if (time <= 0)
            {
                time = 0;
                playerisWounded = false;
                //lblWoundTime.text = "You can Respawn now";
                return;
            }

            lblWoundTime.text = $"You are wounded: {System.Math.Round(time, 0)}";
        }

        public void PlayerOutOfBounds(float timeToDie)
        {
            Color obsColor = new Color32(0x0, 0x0, 0x0, 0x28);
            SetObscurerColor(obsColor);
            lblPlayerOutBounds.localScale = Vector3.one;

            OutOfBoundsRoutine = StartCoroutine(OutOfBoundsAnimation());

            outOfBoundsTween = LeanTween.value(.16f, 1, timeToDie).setOnUpdate((value) =>
            {
                obsColor.a = value;
                SetObscurerColor(obsColor);
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

        public void PlayerNotWounded() { playerisWounded = false; }

        public void PlayerIsWounded(double woundTime)
        {
            SetObscurerColor(new Color32(0x00, 0x00, 0x00, 0xFA));
            btnPlayerGiveUp.localScale = Vector3.one;
            lblWoundTime.rectTransform.localScale = Vector3.one;

            this.woundTime = woundTime;
            playerisWounded = true;
        }

        public void Hide()
        {
            btnPlayerGiveUp.localScale = Vector3.zero;
            lblWoundTime.rectTransform.localScale = Vector3.zero;
        }

        public void SetObscurerColor(Color newColor) => obscurer.color = newColor;

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
}
