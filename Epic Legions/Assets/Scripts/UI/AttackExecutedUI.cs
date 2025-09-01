using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AttackExecutedUI : MonoBehaviour
{
    [SerializeField] private RawImage background;
    [SerializeField] private TextMeshProUGUI attackText;

    private Material backgroundMaterial;

    private Vector3 originalPosition;
    private float speed = 10;

    private Coroutine moveCoroutine;
    private float fadeDuration = 0.5f;
    private Coroutine fadeRoutine;
    private void Start()
    {
        var image = background.GetComponentInChildren<RawImage>();
        backgroundMaterial = new Material(image.material);
        image.material = backgroundMaterial;

        originalPosition = transform.localPosition;
        background.enabled = false;
        attackText.enabled = false;
    }
    public IEnumerator SetAttackText(string text, Vector3 cardposition)
    {
        transform.position = cardposition;
        transform.localScale = Vector3.zero;

        background.enabled = true;
        attackText.enabled = true;


        backgroundMaterial.SetFloat("_Alpha", 1f);
        attackText.color = new Color(attackText.color.r, attackText.color.g, attackText.color.b, 1f);

        attackText.text = text;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        StartCoroutine(Scale());
        yield return moveCoroutine = StartCoroutine(MoveSmoothly());

        yield return new WaitForSeconds(1f);

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        yield return fadeRoutine = StartCoroutine(FadeAlpha());
    }

    private IEnumerator MoveSmoothly()
    {
        while (Vector3.Distance(transform.localPosition, originalPosition) > 0.01f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPosition, speed * Time.deltaTime);

            yield return null;
        }

        transform.localPosition = originalPosition;
    }

    public IEnumerator Scale()
    {
        Vector3 initialScale = transform.localScale;
        while (Vector3.Distance(transform.localScale, Vector3.one) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, speed * Time.deltaTime);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private IEnumerator FadeAlpha()
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            float newAlpha = Mathf.Lerp(1, 0, t);
            backgroundMaterial.SetFloat("_Alpha", newAlpha);
            attackText.color = new Color(attackText.color.r, attackText.color.g, attackText.color.b, newAlpha); 
            yield return null;
        }

        backgroundMaterial.SetFloat("_Alpha", 0);
    }
}
