using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CollectionMenuUI : MonoBehaviour
{
    enum CardCategory { Heroes, Equipment, Spells }
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardsParent;
    [SerializeField] private Button heroesButton;
    [SerializeField] private Button equipmentButton;
    [SerializeField] private Button spellsButton;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Image sortOrderImage;
    [SerializeField] private TMP_InputField searchInputField;
    [SerializeField] private Image elementFilterImage;
    [SerializeField] private Image classFilterImage;


    private List<GameObject> cards = new List<GameObject>();
    private bool descendingOrder;
    private CardCategory category;
    private CardElement elementFilter = CardElement.None;
    private HeroClass classFilter = HeroClass.None;
    private string searchTerm = "";
    private void Start()
    {
        SetHeroes();
    }

    public void SetHeroes()
    {
        category = CardCategory.Heroes;
        ClearCards();

        foreach (var cardSO in descendingOrder ? CardDatabase.Instance.AllCards.OrderByDescending(card => card.CardName) :
            CardDatabase.Instance.AllCards.OrderBy(card => card.CardName))
        {
            if (cardSO is HeroCardSO hero)
            {
                if ((searchTerm == "" || hero.CardName.ToLower().Contains(searchTerm))
                    && (elementFilter == CardElement.None || elementFilter == hero.CardElemnt)
                    && (classFilter == HeroClass.None || classFilter == hero.HeroClass))
                {
                    var card = Instantiate(cardPrefab, cardsParent);
                    card.GetComponent<CardUI>().SetCard(cardSO);
                    cards.Add(card);
                }
            }
        }
    }

    public void SetEquipment()
    {
        category = CardCategory.Equipment;
        ClearCards();
        foreach (CardSO cardSO in descendingOrder ? CardDatabase.Instance.AllCards.OrderByDescending(card => card.CardName) :
            CardDatabase.Instance.AllCards.OrderBy(card => card.CardName))
        {
            if (cardSO is EquipmentCardSO equip)
            {
                if (searchTerm == "" || equip.CardName.Contains(searchTerm)
                    && (elementFilter == CardElement.None || elementFilter == equip.CardElemnt)
                    && (classFilter == HeroClass.None || equip.SupportedClasses.Contains(classFilter)))
                {
                    var card = Instantiate(cardPrefab, cardsParent);
                    card.GetComponent<CardUI>().SetCard(cardSO);
                    cards.Add(card);
                }
            }
        }
    }

    public void SetSpells()
    {
        category = CardCategory.Spells;
        ClearCards();
        foreach (CardSO cardSO in descendingOrder ? CardDatabase.Instance.AllCards.OrderByDescending(card => card.CardName) :
            CardDatabase.Instance.AllCards.OrderBy(card => card.CardName))
        {
            if (cardSO is SpellCardSO spell)
            {
                if (searchTerm == "" || spell.CardName.Contains(searchTerm)
                    && (elementFilter == CardElement.None || elementFilter == spell.CardElemnt))
                {
                    var card = Instantiate(cardPrefab, cardsParent);
                    card.GetComponent<CardUI>().SetCard(cardSO);
                    cards.Add(card);
                }
            }
        }
    }

    public void ClearCards()
    {
        foreach (var card in cards)
        {
            Destroy(card);
        }
        cards.Clear();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    public void ToggleSortOrder()
    {
        descendingOrder = !descendingOrder;
        sortOrderImage.transform.localRotation = Quaternion.Euler(0, 0, descendingOrder ? -90 : 90);
        UpdateCard();
    }

    public void SetElementFilter()
    {
        int element = (int)elementFilter + 1 > 8 ? 0 : (int)elementFilter + 1;
        elementFilter = (CardElement)element;
        elementFilterImage.sprite = CardDatabase.GetElementIcon(elementFilter);
        UpdateCard();
    }

    public void SetClassFilter()
    {
        int cardClass = (int)classFilter + 1 > 9 ? 0 : (int)classFilter + 1;
        classFilter = (HeroClass)cardClass;
        classFilterImage.sprite = CardDatabase.GetClassIcon(classFilter);
        UpdateCard();
    }

    public void SearchCard()
    {
        searchTerm = searchInputField.text;
        UpdateCard();
    }

    private void UpdateCard()
    {
        switch (category)
        {
            case CardCategory.Heroes:
                SetHeroes();
                break;
            case CardCategory.Equipment:
                SetEquipment();
                break;
            case CardCategory.Spells:
                SetSpells();
                break;
            default:
                break;
        }
    }

    public void BackToMenu()
    {
        Loader.LoadScene("MainMenu", false);
    }
}
