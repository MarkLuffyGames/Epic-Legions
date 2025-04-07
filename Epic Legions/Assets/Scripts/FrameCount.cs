using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class FrameCount : MonoBehaviour
{
    public int frameCount;
    public int frameRate;
    public TextMeshProUGUI FPSText;

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
}
