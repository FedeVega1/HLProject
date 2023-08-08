using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace HLProject.Characters
{
    public class ClientPlayerModel : CachedRectTransform
    {
        [SerializeField] Transform ragdollPivot, mainPivot, pivotIK;
        [SerializeField] MultiAimConstraint multiAim;

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

        void Start()
        {
            ragdollParts = ragdollPivot.GetComponentsInChildren<Rigidbody>(true);
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

        public void SetIKParent(Transform newParent) => pivotIK.SetParent(newParent);
        public Transform GetIKPivot() => pivotIK;

        //public void GetIKTarget(Transform target)
        //{
        //    WeightedTransformArray array = multiAim.data.sourceObjects;
        //    array.SetTransform(0, target);
        //    multiAim.data.sourceObjects = array;
        //}
    }
}
