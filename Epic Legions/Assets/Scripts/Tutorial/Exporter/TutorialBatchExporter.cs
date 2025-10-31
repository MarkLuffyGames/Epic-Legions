using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public struct Position2D
{
    public float x;
    public float y;
}

[Serializable]
public class TutorialEntry
{
    public int id;
    public string text;
    public Position2D position;
    public int textWidth;
    public int textHeight;
    public int bgWidth;
    public int bgHeight;
}

[Serializable]
public class TutorialEntryList
{
    public List<TutorialEntry> items = new List<TutorialEntry>();
}

/// <summary>
/// Asigna en 'targets' tus GameObjects padre (cada uno con sus 2 hijos: Image y TMP).
/// Opcionalmente asigna el Canvas para convertir posiciones de mundo a coords de UI si el padre no es UI.
/// </summary>
public class TutorialBatchExporter : MonoBehaviour
{
    [Header("Canvas (opcional pero recomendado si hay UI)")]
    public Canvas canvas;

    [Serializable]
    public struct Target
    {
        public int id;
        public GameObject root; // GameObject PADRE que fija la posición
    }

    [Header("Objetos del tutorial (padres)")]
    public List<GameObject> targets = new List<GameObject>();

    [Header("Salida")]
    public string fileName = "tutorial.json";
    [Tooltip("Si está activo, guarda en Application.persistentDataPath; si no, en Application.dataPath.")]
    public bool usePersistentDataPath = true;

    [ContextMenu("Exportar JSON")]
    public void ExportarJSON()
    {
        // Asegurar tamaños actualizados
        Canvas.ForceUpdateCanvases();

        var dataset = new TutorialEntryList();

        RectTransform canvasRect = canvas ? canvas.transform as RectTransform : null;
        Camera cam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        foreach (var t in targets)
        {
            if (t == null)
                continue;

            // 1) Posición: preferimos RectTransform. Si no hay, convertimos posición de mundo a coords locales del Canvas.
            Vector2 pos = Vector2.zero;
            var rootRT = t.GetComponent<RectTransform>();

            if (rootRT != null)
            {
                // UI: usamos anchoredPosition (lo más típico para HUD/tutorial)
                pos = rootRT.anchoredPosition;
            }
            else if (canvasRect != null)
            {
                // Objeto de mundo → lo proyectamos al Canvas
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, t.transform.position);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, cam, out Vector2 local))
                    pos = local;
            }
            else
            {
                // Sin Canvas: última opción, coordenadas de mundo (no UI)
                pos = (Vector2)t.transform.position;
            }

            // 2) Buscar HIJOS inmediatos: uno con Image (bg) y otro con TMP_Text (texto)
            RectTransform bgRect = null;
            RectTransform textRect = null;
            TMP_Text tmp = null;

            for (int i = 0; i < t.transform.childCount; i++)
            {
                var child = t.transform.GetChild(i);

                // Fondo (Image)
                if (bgRect == null)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null)
                        bgRect = child as RectTransform;
                }

                // Texto (TMP)
                if (textRect == null)
                {
                    tmp = child.GetComponent<TMP_Text>();
                    if (tmp != null)
                        textRect = child as RectTransform;
                }
            }

            // 3) Medidas y contenido
            int textW = 0, textH = 0, bgW = 0, bgH = 0;
            string textStr = tmp ? tmp.text : "";

            if (textRect != null)
            {
                textW = Mathf.RoundToInt(textRect.rect.width);
                textH = Mathf.RoundToInt(textRect.rect.height);
            }

            if (bgRect != null)
            {
                bgW = Mathf.RoundToInt(bgRect.rect.width);
                bgH = Mathf.RoundToInt(bgRect.rect.height);
            }

            // 4) Crear entrada
            var entry = new TutorialEntry
            {
                id = targets.IndexOf(t),
                text = textStr,
                position = new Position2D { x = Mathf.Round(pos.x), y = Mathf.Round(pos.y) },
                textWidth = textW,
                textHeight = textH,
                bgWidth = bgW,
                bgHeight = bgH
            };

            dataset.items.Add(entry);
        }

        // 5) Serializar y guardar (JsonUtility requiere wrapper)
        string json = JsonUtility.ToJson(dataset, true);

        string basePath = usePersistentDataPath ? Application.persistentDataPath : Application.dataPath;
        string path = Path.Combine(basePath, fileName);

        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"[TutorialBatchExporter] Exportadas {dataset.items.Count} entradas a: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TutorialBatchExporter] Error al escribir el JSON: {e.Message}");
        }
    }
}
