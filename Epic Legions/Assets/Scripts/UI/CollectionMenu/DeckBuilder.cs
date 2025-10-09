using System.Collections;
using System.Collections.Generic;
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

    [SerializeField] private TextMeshProUGUI menuName;
    [SerializeField] private TextMeshProUGUI cardCountText;
    [SerializeField] private TextMeshProUGUI heroCountText;
    [SerializeField] private TextMeshProUGUI equipmentCountText;
    [SerializeField] private TextMeshProUGUI spellCountText;

    public DeckBuilderState currentState = DeckBuilderState.ViewingCollection;

    private Deck selectedDeck;

    public bool isEnlargedCard;
    [SerializeField] private EnlargedCardHolder enlargedCardHolder;

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


        deckEditViewScrollRect = deckEditView.GetComponentInChildren<ScrollRect>();
        deckViewScrollRect = decksView.GetComponentInChildren<ScrollRect>();

        selectDeckButton.GetComponentInChildren<TextMeshProUGUI>().text = GameData.Instance.CurrentDeck.deckName;

        deckEditView.SetActive(false);
        decksView.SetActive(false);
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
        StartCoroutine(UpdateUI());
        yield return MoveView(deckEditView, true);
    }

    IEnumerator ConfirmDeckCreation()
    {
        if (currentState != DeckBuilderState.CreatingDeck)
            yield break;

        currentState = DeckBuilderState.ViewingCollection;
        GameData.Instance.SaveNewDeck(deckName.text, GetCardIndicesInDeck());
        yield return MoveView(deckEditView, false);
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
        yield return MoveView(deckEditView, false);
        ClearDeckCards();
    }

    public void AddCardToDeck(GameObject card)
    {
        cardsInDeck.Add(card);
        StartCoroutine(UpdateUI());
    }

    public void RemoveCardFromDeck(GameObject card)
    {
        cardsInDeck.Remove(card);
        StartCoroutine(UpdateUI());
    }

    IEnumerator ShowDecksViewToSelect()
    {
        if (currentState != DeckBuilderState.ViewingCollection)
            yield break;
        currentState = DeckBuilderState.SelectingDeck;
        ShowDecks();
        menuName.text = "Seleccionar Mazo";
        yield return MoveView(decksView, true);
    }

    IEnumerator ShowDecksViewToEdit()
    {
        if (currentState != DeckBuilderState.ViewingCollection)
            yield break;
        currentState = DeckBuilderState.EditingDeckView;
        ShowDecks();
        menuName.text = "Editar Mazo";
        yield return MoveView(decksView, true);
    }

    IEnumerator ShowDecksViewToDelete()
    {
        if (currentState != DeckBuilderState.ViewingCollection)
            yield break;
        currentState = DeckBuilderState.DeletingDeck; 
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
        foreach (var cardId in deck.cardsIds)
        {
            var cardGO = Instantiate(cardPrefab, cardContent.transform);
            var rect = cardGO.transform as RectTransform;
            rect.localScale = Vector3.one * 100;
            cardGO.GetComponent<CardUI>().SetCard(CardDatabase.GetCardById(cardId));
            cardsInDeck.Add(cardGO);
        }
        deckName.text = deck.deckName;
        StartCoroutine(UpdateUI());
        yield return MoveView(deckEditView, true);

    }

    IEnumerator ConfirmDeckEditing()
    {
        if (currentState != DeckBuilderState.EditingDeck)
            yield break;

        currentState = DeckBuilderState.ViewingCollection;
        GameData.Instance.UpdateDeck(selectedDeck, deckName.text, GetCardIndicesInDeck());
        yield return MoveView(deckEditView, false);
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

    public void EnlargeCard(CardUI card)
    {
        isEnlargedCard = true;
        enlargedCardHolder.ShowCard(card.CurrentCard);
    }

    public void ResizeCard()
    {
        isEnlargedCard = false;
    }
}
