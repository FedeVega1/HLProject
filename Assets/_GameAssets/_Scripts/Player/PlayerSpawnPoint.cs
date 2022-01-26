using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnPoint : CachedTransform
{
    [SerializeField] bool debugDrawPlayerMesh;
    [SerializeField] Mesh debugPlayerMesh;
    [SerializeField] int playerTeam;

    [HideInInspector] public bool playerOnPoint;

    GameObject currentPlayerOnSpawn;

    public bool SpawnPlayer(GameObject playerObject)
    {
        if (playerOnPoint) return false;
        currentPlayerOnSpawn = playerObject;
        playerOnPoint = true;
        return true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.gameObject == currentPlayerOnSpawn)
        {
            currentPlayerOnSpawn = null;
            playerOnPoint = false;
        }
    }

    void OnDrawGizmos()
    {
        if (!debugDrawPlayerMesh) return;

        Color color = playerTeam == 0 ? Color.white : TeamManager.FactionColors[playerTeam - 1];
        color.a = .35f;

        Gizmos.color = color;
        Gizmos.DrawMesh(debugPlayerMesh, transform.position + Vector3.up, transform.rotation);        
    }
}
