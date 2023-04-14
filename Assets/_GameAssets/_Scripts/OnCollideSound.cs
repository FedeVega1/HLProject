using UnityEngine;

[RequireComponent(typeof(OnCollideSound))]
public class OnCollideSound : MonoBehaviour
{
    [SerializeField] AudioClip[] randomSoundsToPlay;

    AudioSource aSrc;

    void Awake() => aSrc = GetComponent<AudioSource>();

    void OnCollisionEnter(Collision collision)
    {
        aSrc.PlayOneShot(randomSoundsToPlay[Random.Range(0, randomSoundsToPlay.Length)]);
    }
}
