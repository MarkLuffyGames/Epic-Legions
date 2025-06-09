using System.Collections.Generic;
using UnityEngine;

public class Graveyard : MonoBehaviour
{
    private List<Card> cards = new List<Card>();

    public void AddCard(Card card)
    {
        cards.Insert(0, card);
    }

    public List<Card> GetCards()
    {
        return cards;
    }
    public int GetCardIndex(Card card)
    {
        return cards.IndexOf(card);
    }
    public Card GetCard(int index)
    {
        if (index < 0 || index >= cards.Count)
        {
            return null; // or throw an exception
        }
        return cards[index];
    }
}
