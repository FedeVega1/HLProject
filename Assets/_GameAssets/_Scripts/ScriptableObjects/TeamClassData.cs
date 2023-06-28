using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HLProject
{
    [CreateAssetMenu(fileName = "TeamClassData", menuName = "Data/TeamClass")]
    public class TeamClassData : ScriptableObject
    {
        public string className;
        public Sprite classSprite;
        public int teamSpecific;
        public ClientHandType classVHands;
        public WeaponData[] classWeapons;
    }
}
