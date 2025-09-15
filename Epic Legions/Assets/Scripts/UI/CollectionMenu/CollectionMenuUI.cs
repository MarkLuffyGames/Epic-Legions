using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectionMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardsParent;
    [SerializeField] private Button heroesButton;
    [SerializeField] private Button equipmentButton;
    [SerializeField] private Button spellsButton;

    private List<GameObject> cards = new List<GameObject>();

    private void Start()
    {
        SetHeroes();
    }

    public void SetHeroes()
    {
        ClearCards();
        foreach (CardSO cardSO in CardDatabase.Instance.AllCards)
        {
            if (cardSO is not HeroCardSO) continue;

            var card = Instantiate(cardPrefab, cardsParent);
            card.GetComponent<CardUI>().SetCard(cardSO);
            cards.Add(card);
        }
    }

    public void SetEquipment()
    {
        ClearCards();
        foreach (CardSO cardSO in CardDatabase.Instance.AllCards)
        {
            if (cardSO is not EquipmentCardSO) continue;

            var card = Instantiate(cardPrefab, cardsParent);
            card.GetComponent<CardUI>().SetCard(cardSO);
            cards.Add(card);
        }
    }

    public void SetSpells()
    {
        ClearCards();
        foreach (CardSO cardSO in CardDatabase.Instance.AllCards)
        {
            if (cardSO is not SpellCardSO) continue;
            var card = Instantiate(cardPrefab, cardsParent);
            card.GetComponent<CardUI>().SetCard(cardSO);
            cards.Add(card);
        }
    }

    public void ClearCards()
    {
        foreach (var card in cards)
        {
            Destroy(card, 0.1f);
        }
        cards.Clear();
    }
}
