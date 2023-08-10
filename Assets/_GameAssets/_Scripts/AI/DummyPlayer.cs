using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using HLProject.Scriptables;
using HLProject.Managers;
using HLProject.Weapons;

namespace HLProject.Characters
{
    public class DummyPlayer : Player
    {
        //public override void OnStartServer()
        //{
        //    base.OnStartServer();
        //    DummyStart();
        //}

        [Space(5), Header("Dummy Variables")]
        [SerializeField] GameObject selectionHelper;

        bool dummyPlayerOnDeadCooldown, dummyInit, dummyCanShoot;
        double timeForNextShoot;
        Vector3 movementTarget;
        Transform lastTargetHelper;
        NetWeapon currentWeapon;

        protected override void Start()
        {
            base.Start();
            LoadModelArray();
            if (isServer) Invoke(nameof(DummyStart), .25f);
        }

        protected override void Update()
        {
            base.Update();

            if (!isServer || !dummyInit) return;
            ProcessShooting();

            Vector3 diff = new Vector3(MyTransform.position.x - movementTarget.x, 0, MyTransform.position.z - movementTarget.z);
            Vector2 movInputs = new Vector2(diff.x > .2f ? 1 : (diff.x < -.2f ? -1 : 0), diff.z > .2f ? 1 : (diff.z < -.2f ? -1 : 0));

            PlayerMovement.InputFlag flags = PlayerMovement.InputFlag.Empty;
            if (diff.x > 5 || diff.z > 5) flags |= PlayerMovement.InputFlag.Sprint;

            Vector2 rotInputs = Vector3.zero;
            /*if (MyTransform.position.OnlyXZ() != movementTarget.OnlyXZ())
            {
                float targetAngle = Vector3.Angle(MyTransform.forward, Vector3.Normalize(movementTarget - MyTransform.position).OnlyXZ());
                Debug.LogFormat("{0} - Target Angle: {1} - Norm: {2}", targetAngle > 180, targetAngle, targetAngle / 180f);
                rotInputs = new Vector2(targetAngle > 180 ? -((targetAngle - 180f) / 180f) : (targetAngle / 180f), 0);
            }*/

            movementScript.ProcessPlayerInputs(movInputs, rotInputs, flags);
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

        protected override void CharacterDies(bool criticalHit, LastDamageInfo damageInfo)
        {
            dummyPlayerOnDeadCooldown = true;
            dummyInit = false;
            base.CharacterDies(criticalHit, damageInfo);
        }

        [Server]
        public void DummyStart()
        {
            GameModeManager.INS.TeamManagerInstance.PlayerSelectedTeam(this, -1);
            GameModeManager.INS.PlayerChangeClass(this, GetClassForSelectedTeam());
            GameModeManager.INS.SpawnPlayerByTeam(this);
        }

        public override void SpawnPlayer(Vector3 spawnPosition, Quaternion spawnRotation, float spawnTime)
        {
            base.SpawnPlayer(spawnPosition, spawnRotation, spawnTime);
            movementTarget = MyTransform.position;
            dummyInit = true;
            currentWeapon = inventory.GetCurrentWeapon();
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
            selectionHelper.SetActive(true);
        }

        [TargetRpc]
        public void RpcOnPlayerLostControl(NetworkConnection target)
        {
            selectionHelper.SetActive(false);
            Destroy(lastTargetHelper.gameObject);
        }

        [Server]
        public void CommandMoveTo(Vector3 target)
        {
            movementTarget = target;

            if (lastTargetHelper == null)
            {
                lastTargetHelper = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                lastTargetHelper.name = "Dummy Movement Target Helper";
                Collider coll = lastTargetHelper.GetComponent<Collider>();
                coll.enabled = false;
                Destroy(coll);
            }

            lastTargetHelper.position = movementTarget;
        }

        [Server]
        public void CommandToShoot()
        {
            dummyCanShoot = !dummyCanShoot;
            if (dummyCanShoot) timeForNextShoot = currentWeapon.TimeForNextShoot;
        }

        [Server]
        public void CommandToCycleWeapon()
        {
            inventory.ProcessCommand(PlayerInventory.DummyCommands.CycleWeapon);
        }

        [Server]
        void ProcessShooting()
        {
            if (!dummyCanShoot || currentWeapon.IsReloading || NetworkTime.time < timeForNextShoot) return;

            if (currentWeapon.WeaponMags <= 0)
            {
                dummyCanShoot = false;
                return;
            }

            if (currentWeapon.WeaponAmmo <= 0)
            {
                inventory.ProcessCommand(PlayerInventory.DummyCommands.Reload);
                return;
            }

            inventory.ProcessCommand(PlayerInventory.DummyCommands.Shoot);
            if (dummyCanShoot) timeForNextShoot = currentWeapon.TimeForNextShoot;
        }
    }
}
