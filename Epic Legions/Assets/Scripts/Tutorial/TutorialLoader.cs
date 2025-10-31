using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class TutorialTextData
{
    public int id;
    public string text;
    public Vector2 position;
    public float textWidth;
    public float bgWidth;
    public float bgHeight;
}
public class TutorialLoader : MonoBehaviour
{
    public TutorialTextData[] tutorialTexts;

    private void Awake()
    {
        LoadTutorialData();
    }

    private void LoadTutorialData()
    {
        TextAsset tutorialData = Resources.Load<TextAsset>("Localization/tutorial_texts_es");
        if (tutorialData != null)
        {
            tutorialTexts = JsonHelper.FromJson<TutorialTextData>(tutorialData.text);
        }
        else
        {
            Debug.LogError("Tutorial data file not found!");
        }
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string newJson = "{ \"items\": " + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.items;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }
    }
}
