using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeckUI : MonoBehaviour
{
    public Deck deck;
    [SerializeField] private TextMeshProUGUI deckNameText;
    [SerializeField] private Button deckButton;

    private void Start()
    {
        deckButton.onClick.AddListener(() =>
        {
            DeckBuilder.Instance.SelectDeck(deck, this);
        });
    }

    public void SetDeck(Deck deck)
    {
        this.deck = deck;
        deckNameText.text = deck.deckName;
    }
}
