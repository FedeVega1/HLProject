using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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

        NetworkAnimator playerAnim;
        Player owningPlayer;

        float movThreshold, runThreshold;
        Vector2 lerpedPlayerDir, playerDirTarget;

        public override void OnStartServer()
        {
            playerAnim = GetComponent<NetworkAnimator>();
            owningPlayer = GetComponent<Player>();
        }

        void Update()
        {
            if (!isServer) return;
            movThreshold += (owningPlayer.PlayerIsMoving() ? 4 : -4) * Time.deltaTime;
            runThreshold += (owningPlayer.PlayerIsRunning() ? 4 : -4) * Time.deltaTime;

            playerAnim.animator.SetFloat("Movement", movThreshold = Mathf.Clamp01(movThreshold));
            //playerAnim.animator.SetLayerWeight(1, movThreshold);
            playerAnim.animator.SetFloat("Run", runThreshold = Mathf.Clamp01(runThreshold));

            lerpedPlayerDir = Vector2.Lerp(lerpedPlayerDir, playerDirTarget, Time.deltaTime * 4);
            playerAnim.animator.SetFloat("XMovement", lerpedPlayerDir.x);
            playerAnim.animator.SetFloat("ZMovement", lerpedPlayerDir.y);
        }

        [Server]
        public void OnPlayerChangesWeapons(string newWeapon)
        {
            playerAnim.animator.SetInteger("CurrentGun", WeaponMapping[newWeapon]);
            playerAnim.animator.SetFloat("CurrentGunF", WeaponMappingF[newWeapon]);
            playerAnim.SetTrigger("ChangeWeapon");
        }

        [Server]
        public void OnPlayerCrouches()
        {
            playerAnim.SetTrigger("IsCrouched");
        }

        [Server]
        public void OnPlayerJumps()
        {
            playerAnim.SetTrigger("Jump");
        }

        [Server]
        public void OnPlayerShoots(bool onAltMode)
        {
            playerAnim.SetTrigger("Shoot");
            playerAnim.animator.SetBool("IsAltFire", onAltMode);
        }

        [Server]
        public void OnPlayerReloads()
        {
            playerAnim.SetTrigger("Reload");
        }

        [Server]
        public void OnPlayerAims()
        {
            playerAnim.SetTrigger("Aim");
        }

        [Server]
        public void ReturnToIdle() => playerAnim.SetTrigger("Idle");

        [Server]
        public void SetPlayerDirectionMovement(Vector2 dir)
        {
            playerDirTarget = dir;
        }
    }
}
