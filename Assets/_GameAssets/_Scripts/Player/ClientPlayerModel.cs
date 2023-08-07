using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace HLProject.Characters
{
    public class ClientPlayerModel : MonoBehaviour
    {
        [SerializeField] Transform ragdollPivot;
        [SerializeField] MultiAimConstraint multiAim;

        Animator ragdollAnim;
        Rigidbody[] ragdollParts;
        float transSpeed;
        Vector3 multiAimTargetOffset, lastOffset;

        void Start()
        {
            ragdollParts = ragdollPivot.GetComponentsInChildren<Rigidbody>(true);
            ragdollAnim = ragdollPivot.GetComponent<Animator>();
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

        public void ReleaseRagdoll(string currentAnimation, Vector3 dir, float impulse)
        {
            ragdollAnim.Play(currentAnimation);

            int size = ragdollParts.Length;
            for (int i = 0; i < size; i++)
            {
                ragdollParts[i].isKinematic = false;
                ragdollParts[i].AddForce(dir * impulse, ForceMode.Impulse);
            }
        }

        public void ExplodeRagdoll(string currentAnimation, Vector3 pos, float radius, float impulse)
        {
            ragdollAnim.Play(currentAnimation);

            int size = ragdollParts.Length;
            for (int i = 0; i < size; i++)
            {
                ragdollParts[i].isKinematic = false;
                ragdollParts[i].AddExplosionForce(impulse, pos, radius, 0, ForceMode.Impulse);
            }
        }
    }
}
