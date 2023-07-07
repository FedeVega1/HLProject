using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HLProject
{
    public class Bullet : CachedTransform
    {
        [SerializeField] DecalProjector bulletDecal;
        [SerializeField] ParticleSystem explosionPS;
        [SerializeField] AudioSource aSrc;
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] AudioClip greandeTickSound;

        bool canTravel, canShowDecal, explodeOnHit, canExplode, isServer;
        float speed, timeToExplode, radius, grenadeTickTime, maxTimeToExplode;
        Vector3 startPosition;
        Vector3 destination;
        Rigidbody rb;

        AsyncOperationHandle<IList<AudioClip>> normalExplosionSounds, underwaterExplosionSound, explosionDebris;
        //AsyncOperationHandle<AudioClip> grenadeTickSoundHandle;

        public System.Action<List<HitBox>, Vector3, Quaternion> OnExplode;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void LoadAssets()
        {
            if (!canExplode) return;
            normalExplosionSounds = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "explode0", "explode1", "explode2", "explode3", "explode4", "explode5", "explode6", "explode7", "explode8" }, null, Addressables.MergeMode.Union);
            normalExplosionSounds.Completed += OnSoundsLoaded;

            explosionDebris = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "debris1", "debris2", "debris3", "debris4", "debris5", "debris6" }, null, Addressables.MergeMode.Union);
            explosionDebris.Completed += OnSoundsLoaded;

            /*grenadeTickSoundHandle = Addressables.LoadAssetAsync<AudioClip>("tick1");
            grenadeTickSoundHandle.Completed += OnSoundLoaded;*/
        }

        void OnSoundsLoaded(AsyncOperationHandle<IList<AudioClip>> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Couldn't load Sounds for bullet: {0}", operation.OperationException);
        }

        private void OnDestroy()
        {
            if (normalExplosionSounds.IsValid() && normalExplosionSounds.IsDone) 
                Addressables.Release(normalExplosionSounds);
        }

        /*void OnSoundLoaded(AsyncOperationHandle<AudioClip> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Couldn't load sound for bullet: {0}", operation.OperationException);
        }*/

        public void Init(float initialSpeed, bool showDecal)
        {
            speed = initialSpeed;
            canShowDecal = showDecal;
        }

        void Update()
        {
            if (canExplode)
            {
                if (Time.time < timeToExplode)
                {
                    if (Time.time >= grenadeTickTime)
                    {
                        aSrc.PlayOneShot(greandeTickSound);
                        float remainingTime = (timeToExplode - Time.time) / maxTimeToExplode;
                        float pow = Mathf.Pow(remainingTime, 1.2f);
                        if (pow < .18f) pow = .18f;
                        grenadeTickTime = Time.time + pow;
                        //Debug.LogFormat("{0} - {1} - {2} - {3}", (timeToExplode - Time.time), remainingTime, Mathf.Pow(remainingTime, 1.2f), grenadeTickTime);
                    }

                    return;
                }

                Explode();
                return;
            }

            if (!canTravel) return;

            //print($"Bullet {name} Distance: {Vector3.Distance(MyTransform.position, destination)}");
            if (Vector3.Distance(MyTransform.position, destination) <= .5f) OnHit();

            MyTransform.position = Vector3.MoveTowards(MyTransform.position, destination, speed * Time.deltaTime);
            //Vector3 direction = Vector3.Normalize(destination - MyTransform.position);
            //MyTransform.Translate(speed * Time.deltaTime * direction, Space.World);
        }

        public void TravelTo(Vector3 destination)//, Vector3 normal)
        {
            MyTransform.localPosition = Vector3.zero;
            //MyTransform.localRotation = Quaternion.identity;

            startPosition = MyTransform.position;
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
                bulletDecal.transform.rotation = Quaternion.LookRotation(Vector3.Normalize(destination - startPosition), Vector3.up);
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
            maxTimeToExplode = timeToExplode;
            this.isServer = isServer;

            canExplode = true;
            radius = explosionRadious;
            LoadAssets();

            aSrc.PlayOneShot(greandeTickSound);
            //float remainingTime = (this.timeToExplode - Time.time) / maxTimeToExplode;
            grenadeTickTime = Time.time + 1;
            //Debug.LogFormat("{0} - {1} - {2} - {3}", (this.timeToExplode - Time.time), remainingTime, Mathf.Pow(remainingTime, 4), grenadeTickTime);
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
                    bulletDecal.transform.rotation = Quaternion.identity;
                    bulletDecal.transform.SetParent(null);
                }
                
                aSrc.PlayOneShot(normalExplosionSounds.Result[Random.Range(0, normalExplosionSounds.Result.Count)]);
                LeanTween.delayedCall(1, () => aSrc.PlayOneShot(explosionDebris.Result[Random.Range(0, explosionDebris.Result.Count)]));
                //GameObject areaDebug = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //areaDebug.layer = LayerMask.NameToLayer("Debug");
                //areaDebug.transform.position = MyTransform.position;
                //areaDebug.transform.localScale = Vector3.one * radius * 2;
                //Debug.Break();

                explosionPS.Play();
                explosionPS.transform.parent = null;
            }

            canExplode = false;
            meshRenderer.enabled = false;
            StartCoroutine(WaitForSound(() => Destroy(gameObject)));
        }

        IEnumerator WaitForSound(System.Action endAction)
        {
            yield return new WaitUntil(() => !aSrc.isPlaying);
            endAction?.Invoke();
        }
    }
}
