using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

public abstract class CachedTransform : MonoBehaviour
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

public abstract class CachedNetTransform : NetworkBehaviour
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
public abstract class CachedRigidBody : CachedTransform
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

    public static Vector3 RandomVector3(Vector3 axis, float min, float max)
    {
        float x = axis.x != 0 ? Random.Range(min, max) : 0;
        float y = axis.y != 0 ? Random.Range(min, max) : 0;
        float z = axis.z != 0 ? Random.Range(min, max) : 0;

        return new Vector3(x, y, z);
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
        timeToAction = NetworkTime.time + time;
        actionToPerform = action;
    }

    public bool IsAffectedPlayer(ref Player playerToCheck) => playerToCheck == affectedPlayer;
    public bool OnTime() => NetworkTime.time >= timeToAction;
    public void PerformAction() => actionToPerform?.Invoke(affectedPlayer);
}