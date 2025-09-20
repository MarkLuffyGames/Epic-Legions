using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Deck", menuName = "Hemera Legions/Decks/New Deck")]
public class DeckSO : ScriptableObject
{
    public string deckName;
    public List<int> cardsIds = new List<int>();
}
