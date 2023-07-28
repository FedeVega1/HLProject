using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HLProject.Managers;
using HLProject.Characters;

namespace HLProject.Weapons
{
    public class Bullet : CachedTransform
    {
        [SerializeField] bool alwaysFaceCamera;
        [SerializeField] DecalProjector bulletDecal;
        [SerializeField] ParticleSystem explosionPS;
        [SerializeField] AudioSource aSrc;
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] AudioClip greandeTickSound;
        [SerializeField] List<AssetReference> explosionSounds, debrisSounds, explosionBass, bounceSounds;

        Transform _MainCamera;
        Transform MainCamera
        {
            get
            {
                if (_MainCamera == null) _MainCamera = Camera.main.transform;
                return _MainCamera;
            }
        }

        bool canTravel, canShowDecal, explodeOnHit, canExplode, isServer, canTick, hasExplosionTimer, isBouncingExplosive;
        int bouncesToExplode, currentBounces;
        float speed, timeToExplode, radius, grenadeTickTime, maxTimeToExplode;
        Vector3 startPosition;
        Vector3 destination;
        Rigidbody rb;

        AsyncOperationHandle<IList<AudioClip>> normalExplosionSounds, underwaterExplosionSound, explosionDebris, explosionBassHandle, bounceSoundsHandle;
        //AsyncOperationHandle<AudioClip> grenadeTickSoundHandle;

        public System.Action<List<HitBox>, Vector3, Quaternion> OnExplode;
        public System.Action<HitBox> OnTouch;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void OnEnable() => AudioManager.INS.RegisterAudioSource(aSrc, AudioManager.AudioSourceTarget.Other);
        void OnDisable() => AudioManager.INS.UnRegisterAudioSource(aSrc, AudioManager.AudioSourceTarget.Other);

        void LoadAssets()
        {
            if (explosionSounds != null && explosionSounds.Count > 0)
            {
                normalExplosionSounds = Addressables.LoadAssetsAsync<AudioClip>(explosionSounds, null, Addressables.MergeMode.Union);
                normalExplosionSounds.Completed += OnSoundsLoaded;
            }

            if (debrisSounds != null && debrisSounds.Count > 0)
            {
                explosionDebris = Addressables.LoadAssetsAsync<AudioClip>(debrisSounds, null, Addressables.MergeMode.Union);
                explosionDebris.Completed += OnSoundsLoaded;
            }

            if (explosionBass != null && explosionBass.Count > 0)
            {
                explosionBassHandle = Addressables.LoadAssetsAsync<AudioClip>(explosionBass, null, Addressables.MergeMode.Union);
                explosionBassHandle.Completed += OnSoundsLoaded;
            }

            if (bounceSounds != null && bounceSounds.Count > 0)
            {
                bounceSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(bounceSounds, null, Addressables.MergeMode.Union);
                bounceSoundsHandle.Completed += OnSoundsLoaded;
            }

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
            if (alwaysFaceCamera) meshRenderer.transform.LookAt(MainCamera);

            if (canExplode && hasExplosionTimer)
            {
                if (Time.time < timeToExplode)
                {
                    if (canTick && Time.time >= grenadeTickTime)
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

        public void PhysicsTravelTo(bool isServer, Vector3 direction, ForceMode physForceMode, bool hasTorque, bool _canExplode, float explosionRadious, bool explodeOnTouch, float timeToExplode = -1, int maxBouncesToExplode = -1)
        {
            rb.AddForce(direction * speed, physForceMode);
            if (hasTorque) rb.AddTorque(direction * (speed / 2), physForceMode);
            explodeOnHit = explodeOnTouch;

            if (timeToExplode != -1)
            {
                this.timeToExplode = Time.time + timeToExplode;
                maxTimeToExplode = timeToExplode;
                hasExplosionTimer = true;
            }

            this.isServer = isServer;

            if (maxBouncesToExplode != -1)
            {
                bouncesToExplode = maxBouncesToExplode;
                isBouncingExplosive = true;
            }

            radius = explosionRadious;

            if (explodeOnHit)
            {
                canExplode = false;
                bool _exp = _canExplode;
                LeanTween.delayedCall(.05f, () => canExplode = _exp);
            }
            else
            {
                canExplode = _canExplode;
            }

            LoadAssets();

            if (greandeTickSound != null)
            {
                aSrc.PlayOneShot(greandeTickSound);
                grenadeTickTime = Time.time + 1;
                canTick = true;
            }

            //float remainingTime = (this.timeToExplode - Time.time) / maxTimeToExplode;
            //Debug.LogFormat("{0} - {1} - {2} - {3}", (this.timeToExplode - Time.time), remainingTime, Mathf.Pow(remainingTime, 4), grenadeTickTime);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (bounceSoundsHandle.IsValid() && bounceSoundsHandle.Result != null) 
                aSrc.PlayOneShot(bounceSoundsHandle.Result[Random.Range(0, bounceSoundsHandle.Result.Count)]);

            if (!canExplode) return;

            HitBox charHitBox;
            if (isBouncingExplosive)
            {
                if (isServer)
                {
                    charHitBox = collision.transform.GetComponent<HitBox>();
                    if (charHitBox != null) OnTouch?.Invoke(charHitBox);
                }

                currentBounces++;
                if (currentBounces > bouncesToExplode) Explode();
                return;
            }

            if (explodeOnHit) Explode();
        }

        void OnCollisionStay(Collision collision) 
        {
            if (!canExplode) return;
            if (explodeOnHit) Explode();
        }

        void Explode()
        {
            if (!canExplode) return;

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
                
                if (normalExplosionSounds.IsValid()) aSrc.PlayOneShot(normalExplosionSounds.Result[Random.Range(0, normalExplosionSounds.Result.Count)]);
                if (explosionBassHandle.IsValid()) aSrc.PlayOneShot(explosionBassHandle.Result[Random.Range(0, explosionBassHandle.Result.Count)]);
                if (explosionDebris.IsValid()) LeanTween.delayedCall(.2f, () => aSrc.PlayOneShot(explosionDebris.Result[Random.Range(0, explosionDebris.Result.Count)]));
                //GameObject areaDebug = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                //areaDebug.layer = LayerMask.NameToLayer("Debug");
                //areaDebug.transform.position = MyTransform.position;
                //areaDebug.transform.localScale = Vector3.one * radius * 2;
                //Debug.Break();

                explosionPS.Play();
                explosionPS.transform.parent = null;
            }

            canExplode = false;
            hasExplosionTimer = false;
            meshRenderer.enabled = false;
            rb.isKinematic = true;

            StartCoroutine(WaitForSound(() => Destroy(gameObject)));
        }

        IEnumerator WaitForSound(System.Action endAction)
        {
            yield return new WaitUntil(() => !aSrc.isPlaying);
            endAction?.Invoke();
        }
    }
}
