using System.Collections;
using UnityEngine;

public enum HLCursorStyle { Default, Text, Hand, Busy }

public class CursorThemeHL : MonoBehaviour
{
    public static CursorThemeHL Instance { get; private set; }

    [Header("Texturas 64px (recomendado)")]
    public Texture2D defaultCursor;
    public Vector2 defaultHotspot = new Vector2(8, 8);

    public Texture2D textCursor;
    public Vector2 textHotspot = new Vector2(32, 32);

    public Texture2D handCursor;
    public Vector2 handHotspot = new Vector2(24, 22);

    [Header("Busy (animado)")]
    public Texture2D[] busyFrames;   // 12 frames: Cursor_Busy_64_00..11
    public Vector2 busyHotspot = new Vector2(8, 8);
    [Range(4, 24)] public float busyFPS = 12f;

    private Coroutine busyRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Apply(HLCursorStyle.Default);
    }

    public void Apply(HLCursorStyle style)
    {
        StopBusy();

        switch (style)
        {
            case HLCursorStyle.Default:
                Cursor.SetCursor(defaultCursor, defaultHotspot, CursorMode.Auto);
                break;
            case HLCursorStyle.Text:
                Cursor.SetCursor(textCursor, textHotspot, CursorMode.Auto);
                break;
            case HLCursorStyle.Hand:
                Cursor.SetCursor(handCursor, handHotspot, CursorMode.Auto);
                break;
            case HLCursorStyle.Busy:
                if (busyFrames != null && busyFrames.Length > 0)
                    busyRoutine = StartCoroutine(AnimateBusy());
                else
                    Cursor.SetCursor(defaultCursor, defaultHotspot, CursorMode.Auto);
                break;
        }
    }

    public void StopBusy()
    {
        if (busyRoutine != null) { StopCoroutine(busyRoutine); busyRoutine = null; }
    }

    private IEnumerator AnimateBusy()
    {
        int i = 0;
        float delay = 1f / Mathf.Max(1f, busyFPS);
        while (true)
        {
            Cursor.SetCursor(busyFrames[i], busyHotspot, CursorMode.Auto);
            i = (i + 1) % busyFrames.Length;
            yield return new WaitForSeconds(delay);
        }
    }
}
