using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectsActivatedUI : MonoBehaviour
{

    [SerializeField] private GameObject cardEffectActivatedPrefab;
    [SerializeField] private GameObject cardEffectActivatedUI;
    [SerializeField] private RectTransform content;
    [SerializeField] private List<GameObject> effectsActivated;

    private void Start()
    {
        HideEffectsActivated();
    }
    public void ShowEffectsActivated(List<Effect> cardEffectList)
    {
        if(cardEffectList == null || cardEffectList.Count == 0) return;
        cardEffectActivatedUI.SetActive(true);

        Vector2 size = content.sizeDelta;
        size.y = cardEffectList.Count * 100;
        content.sizeDelta = size;

        foreach (Effect cardEffect in cardEffectList)
        {
            GameObject cardEffectActivated = Instantiate(cardEffectActivatedPrefab, content);
            cardEffectActivated.GetComponent<CardEffectActivated>().SetCardEffect(cardEffect);
            effectsActivated.Add(cardEffectActivated);
        }
    }

    public void HideEffectsActivated()
    {
        cardEffectActivatedUI.SetActive(false);

        for (int i = effectsActivated.Count - 1; i >= 0; i--)
        {
            Destroy(effectsActivated[i]);
            effectsActivated.RemoveAt(i);
        }
    }
}
