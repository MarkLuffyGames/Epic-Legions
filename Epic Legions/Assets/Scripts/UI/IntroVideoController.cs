using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class IntroVideoController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource; // opcional
    [SerializeField] private CanvasGroup avisoGroup;
    [SerializeField] private CanvasGroup blackScreenGroup;
    [SerializeField] private TextMeshProUGUI avisoText;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private VideoClip videoClipIntro;

    [Header("Texto del aviso")]
    [SerializeField] private string textoAviso = "Pulsa de nuevo para omitir";
    [SerializeField] private float avisoFadeSeconds = 0.15f;
    [SerializeField] private bool ocultarAvisoAutomatico = true;
    [SerializeField] private float segundosAvisoVisible = 2.5f;

    [Header("Transición")]
    [SerializeField] private float fadeOutSeconds = 0.2f; // 0 = sin fade

    [Header("Siguiente escena")]
    [SerializeField] private string nextSceneByName = ""; // deja vacío para usar índice
    [SerializeField] private int nextSceneBuildIndex = -1; // -1 = escena actual + 1

    private bool skipArmado = false;
    private float avisoOcultaEn = -1f;

    private void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (avisoGroup != null)
        {
            avisoGroup.alpha = 0f;
            avisoGroup.gameObject.SetActive(false);
        }
        if (avisoText != null) avisoText.text = "";
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached += OnVideoTerminado;
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= OnVideoTerminado;
    }

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[IntroVideo] Falta VideoPlayer.");
            StartCoroutine(CargarSiguienteEscena());
            return;
        }

        videoPlayer.clip = videoClipIntro;

        // Arranca la reproducción
        videoPlayer.Play();
        StartCoroutine(FadeCanvasGroup(blackScreenGroup, blackScreenGroup.alpha, 0f, avisoFadeSeconds));
    }

    private void Update()
    {
        // Entrada de clic/Toque
        if (DetectoClickOTouch())
        {
            if (!skipArmado)
            {
                // Primer toque/clic: mostrar aviso y armar skip
                skipArmado = true;
                MostrarAviso();
            }
            else
            {
                // Segundo toque/clic: saltar
                Skip();
            }
        }

        // Ocultar aviso automáticamente si corresponde
        if (ocultarAvisoAutomatico && skipArmado && avisoOcultaEn > 0f && Time.time >= avisoOcultaEn)
        {
            OcultarAviso();
            // Podemos mantener skipArmado o desarmarlo; aquí lo desarmamos para evitar saltos accidentales.
            skipArmado = false;
        }
    }

    private bool DetectoClickOTouch()
    {
        if(videoPlayer.clip == videoClipIntro)
        {
            // Durante el video intro no se permite saltar
            return false;
        }
        if (((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) 
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)))
        {
            return true;
        }

        return false;
    }

    private void MostrarAviso()
    {
        if (avisoText != null) avisoText.text = textoAviso;
        if (avisoGroup != null)
        {
            avisoGroup.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(avisoGroup, avisoGroup.alpha, 1f, avisoFadeSeconds));
        }
        if (ocultarAvisoAutomatico)
            avisoOcultaEn = Time.time + Mathf.Max(0.25f, segundosAvisoVisible);
        else
            avisoOcultaEn = -1f;
    }

    private void OcultarAviso()
    {
        if (avisoGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeOutAndDisable(avisoGroup, avisoFadeSeconds));
        }
    }

    private void Skip()
    {
        // Detener y pasar de escena (con pequeño fade opcional)
        if (fadeOutSeconds > 0f)
        {
            StartCoroutine(FadeOutYSalir());
        }
        else
        {
            StartCoroutine(CargarSiguienteEscena());
        }
    }

    private void OnVideoTerminado(VideoPlayer vp)
    {
        if(videoPlayer.clip == videoClipIntro)
        {
            // Reproducir el video principal
            videoPlayer.clip = videoClip;
            videoPlayer.Play();
            return;
        }
        else
        {
            // Video principal terminado: pasar de escena
            StartCoroutine(CargarSiguienteEscena());
        }
    }

    private IEnumerator CargarSiguienteEscena()
    {
        yield return FadeCanvasGroup(blackScreenGroup, blackScreenGroup.alpha, 1f, avisoFadeSeconds);
        // Elegir siguiente escena
        if (!string.IsNullOrEmpty(nextSceneByName))
        {
            SceneManager.LoadScene(nextSceneByName);
            yield break;
        }

        if (nextSceneBuildIndex >= 0)
        {
            SceneManager.LoadScene(nextSceneBuildIndex);
            yield break;
        }

        // Por defecto: escena actual + 1
        int idx = SceneManager.GetActiveScene().buildIndex + 1;
        if (idx < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(idx);
        else
            Debug.LogWarning("[IntroVideo] No hay siguiente escena en Build Settings.");
    }

    // ===== Utilidades de fade =====
    private System.Collections.IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / seconds);
            yield return null;
        }
        cg.alpha = to;
    }

    private System.Collections.IEnumerator FadeOutAndDisable(CanvasGroup cg, float seconds)
    {
        float start = cg.alpha;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, 0f, t / seconds);
            yield return null;
        }
        cg.alpha = 0f;
        cg.gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator FadeOutYSalir()
    {
        // Pequeño fade de audio (si existe)
        float t = 0f;
        float startVol = 1f;
        if (audioSource != null)
        {
            startVol = audioSource.volume;
        }

        while (t < fadeOutSeconds)
        {
            t += Time.deltaTime;
            float k = 1f - (t / fadeOutSeconds);
            if (audioSource != null) audioSource.volume = startVol * k;
            yield return null;
        }

        StartCoroutine(CargarSiguienteEscena());
    }
}
