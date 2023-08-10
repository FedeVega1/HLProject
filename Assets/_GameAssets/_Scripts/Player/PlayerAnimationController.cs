using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using HLProject.Scriptables;

namespace HLProject.Characters
{
    public class PlayerAnimationController : NetworkBehaviour
    {
        static readonly Dictionary<string, int> WeaponMapping = new Dictionary<string, int>()
        {
            { "Crowbar", 0 }, { "Stunstick", 0 },
            { "9mm Pistol", 1 }, { ".357 Magnum Revolver", 1 }, { "TEST PISTOL", 1 },
            { "SMG1", 2 }, { "SMG1 Grenade launcher", 2 }, { "TEST SMG", 2 },
            { "AR2 Rifle", 3 }, { "AR2 Launcher", 3 }, 
            { "Shotgun", 4 },
            { "Grenade", 5 }, { "TEST GRANADE", 5 },
        };

        static readonly Dictionary<string, float> WeaponMappingF = new Dictionary<string, float>()
        {
            { "Crowbar", .4f }, { "Stunstick", .4f }, 
            { "9mm Pistol", .6f }, { ".357 Magnum Revolver", .6f }, { "TEST PISTOL", .6f },
            { "SMG1", 1 }, { "SMG1 Grenade launcher", 1 }, { "TEST SMG", 1 },
            { "AR2 Rifle", 0 }, { "AR2 Launcher", 0 }, 
            { "Shotgun", .8f },
            { "Grenade", .2f }, { "TEST GRANADE", .2f },
        };

        public bool IsReady { get; private set; }

        NetworkAnimator playerAnim;
        Player owningPlayer;
        PlayerModelData modelData;

        float movThreshold, runThreshold, crouchThreshold;
        string currentAnim, lastAnim;
        Vector2 lerpedPlayerDir, playerDirTarget;

        public override void OnStartServer()
        {
            playerAnim = GetComponent<NetworkAnimator>();
            owningPlayer = GetComponent<Player>();
        }

        void Update()
        {
            if (!IsReady || !isServer) return;
            movThreshold += (owningPlayer.PlayerIsMoving() ? 4 : -4) * Time.deltaTime;
            runThreshold += (owningPlayer.PlayerIsRunning() ? 4 : -4) * Time.deltaTime;
            crouchThreshold += (owningPlayer.PlayerIsCrouched() ? 4 : -.8f) * Time.deltaTime;

            playerAnim.animator.SetFloat("Movement", movThreshold = Mathf.Clamp01(movThreshold));
            playerAnim.animator.SetFloat("Run", runThreshold = Mathf.Clamp01(runThreshold));
            playerAnim.animator.SetFloat("CrouchAmmount", crouchThreshold = Mathf.Clamp01(crouchThreshold));

            lerpedPlayerDir = Vector2.Lerp(lerpedPlayerDir, playerDirTarget, Time.deltaTime * 4);
            playerAnim.animator.SetFloat("XMovement", lerpedPlayerDir.x);
            playerAnim.animator.SetFloat("ZMovement", lerpedPlayerDir.y);

            if (movThreshold != 0) currentAnim = "Walk";
            else currentAnim = "Idle";
            if (runThreshold != 0) currentAnim = "Run";
            if (crouchThreshold != 0 && movThreshold != 0) currentAnim = "CrouchWalk";
            else if (crouchThreshold != 0) currentAnim = "Crouch";

            if (currentAnim != lastAnim)
            {
                var data = GetOffset();
                owningPlayer.PlayerModel.SetMultiAimOffset(data.Item1, data.Item2);
            }

            lastAnim = currentAnim;
        }

        [Server]
        public void OnPlayerChangesWeapons(string newWeapon)
        {
            if (!IsReady) return;
            playerAnim.animator.SetInteger("CurrentGun", WeaponMapping[newWeapon]);
            playerAnim.animator.SetFloat("CurrentGunF", WeaponMappingF[newWeapon]);
            playerAnim.SetTrigger("ChangeWeapon");
            currentAnim = "Change Weapon";
        }

        [Server]
        public void TogglePlayerCrouch(bool toggle)
        {
            if (!IsReady) return;
            playerAnim.animator.SetBool("IsCrouched", toggle);
            playerAnim.SetTrigger("Idle");
            currentAnim = "Idle";
        }

        [Server]
        public void OnPlayerJumps()
        {
            if (!IsReady) return;
            playerAnim.SetTrigger("Jump");
            currentAnim = "Jump";
        }

        [Server]
        public void OnPlayerLands()
        {
            if (!IsReady) return;
            playerAnim.SetTrigger("Landed");
            currentAnim = "Land";
        }

        [Server]
        public void OnPlayerShoots(bool onAltMode)
        {
            if (!IsReady) return;
            playerAnim.SetTrigger("Shoot");
            playerAnim.animator.SetBool("IsAltFire", onAltMode);
            currentAnim = "Shoot";
        }

        [Server]
        public void OnPlayerReloads()
        {
            if (!IsReady) return;
            playerAnim.SetTrigger("Reload");
            currentAnim = "Reload";
        }

        [Server]
        public void OnPlayerAims()
        {
            if (!IsReady) return;
            playerAnim.SetTrigger("Aim");
            currentAnim = "Aim";
        }

        [Server]
        public void ReturnToIdle()
        {
            if (!IsReady) return;
            playerAnim.SetTrigger("Idle");
            currentAnim = "Idle";
        }

        [Server]
        public void SetPlayerDirectionMovement(Vector2 dir)
        {
            if (!IsReady) return;
            playerDirTarget = dir;
        }

        public void SetPlayerModelData(ref PlayerModelData data, NetworkAnimator anim)
        {
            IsReady = true;
            modelData = data;
            playerAnim = anim;
        }

        (Vector3, float) GetOffset()
        {
            int size = modelData.armOffsets.Length;
            for (int i = 0; i < size; i++)
            {
                if (currentAnim == modelData.armOffsets[i].animation)
                    return (modelData.armOffsets[i].offset, modelData.armOffsets[i].transitionSpeed);
            }

            return (Vector3.positiveInfinity, 0);
        }

        public void OnPlayerDies()
        {
            IsReady = false;
            playerAnim = null;
        }
    }
}
