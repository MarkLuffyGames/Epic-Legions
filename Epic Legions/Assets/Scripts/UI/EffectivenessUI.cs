using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EffectivenessUI : MonoBehaviour
{
    [SerializeField] private Color highEffectivenessColor;
    [SerializeField] private Color lowEffectivenessColor;

    [SerializeField] private float fadeDuration = 0.5f;

    private RawImage _image;
    private TextMeshProUGUI _text;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        _image = GetComponent<RawImage>();
        _text = GetComponentInChildren<TextMeshProUGUI>();
        SetStartValue(true);
    }

    public void Activate(bool isHighEffectiveness)
    {
        SetStartValue(isHighEffectiveness);

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeAlpha(1f));
    }

    private void SetStartValue(bool isHighEffectiveness)
    {
        _image.color = isHighEffectiveness ? highEffectivenessColor : lowEffectivenessColor;
        _text.text = isHighEffectiveness ? "Debil" : "Resistente"; 
        _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, 0);
        _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, 0);
    }
    private IEnumerator FadeAlpha(float target)
    {
        float start = target == 1f ? 0f : 1f;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            float newAlpha = Mathf.Lerp(start, target, t);
            _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, newAlpha);
            _text.color = new Color(_text.color.r, _text.color.g, _text.color.b, newAlpha);
            yield return null;
        }

        _image.color = new Color(_image.color.r, _image.color.g, _image.color.b, target);

        yield return new WaitForSeconds(0.5f);

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeAlpha(target == 1 ? 0f : 1f));
    }

    public void Deactivate()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        SetStartValue(true);
    }
}
