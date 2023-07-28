using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HLProject.Managers;
using HLProject.Characters;

namespace HLProject
{
    public class TeamBase : ControlPoint
    {
        [SerializeField] PlayerSpawnPoint[] spawnPoints;

        protected override void OnTriggerEnter(Collider other)
        {
            if (!isServer || !other.CompareTag("Player")) return;

            if (uncapturablePoint)
            {
                Player playerScript = other.GetComponent<Player>();
                if (playerScript.GetPlayerTeam() != currentTeam) GameModeManager.INS.PlayerOnEnemyBase(playerScript);
                playersInCP.Add(playerScript);
                return;
            }

            base.OnTriggerEnter(other);
        }

        protected override void OnTriggerExit(Collider other)
        {
            if (!isServer || !other.CompareTag("Player")) return;

            if (uncapturablePoint)
            {
                int size = playersInCP.Count;
                for (int i = 0; i < size; i++)
                {
                    if (playersInCP[i].gameObject == other.gameObject)
                    {
                        if (playersInCP[i].GetPlayerTeam() != currentTeam) GameModeManager.INS.PlayerLeftEnemyBase(playersInCP[i]);
                        playersInCP.RemoveAt(i);
                        break;
                    }
                }

                return;
            }

            base.OnTriggerExit(other);
        }

        public Transform GetFreeSpawnPoint(GameObject playerObject)
        {
            bool onFreePoint = false;
            int size = spawnPoints.Length;
            for (int i = 0; i < size; i++)
            {
                if (!spawnPoints[i].playerOnPoint)
                {
                    onFreePoint = true;
                    break;
                }
            }

            if (!onFreePoint) return transform;

            int random = Random.Range(0, size);
            while (!spawnPoints[random].SpawnPlayer(playerObject)) random = Random.Range(0, size);
            return spawnPoints[random].MyTransform;
        }
    }
}
