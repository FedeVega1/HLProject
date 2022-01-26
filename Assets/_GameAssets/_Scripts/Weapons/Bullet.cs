using UnityEngine;

public class Bullet : CachedTransform
{
    bool canTravel;
    float speed;
    Vector3 destination;

    public void Init(float initialSpeed)
    {
        speed = initialSpeed;
    }

    void Update()
    {
        if (!canTravel) return;

        print($"Bullet {name} Distance: {Vector3.Distance(MyTransform.position, destination)}");
        if (Vector3.Distance(MyTransform.position, destination) <= .5f)
            Destroy(gameObject);

        MyTransform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    public void TravelTo(Vector3 destination)
    {
        MyTransform.localPosition = Vector3.zero;
        MyTransform.localRotation = Quaternion.identity;
        this.destination = destination;
    }
}
