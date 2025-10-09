using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    public Slider progressBar;
    public Animator imageAnimator;

    private void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    private IEnumerator LoadSceneAsync()
    {
        string sceneName = Loader.SceneToLoad;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("No target scene set to load.");
            yield break;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null)
                progressBar.value = progress;

            // Cuando llega al 90%, ya está lista para activarse
            if (operation.progress >= 0.9f)
            {
                yield return new WaitForSeconds(1f); // Delay opcional
                imageAnimator.SetTrigger("Activate");
                yield return new WaitForSeconds(0.6f); // Espera a que termine la animación
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
