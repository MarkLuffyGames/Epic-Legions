using Microsoft.Unity.VisualStudio.Editor;
using UnityEditor;
using UnityEngine;

public enum CardElement { None, Fire, Water, Plant, Earth, Lightning, Wind, Light, Darkness }

public enum CardType { Heroes, Spells, Artifacts, Field }
public class CardSO : ScriptableObject
{
    [SerializeField] private int cardId;
    [SerializeField] public string cardName;
    [SerializeField] private string cardLastName;
    [SerializeField] public Sprite cardSprite;
    [SerializeField] private CardElement cardElemnt;

    public int CardID => cardId;
    public string CardName => cardName;
    public string CardLastName => cardLastName;
    public Sprite CardSprite => cardSprite;
    public CardElement CardElemnt => cardElemnt;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cardId == 0)
        {
            var generator = AssetDatabase.LoadAssetAtPath<IDGenerator>("Assets/Scripts/ScriptableObjects/IDGenerator/IDGenerator.asset");
            if (generator != null)
            {
                cardId = generator.ID();
                EditorUtility.SetDirty(this);
                Debug.Log($"Card ID set to {cardId} for {cardName} {cardLastName}");
            }
            else
            {
                Debug.LogWarning("IDGenerator not found. Please create an IDGenerator asset at Assets/Scripts/ScriptableObjects/IdGenerator.asset");
            }
        }
    }
#endif
}
