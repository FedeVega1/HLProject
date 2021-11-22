using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Player : NetworkBehaviour
{
    [SerializeField] GameObject playerCamera;

    void Start()
    {
        if (!isLocalPlayer)
        {
            Destroy(playerCamera);
        }
    }

    void Update()
    {
        
    }
}
