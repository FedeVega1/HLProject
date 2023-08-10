using UnityEngine;

namespace HLProject
{
    public class CollisionDebugger : MonoBehaviour
    {
        void OnCollisionEnter(Collision collision)
        {
            Debug.LogFormat("<color=green>{0} collision with {1}</color>", name, collision.transform.name);
        }
    }
}
