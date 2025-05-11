using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    private AudioSource audioSource;

    [SerializeField] private AudioClip cardDrawSound;
    [SerializeField] private AudioClip cardPlacingSound;
    [SerializeField] private AudioClip cardDestroySound;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if(Instance == null)
        {
            Instance = this;
        }

    }

    public void PlayCardDraw()
    {
        audioSource.clip = cardDrawSound;
        audioSource.Play();
    }

    public void PlayCardPlacing()
    {
        audioSource.clip = cardPlacingSound;
        audioSource.Play();
    }

    public void PlayCardDestroy()
    {
        audioSource.clip = cardDestroySound;
        audioSource.Play();
    }
}
