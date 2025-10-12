using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnlargedCardHolder : MonoBehaviour
{
    [SerializeField] private CardUI cardUI;
    [SerializeField] private GameObject[] menuCardObjects;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI cardCountText;

    private CardUI origCard;

    public CardUI CardUI => cardUI;
    private void Awake()
    {
        closeButton.onClick.AddListener(HideCard);
    }

    private void Start()
    {
        HideCard();
    }
    public void ShowCard(CardUI card)
    {
        origCard = card;
        cardUI.SetCard(card.CurrentCard);
        ShowMenuCard();
    }

    private void ShowMenuCard()
    {
        foreach (var obj in menuCardObjects)
        {
            obj.SetActive(true);
        }

        UpdateCardCount();
    }

    private void HideCard()
    {
        cardUI.ClearCard();
        foreach (var obj in menuCardObjects)
        {
            obj.SetActive(false);
        }
        DeckBuilder.Instance.ResizeCard();
    }

    public void UpdateCardCount()
    {
        if(DeckBuilder.Instance.currentState == DeckBuilderState.CreatingDeck 
            || DeckBuilder.Instance.currentState == DeckBuilderState.EditingDeck)
        {
            cardCountText.text = $"{DeckBuilder.Instance.GetCardCountInDeck(origCard.CurrentCard)}/{(origCard.CurrentCard is SpellCardSO spell ? 4 : 1)}";
        }
        else
        {
            cardCountText.text = "1";
        }
    }
}
