using UnityEngine;

public class DestroyAfterSoundPlayed : MonoBehaviour 
{
    public AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!audioSource.isPlaying)
            Destroy(gameObject);
    }
}
