using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    public string deckName;
    public List<int> cardsIds = new List<int>();

    public Deck()
    {
        deckName = "New Deck";
        cardsIds = new List<int>();
    }

    public Deck(DeckSO deckSO)
    {
        deckName = deckSO.deckName;
        cardsIds = deckSO.cardsIds;
    }
}
