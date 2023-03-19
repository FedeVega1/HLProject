using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
//using Mirror;

public abstract class CachedTransform : CommonNetworkBehaviour
{
    Transform _MyTransform;
    public Transform MyTransform
    {
        get 
        {
            if (_MyTransform == null) _MyTransform = transform;
            return _MyTransform; 
        }
    }
}

public abstract class CachedNetTransform : CommonNetworkBehaviour
{
    Transform _MyTransform;
    public Transform MyTransform
    {
        get
        {
            if (_MyTransform == null) _MyTransform = transform;
            return _MyTransform;
        }
    }


}

[RequireComponent(typeof(Rigidbody))]
public abstract class CachedRigidBody : CommonNetworkBehaviour
{
    Rigidbody _MyRigidBody;
    public Rigidbody MyRigidBody
    {
        get
        {
            if (_MyRigidBody == null) _MyRigidBody = GetComponent<Rigidbody>();
            return _MyRigidBody;
        }
    }
}

[RequireComponent(typeof(RectTransform))]
public abstract class CachedRectTransform : MonoBehaviour
{
    RectTransform _MyRectTransform;
    public RectTransform MyRectTransform
    {
        get
        {
            if (_MyRectTransform == null) _MyRectTransform = GetComponent<RectTransform>();
            return _MyRectTransform;
        }
    }
}

public abstract class CommonNetworkBehaviour : NetworkBehaviour
{
    protected double NetTime => NetworkManager.Singleton.ServerTime.Time;

    ClientRpcParams _SendRpcToPlayer, _SendRpcToEveryoneExceptPlayer;
    protected ClientRpcParams SendRpcToPlayer
    {
        get
        {
            if (_SendRpcToPlayer.Send.TargetClientIds == null) _SendRpcToPlayer = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { NetworkBehaviourId } } };
            return _SendRpcToPlayer;
        }
    }

    protected ClientRpcParams SendRpcToEveryoneExceptPlayer
    {
        get
        {
            if (_SendRpcToEveryoneExceptPlayer.Send.TargetClientIds == null)
                SetBlacklistParam();

            return _SendRpcToEveryoneExceptPlayer;
        }
    }

    NetworkObject _NetObject;
    protected NetworkObject NetObject
    {
        get
        {
            if (_NetObject == null) _NetObject = GetComponent<NetworkObject>();
            return _NetObject;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            OnServerSpawn();
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        OnClientSpawn();
        if (IsLocalPlayer)
        {
            OnLocalPlayerSpawn();
            return;
        }

        OnExternalClientSpawn();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnServerDespawn();
            return;
        }

        OnClientDespawn();
        if (IsLocalPlayer)
        {
            OnLocalPlayerDespawn();
            return;
        }

        OnExternalClientDespawn();
    }

    protected virtual void OnServerSpawn() { }
    protected virtual void OnClientSpawn() { }
    protected virtual void OnLocalPlayerSpawn() { }
    protected virtual void OnExternalClientSpawn() { }

    protected virtual void OnServerDespawn() { }
    protected virtual void OnClientDespawn() { }
    protected virtual void OnLocalPlayerDespawn() { }
    protected virtual void OnExternalClientDespawn() { }

    void OnClientConnected(ulong clientID) => SetBlacklistParam();

    void SetBlacklistParam()
    {
        int size = NetworkManager.ConnectedClientsIds.Count;
        ulong[] array = new ulong[size - 1];

        for (int i = 0, n = 0; i < size; i++)
        {
            if (NetworkManager.ConnectedClientsIds[i] == NetworkBehaviourId) continue;
            array[n] = NetworkManager.ConnectedClientsIds[i];
        }

        _SendRpcToEveryoneExceptPlayer = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = array } };
    }
}

public static class Utilities
{
    public static string RemovePlayerNumber(this string playerName)
    {
        if (!playerName.Contains('#')) return playerName;

        string newPlayerName = "";
        int size = playerName.Length;

        for (int i = 0; i < size; i++)
        {
            if (playerName[i] == '#') break;
            newPlayerName += playerName[i];
        }

        return newPlayerName;
    }

    public static bool MouseOverUI()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}

public class PlayerTimer
{
    readonly Player affectedPlayer;
    readonly double timeToAction;
    readonly System.Action<Player> actionToPerform;

    public PlayerTimer(ref Player _Player, float time, System.Action<Player> action)
    {
        affectedPlayer = _Player;
        timeToAction = NetworkManager.Singleton.ServerTime.Time + time;
        actionToPerform = action;
    }

    public bool IsAffectedPlayer(ref Player playerToCheck) => playerToCheck == affectedPlayer;
    public bool OnTime() => NetworkManager.Singleton.ServerTime.Time >= timeToAction;
    public void PerformAction() => actionToPerform?.Invoke(affectedPlayer);
}