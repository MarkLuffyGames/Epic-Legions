using UnityEngine;

public enum CardElement { None, Fire, Water, Plant, Earth, Lightning, Wind, Light, Darkness }

public enum CardType { Heroes, Spells, Artifacts, Field }
public class CardSO : ScriptableObject
{
    [SerializeField] private int cardId;
    [SerializeField] private string cardName;
    [SerializeField] private string cardLastName;
    [SerializeField] private Sprite cardSprite;
    [SerializeField] private CardElement cardElemnt;

    public int CardID => cardId;
    public string CardName => cardName;
    public string CardLastName => cardLastName;
    public Sprite CardSprite => cardSprite;
    public CardElement CardElemnt => cardElemnt;
}
