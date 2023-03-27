using UnityEngine;
using Mirror;

public class EffectDetailLifetime : CachedTransform
{
    [SerializeField] float lifetime;

    double timeToFadeOut;
    Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();
    void OnEnable() => timeToFadeOut = NetworkTime.localTime + lifetime;

    void Update()
    {
        if (NetworkTime.localTime < timeToFadeOut) return;
        FadeObject();
    }

    void FadeObject()
    {
        enabled = false;
        if (rb != null) rb.isKinematic = true;
        LeanTween.scale(gameObject, Vector3.zero, .5f).setOnComplete(() => Destroy(gameObject));
    }
}
