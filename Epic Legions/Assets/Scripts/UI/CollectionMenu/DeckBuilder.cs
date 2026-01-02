using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public enum DeckBuilderState
{
    ViewingCollection,
    SelectingDeck,
    EditingDeckView,
    CreatingDeck,
    EditingDeck,
    DeletingDeck
}
public class DeckBuilder : MonoBehaviour
{
    public static DeckBuilder Instance;


    [SerializeField] private CollectionMenuUI collectionMenu;

    [SerializeField] private GameObject deckEditView;
    [SerializeField] private GameObject decksView;

    [SerializeField] private ScrollRect deckEditViewScrollRect;
    [SerializeField] private ScrollRect deckViewScrollRect;
    [SerializeField] private GameObject deckContent;
    [SerializeField] private GameObject cardContent;

    [SerializeField] private TMP_InputField deckName;
    private List<GameObject> cardsInDeck = new List<GameObject>();

    [SerializeField] private GameObject deckPrefab;
    [SerializeField] private GameObject cardPrefab;

    [SerializeField] private Button selectDeckButton;
    [SerializeField] private Button createDeckButton;
    [SerializeField] private Button editDeckButton;
    [SerializeField] private Button deleteDeckButton;
    [SerializeField] private Button cancelDeckCreationButton;
    [SerializeField] private Button ConfirmDeckCreationButton;
    [SerializeField] private Button HideDecksButton;
    [SerializeField] private Button setDeckNameButton;
    [SerializeField] private Button confirmDeckEditingButton;
    [SerializeField] private Button addCardButton;
    [SerializeField] private Button RemoveCardButton;
    [SerializeField] private Button nextCardButton;
    [SerializeField] private Button previousCardButton;

    [SerializeField] private TextMeshProUGUI menuName;
    [SerializeField] private TextMeshProUGUI cardCountText;
    [SerializeField] private TextMeshProUGUI heroCountText;
    [SerializeField] private TextMeshProUGUI equipmentCountText;
    [SerializeField] private TextMeshProUGUI spellCountText;

    public DeckBuilderState currentState = DeckBuilderState.ViewingCollection;

    private Deck selectedDeck;
    private Deck selectedDeckEdit;
    public Deck SelectedDeck => selectedDeck;

    public bool isEnlargedCard;
    int currentEnlargedCardIndex = -1;

    [SerializeField] private EnlargedCardHolder enlargedCardHolder;
    [SerializeField] private DeckDropZone deckDropZone;

    private Canvas canvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        canvas = GetComponent<Canvas>();
    }
    private void Start()
    {
        createDeckButton.onClick.AddListener(() =>
        {
            StartCoroutine(CreateDeck());
        });

        cancelDeckCreationButton.onClick.AddListener(() =>
        {
            StartCoroutine(CancelDeckCreation());
        });

        editDeckButton.onClick.AddListener(() =>
        {
            StartCoroutine(ShowDecksViewToEdit());
        });

        ConfirmDeckCreationButton.onClick.AddListener(() =>
        {
            StartCoroutine(ConfirmDeckCreation());
        });

        setDeckNameButton.onClick.AddListener(() =>
        {
            //TODO: Open Keyboard for input
            deckName.Select();
        });

        HideDecksButton.onClick.AddListener(() =>
        {
            StartCoroutine(HideDecks());
        });

        confirmDeckEditingButton.onClick.AddListener(() =>
        {
            StartCoroutine(ConfirmDeckEditing());
        });

        selectDeckButton.onClick.AddListener(() =>
        {
            StartCoroutine(ShowDecksViewToSelect());
        });

        deleteDeckButton.onClick.AddListener(() =>
        {
            StartCoroutine(ShowDecksViewToDelete());
        });

        addCardButton.onClick.AddListener(() =>
        {
            deckDropZone.AddCardToDeck(enlargedCardHolder.CardUI.gameObject, enlargedCardHolder.CardUI);
            enlargedCardHolder.UpdateCardCount();
        });

        RemoveCardButton.onClick.AddListener(() =>
        {
            var cardToRemove = cardsInDeck.FirstOrDefault(
                c => c.GetComponent<CardUI>().CurrentCard.CardID == enlargedCardHolder.CardUI.CurrentCard.CardID)?.GetComponent<CardUI>();

            if(cardToRemove) deckDropZone.RemoveCardFromDeck(cardToRemove);
            enlargedCardHolder.UpdateCardCount();
        });

        nextCardButton.onClick.AddListener(() =>
        {
            ChangeEnlargedCard(true);
        });
        previousCardButton.onClick.AddListener(() =>
        {
            ChangeEnlargedCard(false);
        });


        deckEditViewScrollRect = deckEditView.GetComponentInChildren<ScrollRect>();
        deckViewScrollRect = decksView.GetComponentInChildren<ScrollRect>();

        selectDeckButton.GetComponentInChildren<TextMeshProUGUI>().text = GameData.Instance.CurrentDeck.deckName;

        deckEditView.SetActive(false);
        decksView.SetActive(false);

        collectionMenu = FindFirstObjectByType<CollectionMenuUI>();
        collectionMenu.onChangeCollection += (s, e) =>
        {
            if (currentState == DeckBuilderState.CreatingDeck || currentState == DeckBuilderState.EditingDeck)
            {
                if (cardsInDeck.Count >= 40)
                    BlockAllCards();
                else
                    UnblockAllCards();
            }
        };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Instance = null;
    }

    IEnumerator CreateDeck()
    {
        if (currentState != DeckBuilderState.ViewingCollection)
            yield break;

        currentState = DeckBuilderState.CreatingDeck;
        deckName.text = "Nuevo Mazo";
        selectedDeckEdit = new Deck { deckName = "Nuevo Mazo", cardsIds = new List<int>() };
        StartCoroutine(UpdateUI());
        yield return MoveView(deckEditView, true);
        canvas.sortingOrder = 1001;
        
    }

    IEnumerator ConfirmDeckCreation()
    {
        if (currentState != DeckBuilderState.CreatingDeck)
            yield break;

        currentState = DeckBuilderState.ViewingCollection;
        blockedCards.Clear();
        UnblockAllCards();
        GameData.Instance.SaveNewDeck(deckName.text, GetCardIndicesInDeck());
        enlargedCardHolder.HideCard();
        yield return MoveView(deckEditView, false);
        canvas.sortingOrder = 0;
        ClearDeckCards();
    }

    private List<int> GetCardIndicesInDeck()
    {
        List<int> indices = new List<int>();
        foreach (var card in cardsInDeck)
        {
            var cardUI = card.GetComponent<CardUI>();
            if (cardUI != null && cardUI.CurrentCard != null)
            {
                indices.Add(cardUI.CurrentCard.CardID);
            }
        }
        return indices;
    }
    IEnumerator CancelDeckCreation()
    {
        if (currentState != DeckBuilderState.CreatingDeck && currentState != DeckBuilderState.EditingDeck)
            yield break;
        currentState = DeckBuilderState.ViewingCollection;
        blockedCards.Clear();
        UnblockAllCards();
        enlargedCardHolder.HideCard();
        yield return MoveView(deckEditView, false);
        canvas.sortingOrder = 0;
        ClearDeckCards();
    }

    public void AddCardToDeck(GameObject card)
    {
        cardsInDeck.Add(card);
        var cardUI = card.GetComponent<CardUI>();
        selectedDeckEdit.cardsIds.Add(cardUI.CurrentCard.CardID);

        if (cardsInDeck.Count >= 40)
            BlockAllCards();
        else
            BlockCard(cardUI);

        StartCoroutine(UpdateUI());
        StartCoroutine(ScrollDeckEditingView());
    }

    public void RemoveCardFromDeck(CardUI cardUI)
    {
        cardsInDeck.Remove(cardUI.gameObject);
        selectedDeckEdit.cardsIds.Remove(cardUI.CurrentCard.CardID);

        UnblockCard(cardUI);
        if (cardsInDeck.Count == 39)
            UnblockAllCards();
            
        if(cardsInDeck.Count == 0 
            && (enlargedCardHolder.OrigCard == null || enlargedCardHolder.OrigCard.cardDraggable.DeckDropZone.isDeck))
        {
            enlargedCardHolder.HideCard();
            StartCoroutine(UpdateUI());
            return;
        }
        if (enlargedCardHolder.OrigCard != null && enlargedCardHolder.OrigCard.cardDraggable.DeckDropZone.isDeck)
        {
            currentEnlargedCardIndex--;
            ChangeEnlargedCard(true);
        }

        StartCoroutine(UpdateUI());
    }

    IEnumerator ShowDecksViewToSelect()
    {
        if (currentState != DeckBuilderState.ViewingCollection)
            yield break;
        currentState = DeckBuilderState.SelectingDeck;
        blockedCards.Clear();
        UnblockAllCards();
        ShowDecks();
        menuName.text = "Seleccionar Mazo";
        yield return MoveView(decksView, true);
    }

    IEnumerator ShowDecksViewToEdit()
    {
        if (currentState != DeckBuilderState.ViewingCollection)
            yield break;
        currentState = DeckBuilderState.EditingDeckView;
        blockedCards.Clear();
        UnblockAllCards();
        ShowDecks();
        menuName.text = "Editar Mazo";
        yield return MoveView(decksView, true);
    }

    IEnumerator ShowDecksViewToDelete()
    {
        if (currentState != DeckBuilderState.ViewingCollection)
            yield break;
        currentState = DeckBuilderState.DeletingDeck;
        blockedCards.Clear();
        UnblockAllCards();
        ShowDecks();
        menuName.text = "Eliminar Mazo";
        yield return MoveView(decksView, true);
    }

    private void ShowDecks()
    {
        foreach (var deck in GameData.Instance.decks)
        {
            var deckGO = Instantiate(deckPrefab, deckContent.transform);
            deckGO.GetComponent<DeckUI>().SetDeck(deck);
        }
    }

    public void SelectDeck(Deck deck, DeckUI deckUI)
    {
        selectedDeck = deck;
        if (currentState == DeckBuilderState.EditingDeckView)
        {
            StartCoroutine(EditingDeck(deck));
        }
        else if (currentState == DeckBuilderState.SelectingDeck)
        {
            GameData.Instance.SetCurrentDeck(deck);
            selectDeckButton.GetComponentInChildren<TextMeshProUGUI>().text = deck.deckName;
            StartCoroutine(HideDecks());
        }
        else if (currentState == DeckBuilderState.DeletingDeck)
        {
            if (GameData.Instance.decks.Count == 1)
            {
                //TODO: Show message that at least one deck is required
                Debug.LogWarning("At least one deck is required");
                return;
            }

            GameData.Instance.DeleteDeck(deck);
            if (GameData.Instance.CurrentDeck == deck) GameData.Instance.SetCurrentDeck(GameData.Instance.decks[0]);
            selectDeckButton.GetComponentInChildren<TextMeshProUGUI>().text = GameData.Instance.decks[0].deckName;
            Destroy(deckUI.gameObject);

        }
    }

    IEnumerator EditingDeck(Deck deck)
    {
        if (currentState != DeckBuilderState.EditingDeckView) 
            yield break;

        StartCoroutine(HideDecks());
        currentState = DeckBuilderState.EditingDeck;

        selectedDeckEdit = new Deck { deckName = deck.deckName, cardsIds = new List<int>(deck.cardsIds) };

        foreach (var cardId in deck.cardsIds)
        {
            var cardGO = Instantiate(cardPrefab, cardContent.transform);
            var rect = cardGO.transform as RectTransform;
            rect.localScale = Vector3.one * 100;
            var cardUI = cardGO.GetComponent<CardUI>();
            cardUI.SetCard(CardDatabase.GetCardById(cardId));
            cardsInDeck.Add(cardGO);
            BlockCard(cardUI);
        }
        if(cardsInDeck.Count >= 40)
            BlockAllCards();

        deckName.text = deck.deckName;
        StartCoroutine(UpdateUI());
        yield return MoveView(deckEditView, true);
        canvas.sortingOrder = 1001;
    }

    IEnumerator ConfirmDeckEditing()
    {
        if (currentState != DeckBuilderState.EditingDeck)
            yield break;

        currentState = DeckBuilderState.ViewingCollection;
        GameData.Instance.UpdateDeck(selectedDeck, deckName.text, GetCardIndicesInDeck());
        enlargedCardHolder.HideCard();
        yield return MoveView(deckEditView, false);
        canvas.sortingOrder = 0;
        ClearDeckCards();
    }
    IEnumerator HideDecks()
    {
        if (currentState != DeckBuilderState.EditingDeckView && 
            currentState != DeckBuilderState.SelectingDeck &&
            currentState != DeckBuilderState.DeletingDeck)
            yield break;
        currentState = DeckBuilderState.ViewingCollection;
        yield return MoveView(decksView, false);
        ClearDecks();
    }

    private void ClearDecks()
    {
        foreach (Transform child in deckContent.transform)
        {
            Destroy(child.gameObject);
        }
        deckViewScrollRect.verticalNormalizedPosition = 1f;
    }

    private void ClearDeckCards()
    {
        foreach (var card in cardsInDeck)
        {
            Destroy(card);
        }
        cardsInDeck.Clear();
        deckEditViewScrollRect.verticalNormalizedPosition = 1f;
    }

    IEnumerator MoveView(GameObject view, bool show)
    {
        view.SetActive(true);
        float duration = 0.2f;
        float elapsedTime = 0f;
        Vector3 startingPos = view.transform.position;
        Vector3 targetPos = new Vector3(show ? 18 : -600, startingPos.y, startingPos.z);
        while (elapsedTime < duration)
        {
            view.transform.position = Vector3.Lerp(startingPos, targetPos, (elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        view.transform.position = targetPos;
        if (!show)
            view.SetActive(false);
    }

    private IEnumerator UpdateUI()
    {
        yield return null; // Wait a frame to ensure all changes are applied
        cardCountText.text = $"{cardsInDeck.Count}/40";
        heroCountText.text = $"{cardsInDeck.FindAll(c => c.GetComponent<CardUI>().CurrentCard is HeroCardSO).Count}";
        equipmentCountText.text = $"{cardsInDeck.FindAll(c => c.GetComponent<CardUI>().CurrentCard is EquipmentCardSO).Count}";
        spellCountText.text = $"{cardsInDeck.FindAll(c => c.GetComponent<CardUI>().CurrentCard is SpellCardSO).Count}";
    }

    IEnumerator ScrollDeckEditingView()
    {
        yield return null;
        float duration = 0.2f;
        float elapsedTime = 0f;
        Vector2 startingPos = new Vector2(deckEditViewScrollRect.verticalNormalizedPosition, 0);
        Vector2 targetPos = new Vector2(-0f, 0f);
        while (elapsedTime < duration)
        {
            deckEditViewScrollRect.verticalNormalizedPosition = Vector2.Lerp(startingPos, targetPos, (elapsedTime / duration)).x;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        deckEditViewScrollRect.verticalNormalizedPosition = targetPos.x;
    }

    public void EnlargeCard(CardUI card)
    {
        isEnlargedCard = true;

        if(card.cardDraggable.DeckDropZone.isDeck)
            currentEnlargedCardIndex = cardsInDeck.IndexOf(card.gameObject);
        else
            currentEnlargedCardIndex = collectionMenu.CardsList.IndexOf(card);

        enlargedCardHolder.ShowCard(card);
    }

    public void ResizeCard()
    {
        isEnlargedCard = false;
    }

    public bool CanAddCardToDeck(CardSO cardSO)
    {
        if (cardsInDeck.Count >= 40 || blockedCards.Contains(cardSO.CardID))
            return false;

        return true;
    }

    public int GetCardCountInDeck(CardSO cardSO)
    {
        int count = 0;
        foreach (var c in selectedDeckEdit.cardsIds)
        {
            if (c == cardSO.CardID)
            {
                count++;
            }
        }
        return count;
    }

    List<int> blockedCards = new List<int>();
    public void BlockCard(CardUI cardUI)
    {
        if(cardUI.CurrentCard is SpellCardSO)
        {
            int count = GetCardCountInDeck(cardUI.CurrentCard);
            if (count >= 4)
            {
                collectionMenu.Cards.FirstOrDefault(c => c.CurrentCard.CardID == cardUI.CurrentCard.CardID)?.cardDraggable.SetAlpha(0.3f);
                blockedCards.Add(cardUI.CurrentCard.CardID);
            }
        }
        else
        {
            collectionMenu.Cards.FirstOrDefault(c => c.CurrentCard.CardID == cardUI.CurrentCard.CardID)?.cardDraggable.SetAlpha(0.3f);
            blockedCards.Add(cardUI.CurrentCard.CardID);
        }
            
    }

    public void BlockCards(List<CardUI> cardUIs)
    {
        foreach (var cardUI in cardUIs)
        {
            BlockCard(cardUI);
        }
    }

    public void UnblockCard(CardUI cardUI)
    {
        collectionMenu.Cards.FirstOrDefault(c => c.CurrentCard.CardID == cardUI.CurrentCard.CardID)?.cardDraggable.SetAlpha(1f);
        blockedCards.Remove(cardUI.CurrentCard.CardID);
    }
    public void BlockAllCards()
    {
        foreach (var card in collectionMenu.Cards)
        {
            card.cardDraggable.SetAlpha(0.3f);
        }
    }

    public void UnblockAllCards()
    {
        foreach (var card in collectionMenu.Cards)
        {
            if(blockedCards.Contains(card.CurrentCard.CardID))
                BlockCard(card);
            else
                card.cardDraggable.SetAlpha(1f);
        }
    }

    private void ChangeEnlargedCard(bool increase)
    {
        if (enlargedCardHolder.OrigCard.cardDraggable.DeckDropZone.isDeck)
        {
            currentEnlargedCardIndex = (currentEnlargedCardIndex + (increase ? 1 : -1 + cardsInDeck.Count)) % cardsInDeck.Count;
            enlargedCardHolder.ShowCard(cardsInDeck[currentEnlargedCardIndex].GetComponent<CardUI>());
        }
        else if (collectionMenu.Cards.Count > 0)
        {
            currentEnlargedCardIndex = (currentEnlargedCardIndex + (increase ? 1 : -1 + collectionMenu.Cards.Count)) % collectionMenu.Cards.Count;
            enlargedCardHolder.ShowCard(collectionMenu.Cards[currentEnlargedCardIndex]);
        }
    }
}
