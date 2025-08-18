using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class FrameCount : MonoBehaviour
{
    public static FrameCount instance;
    public int frameCount;
    public int frameRate;
    public TextMeshProUGUI FPSText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        InvokeRepeating("FrameCounts", 1, 1);
    }

    void FrameCounts()
    {
        frameRate = Time.frameCount - frameCount;
        frameCount = Time.frameCount;
        FPSText.text = frameRate.ToString();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeCardDatabase()
    {
        instance = null;
    }
}
