using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class Bullet : CachedTransform
{
    [SerializeField] DecalProjector bulletDecal;
    [SerializeField] ParticleSystem explosionPS;

    bool canTravel, canShowDecal, explodeOnHit, canExplode, isServer;
    float speed, timeToExplode, radius;
    Vector3 hitNormal;
    Vector3 destination;
    Rigidbody rb;

    public System.Action<List<HitBox>, Vector3, Quaternion> OnExplode;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Init(float initialSpeed, bool showDecal)
    {
        speed = initialSpeed;
        canShowDecal = showDecal;
    }

    void Update()
    {
        if (canExplode)
        {
            if (Time.time < timeToExplode) return;
            Explode();
            return;
        }

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

    public void PhysicsTravelTo(bool isServer, Vector3 direction, float explosionRadious, bool explodeOnTouch, float timeToExplode = 0)
    {
        rb.AddForce(direction * speed);
        rb.AddTorque(direction * (speed / 2));
        explodeOnHit = explodeOnTouch;

        this.timeToExplode = Time.time + timeToExplode;
        this.isServer = isServer;

        canExplode = true;
        radius = explosionRadious;
    }

    void OnCollisionEnter(Collision collision) { if (canExplode && explodeOnHit) Explode(); }

    void Explode()
    {
        if (isServer)
        {
            Collider[] possibleTargets = new Collider[30];
            int quantity = Physics.OverlapSphereNonAlloc(MyTransform.position, radius, possibleTargets, LayerMask.GetMask("PlayerHitBoxes"));

            //GameObject areaDebug = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //areaDebug.layer = LayerMask.NameToLayer("Debug");
            //areaDebug.transform.position = MyTransform.position;
            //areaDebug.transform.localScale = Vector3.one * radius * 2;

            List<HitBox> hitBoxList = new List<HitBox>();
            for (int i = 0; i < quantity; i++)
            {
                HitBox hitBox = possibleTargets[i].GetComponent<HitBox>();
                if (hitBox == null) continue;
                hitBoxList.Add(hitBox);
            }

            OnExplode?.Invoke(hitBoxList, MyTransform.position, MyTransform.rotation);
        }
        else
        {
            if (canShowDecal)
            {
                bulletDecal.gameObject.SetActive(true);
                bulletDecal.transform.position = MyTransform.position;
                bulletDecal.transform.SetParent(null);
            }

            //GameObject areaDebug = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //areaDebug.layer = LayerMask.NameToLayer("Debug");
            //areaDebug.transform.position = MyTransform.position;
            //areaDebug.transform.localScale = Vector3.one * radius * 2;
            //Debug.Break();

            explosionPS.Play();
            explosionPS.transform.parent = null;
        }
        
        canExplode = false;
        Destroy(gameObject);
    }
}
