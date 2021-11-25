using UnityEngine;
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
}