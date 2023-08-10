using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;

namespace HLProject.Characters
{
    public class ClientPlayerModel : CachedTransform
    {
        [SerializeField] Transform ragdollPivot, mainPivot, pivotIK;
        [SerializeField] MultiAimConstraint multiAim;

#if UNITY_EDITOR
        [SerializeField] float testImpulse;
#endif

        NetworkAnimator _ModelAnimator;
        public NetworkAnimator ModelAnimator
        {
            get
            {
                if (_ModelAnimator == null) _ModelAnimator = GetComponent<NetworkAnimator>();
                return _ModelAnimator;
            }
        }

        SkinnedMeshRenderer _ModelMeshRenderer;
        public SkinnedMeshRenderer ModelMeshRenderer
        {
            get
            {
                if (_ModelMeshRenderer == null) _ModelMeshRenderer = mainPivot.GetComponentInChildren<SkinnedMeshRenderer>();
                return _ModelMeshRenderer;
            }
        }

        Animator ragdollAnim;
        Rigidbody[] ragdollParts;
        float transSpeed;
        Vector3 multiAimTargetOffset, lastOffset;
        SkinnedMeshRenderer ragdollMeshRenderer;
        Vector3[] ragdollPartsOriginalPos;
        Quaternion[] ragdollPartsOriginalRot;
        Transform ragdollHeadPivot;

        void Start()
        {
            ragdollParts = ragdollPivot.GetComponentsInChildren<Rigidbody>(true);

            ragdollPartsOriginalPos = new Vector3[ragdollParts.Length];
            ragdollPartsOriginalRot = new Quaternion[ragdollParts.Length];

            ragdollAnim = ragdollPivot.GetComponent<Animator>();
            ragdollMeshRenderer = ragdollPivot.GetComponent<SkinnedMeshRenderer>();
            multiAimTargetOffset = multiAim.data.offset;
        }

        void Update()
        {
            if (multiAimTargetOffset.CompareXYZ(lastOffset, Vector3.kEpsilon)) return;
            multiAim.data.offset = Vector3.Slerp(multiAim.data.offset, multiAimTargetOffset, Time.deltaTime * transSpeed);
            lastOffset = multiAim.data.offset;
        }

        public void SetMultiAimOffset(Vector3 newOffset, float speed)
        {
            if (newOffset.x == Vector3.positiveInfinity.x || newOffset.x == Vector3.negativeInfinity.x) return;
            multiAimTargetOffset = newOffset;
            transSpeed = speed;
        }

        public void ToggleMainModel(bool toggle) => mainPivot.gameObject.SetActive(toggle);

        public void ReleaseRagdoll(Vector3 dir, float impulse)
        {
            if (gameObject == null) return;
            StartCoroutine(ReleaseRagdollRoutine(dir, impulse));
        }

        public void ExplodeRagdoll(Vector3 pos, float radius, float impulse)
        {
            if (gameObject == null) return;
            StartCoroutine(ExplodeRagdollRoutine(pos, radius, impulse));
        }

        public void SetIKParent(Transform newParent) => pivotIK.SetParent(newParent);
        public Transform GetIKPivot() => pivotIK;

        IEnumerator ReleaseRagdollRoutine(Vector3 dir, float impulse)
        {
            ragdollPivot.gameObject.SetActive(true);
            ragdollPivot.parent = null;
            //yield return null;

            /*AnimatorClipInfo uppperClipInfo = ModelAnimator.animator.GetCurrentAnimatorClipInfo(0)[0];
            AnimatorClipInfo bottomClipInfo = ModelAnimator.animator.GetCurrentAnimatorClipInfo(1)[0];

            ragdollAnim.Play(uppperClipInfo.clip.name, 0, ModelAnimator.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            ragdollAnim.Play(bottomClipInfo.clip.name, 1, ModelAnimator.animator.GetCurrentAnimatorStateInfo(1).normalizedTime);

            Debug.LogFormat("Ragdoll Upper Anim: {0} - Bottom Anim: {1}", uppperClipInfo.clip.name, bottomClipInfo.clip.name);*/

            ragdollAnim.enabled = false;
            yield return new WaitForEndOfFrame();

            int size = ragdollParts.Length;
            for (int i = 0; i < size; i++)
            {
                ragdollParts[i].isKinematic = false;
                ragdollPartsOriginalPos[i] = ragdollParts[i].position;
                ragdollPartsOriginalRot[i] = ragdollParts[i].rotation;
                ragdollParts[i].AddForce(dir * impulse, ForceMode.Impulse);
            }
        }

        IEnumerator ExplodeRagdollRoutine(Vector3 pos, float radius, float impulse)
        {
            ragdollPivot.gameObject.SetActive(true);
            ragdollPivot.parent = null;
            //yield return null;

            /*AnimatorClipInfo uppperClipInfo = ModelAnimator.animator.GetCurrentAnimatorClipInfo(0)[0];
            AnimatorClipInfo bottomClipInfo = ModelAnimator.animator.GetCurrentAnimatorClipInfo(1)[0];

            ragdollAnim.Play(uppperClipInfo.clip.name, 0, ModelAnimator.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            ragdollAnim.Play(bottomClipInfo.clip.name, 1, ModelAnimator.animator.GetCurrentAnimatorStateInfo(1).normalizedTime);
            
            Debug.LogFormat("Ragdoll Upper Anim: {0} - Bottom Anim: {1}", uppperClipInfo.clip.name, bottomClipInfo.clip.name);*/

            ragdollAnim.enabled = false;
            yield return new WaitForEndOfFrame();

            int size = ragdollParts.Length;
            for (int i = 0; i < size; i++)
            {
                ragdollParts[i].isKinematic = false;
                ragdollPartsOriginalPos[i] = ragdollParts[i].position;
                ragdollPartsOriginalRot[i] = ragdollParts[i].rotation;
                ragdollParts[i].AddExplosionForce(impulse, pos, radius, 0, ForceMode.Impulse);
            }
        }

        IEnumerator ResetRagdoll()
        {
            ragdollAnim.enabled = true;
            yield return new WaitForEndOfFrame();

            ragdollPivot.gameObject.SetActive(false);
            ragdollPivot.SetParent(MyTransform);
            ragdollPivot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            int size = ragdollParts.Length;
            for (int i = 0; i < size; i++)
            {
                ragdollParts[i].ResetCenterOfMass();
                ragdollParts[i].ResetInertiaTensor();
                ragdollParts[i].angularVelocity = Vector3.zero;
                ragdollParts[i].velocity = Vector3.zero;

                ragdollParts[i].isKinematic = true;
                ragdollParts[i].position = ragdollPartsOriginalPos[i];
                ragdollParts[i].rotation = ragdollPartsOriginalRot[i];
            }
        }

        public Transform GetRagdollHeadPivot()
        {
            if (ragdollHeadPivot == null)
            {
                int size = ragdollParts.Length;
                for (int i = 0; i < size; i++)
                {
                    if (ragdollParts[i].name.Contains("Head"))
                    {
                        ragdollHeadPivot = ragdollParts[i].transform;
                        break;
                    }
                }
            }

            return ragdollHeadPivot;
        }

#if UNITY_EDITOR
        [ContextMenu("Release Ragdoll")]
        public void ForceReleaseRagdoll() => StartCoroutine(ReleaseRagdollRoutine(-ragdollPivot.forward, testImpulse));

        [ContextMenu("Reset Ragdoll")]
        public void ForceResetRagdoll() => StartCoroutine(ResetRagdoll());
#endif
    }
}
