using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using HLProject.Scriptables;
using HLProject.Managers;

namespace HLProject.Characters
{
    public class DummyPlayer : Player
    {
        //public override void OnStartServer()
        //{
        //    base.OnStartServer();
        //    DummyStart();
        //}

        bool dummyPlayerOnDeadCooldown;

        protected override void Start()
        {
            base.Start();
            if (isServer) Invoke(nameof(DummyStart), .25f);
        }

        protected override void PlayerWoundedUpdate()
        {
            if (isDead && dummyPlayerOnDeadCooldown)
            {
                LeanTween.delayedCall((float) timeToRespawn, () => GameModeManager.INS.SpawnPlayerByTeam(this));
                dummyPlayerOnDeadCooldown = false;
                return;
            }

            if (isDead || isInvencible || !isWounded || NetworkTime.time < woundedTime) return;
            base.PlayerWoundedUpdate();
        }

        protected override void CharacterDies(bool criticalHit)
        {
            dummyPlayerOnDeadCooldown = true;
            base.CharacterDies(criticalHit);
        }

        [Server]
        public void DummyStart()
        {
            GameModeManager.INS.TeamManagerInstance.PlayerSelectedTeam(this, -1);
            GameModeManager.INS.PlayerChangeClass(this, GetClassForSelectedTeam());
            GameModeManager.INS.SpawnPlayerByTeam(this);
        }

        int GetClassForSelectedTeam()
        {
            IList<TeamClassData> classDataArray = GameModeManager.INS.GetClassData();
            List<int> filteredClassData = new List<int>();

            int size = classDataArray.Count;
            for (int i = 0; i < size; i++)
            {
                if (classDataArray[i].teamSpecific == playerTeam && !classDataArray[i].className.Contains("Test"))
                {
                    filteredClassData.Add(i);
                    //print($"{playerTeam} - {classDataArray[i]}");
                }
            }

            int index = Random.Range(0, filteredClassData.Count);
            classData = classDataArray[filteredClassData[index]];
            //print($"{index} - {filteredClassData[index]} - {classData}");
            return filteredClassData[index];
        }

        [TargetRpc]
        public void RpcOnPlayerControl(NetworkConnection target)
        {

        }
    }
}
