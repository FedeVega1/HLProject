using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class Bullet : CachedTransform
{
    [SerializeField] DecalProjector bulletDecal;

    bool canTravel, canShowDecal;
    float speed;
    Vector3 hitNormal;
    Vector3 destination;

    public void Init(float initialSpeed, bool showDecal)
    {
        speed = initialSpeed;
        canShowDecal = showDecal;
    }

    void Update()
    {
        if (!canTravel) return;

        //print($"Bullet {name} Distance: {Vector3.Distance(MyTransform.position, destination)}");
        if (Vector3.Distance(MyTransform.position, destination) <= .5f) OnHit();

        Vector3 direction = Vector3.Normalize(destination - MyTransform.position);
        MyTransform.Translate(speed * Time.deltaTime * direction, Space.World);
    }

    public void TravelTo(Vector3 destination)//, Vector3 normal)
    {
        MyTransform.localPosition = Vector3.zero;
        //MyTransform.localRotation = Quaternion.identity;

        this.destination = destination;
        //hitNormal = normal;
        canTravel = true;
    }

    void OnHit()
    {
        if (canShowDecal)
        {
            bulletDecal.gameObject.SetActive(true);
            bulletDecal.transform.position = destination;
            bulletDecal.transform.rotation = Quaternion.LookRotation(Vector3.Normalize(destination - MyTransform.position), Vector3.up);
            bulletDecal.transform.SetParent(null);
        }

        Destroy(gameObject);
    }
}
