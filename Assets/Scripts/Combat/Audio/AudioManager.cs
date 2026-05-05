using UnityEngine;

public class AudioManager : MonoBehaviour
{
    static public AudioManager instance;

    public AudioClip ataqueBasico;
    public AudioClip Oro;
    public AudioClip avanzarPiso;
    public AudioSource audioSource;

    public void Awake()
    {
        if(instance  == null)
            instance = this;
    }

    public void PlayAudioAttack()
    {
        audioSource.PlayOneShot(ataqueBasico);
    }
    public void PlayAudioGold()
    {
        audioSource.PlayOneShot(Oro);
    }
    public void PlayAudioFloor()
    {
        audioSource.PlayOneShot(avanzarPiso);
    }
}
