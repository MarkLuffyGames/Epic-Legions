using UnityEngine;
using UnityEngine.UI;

public class EnlargedCardHolder : MonoBehaviour
{
    [SerializeField] private CardUI cardUI;
    [SerializeField] private GameObject[] menuCardObjects;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        closeButton.onClick.AddListener(HideCard);
    }

    private void Start()
    {
        HideCard();
    }
    public void ShowCard(CardSO card)
    {
        cardUI.SetCard(card);
        ShowMenuCard();
    }

    private void ShowMenuCard()
    {
        foreach (var obj in menuCardObjects)
        {
            obj.SetActive(true);
        }
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
}
