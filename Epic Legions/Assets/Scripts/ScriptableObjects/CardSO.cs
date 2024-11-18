using UnityEngine;

public enum CardElement { Fire, Water, Plant, Earth, Lightning, Wind, Light, Darkness }

public enum CardType { Heroes, Spells, Artifacts, Field }
public class CardSO : ScriptableObject
{
    [SerializeField] private int cardId;
    [SerializeField] private string cardName;
    [SerializeField] private Sprite cardSprite;
    [SerializeField] private CardElement cardElemnt;
    [SerializeField] private CardType cardType;

    public int CardID => cardId;
    public string CardName => cardName;
    public Sprite CardSprite => cardSprite;
    public CardElement CardElemnt => cardElemnt;
    public CardType CardType => cardType;
}
