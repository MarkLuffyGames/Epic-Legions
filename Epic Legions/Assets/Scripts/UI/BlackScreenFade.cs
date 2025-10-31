using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlackScreenFade : MonoBehaviour
{
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private float seconds = 1f;
    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator FadeOutAndDisable()
    {
        cg.alpha = 1f;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t / seconds);
            yield return null;
        }
        cg.alpha = 0f;
        cg.gameObject.SetActive(false);
    }
    public IEnumerator FadeCanvasGroup()
    {
        cg.gameObject.SetActive(true);
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(cg.alpha, 1, t / seconds);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

}
