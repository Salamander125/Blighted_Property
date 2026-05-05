using UnityEngine;
using System.Collections;

public class Ability_Controller : MonoBehaviour
{
    [Header("Sonido habilidad")]
    public AudioClip soundEffect;
    public AudioClip secondSoundEffect;
    public GameObject audioSource;
    private AudioSource audio;

    public void Awake()
    {
        audioSource = GameObject.FindWithTag("Audio");

        if (audioSource != null && audioSource.TryGetComponent(out audio))
        {
            audio.PlayOneShot(soundEffect);
        }
        else
        {
            Debug.LogWarning("No se encontró AudioSource en el objeto con tag 'Audio'");
        }
    }
    public void FinalizarAnimacion()
    {
        Destroy(gameObject);
    }

    public void PlaySecondSound()
    {
        audio.PlayOneShot(secondSoundEffect);
    }
}
