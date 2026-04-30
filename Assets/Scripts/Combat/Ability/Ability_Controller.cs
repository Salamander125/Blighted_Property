using UnityEngine;
using System.Collections;

public class Ability_Controller : MonoBehaviour
{
    [Header("Sonido habilidad")]
    public AudioClip soundEffect;
    public AudioSource audioSource;

    public void Awake()
    {
        audioSource = FindFirstObjectByType<AudioSource>();

        audioSource.PlayOneShot(soundEffect);
    }
    public void FinalizarAnimacion()
    {
        Destroy(gameObject);
    }
}
