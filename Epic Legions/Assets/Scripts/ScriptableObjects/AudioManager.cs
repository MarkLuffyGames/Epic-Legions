using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    private AudioSource audioSource;

    [SerializeField] private AudioClip cardDrawSound;
    [SerializeField] private AudioClip cardPlacingSound;
    [SerializeField] private AudioClip spellPlacingSound;
    [SerializeField] private AudioClip cardDestroySound;
    [SerializeField] private AudioClip phaseChangedSound;

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
        audioSource.volume = 0.2f;
        audioSource.clip = cardDrawSound;
        audioSource.Play();
    }

    public void PlayCardPlacing()
    {
        audioSource.volume = 0.2f;
        audioSource.clip = cardPlacingSound;
        audioSource.Play();
    }
    public void PlaySpellPlacing()
    {
        audioSource.volume = 1f;
        audioSource.clip = spellPlacingSound;
        audioSource.Play();
    }

    public void PlayCardDestroy()
    {
        audioSource.volume = 0.2f;
        audioSource.clip = cardDestroySound;
        audioSource.Play();
    }
    public void PlayPhaseChanged()
    {
        audioSource.volume = 0.2f;
        audioSource.clip = phaseChangedSound;
        audioSource.Play();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeCardDatabase()
    {
        Instance = null;
    }
}
