using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HLProject.Characters;

namespace HLProject.Scriptables
{
    [CreateAssetMenu(fileName = "PlayerModelData", menuName = "Data/PlayerModel")]
    public class PlayerModelData : ScriptableObject
    {
        [System.Serializable]
        public struct AnimArmOffset
        {
            public string animation;
            public Vector3 offset;
            public float transitionSpeed;
        }

        public string modelName;
        public ClientPlayerModel playerModelObject;
        public AnimArmOffset[] armOffsets;
    }
}
