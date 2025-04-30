using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private List<CardSO> deck;
    [SerializeField] private Transform deckPosition;
    [SerializeField] private Transform graveyardPosition;
    [SerializeField] private List<FieldPosition> fieldPositionList;
    [SerializeField] private FieldPosition spellFieldPosition;
    [SerializeField] private HandCardHandler handCardHandler;
    [SerializeField] private DuelManager duelManager;
    [SerializeField] private PlayerManager rivalPlayerManager;
    [SerializeField] private GameObject nextPhaseButton;
    [SerializeField] private GameObject waitTextGameObject;
    [SerializeField] private TextMeshProUGUI playerHealtText;
    [SerializeField] private TextMeshProUGUI playerEnergyText;

    private int playerHealt;
    private int playerEnergy;

    public GameObject cardPrafab;

    private List<Card> card = new List<Card>();

    public bool isReady;

    public FieldPosition SpellFieldPosition => spellFieldPosition;
    public int PlayerEnergy => playerEnergy;

    public bool isPlayer;


    private void Start()
    {
        SetPlayerHealt(100);
        SetPlayerEnergy(100);
    }
    public void AddCardToPlayerDeck(CardSO cardSO, int numberOfCards)
    {
        deck.Add(cardSO);
        
        if(deck.Count == numberOfCards)
        {
            InstancePlayerDeck();
        }
    }

    /// <summary>
    /// Instancia el deck del jugador
    /// </summary>
    public void InstancePlayerDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            var newCard = Instantiate(cardPrafab, deckPosition);
            newCard.transform.rotation = Quaternion.Euler(-90, 0, 0);
            newCard.transform.localPosition = new Vector3(0, i * 0.02f, 0);
            card.Add(newCard.GetComponent<Card>());
            card[i].SetCard(deck[i], duelManager);
        }

        if (isPlayer || duelManager.IsSinglePlayer)
        {
            isReady = true;
            IsReady();
        }
        
    }

    /// <summary>
    /// Roba las cartas iniciales del duelo
    /// </summary>
    /// <returns></returns>
    public void DrawStartCards()
    {
        StartCoroutine(DrawStartCardsCoroutine());
    }

    private IEnumerator DrawStartCardsCoroutine()
    {
        yield return new WaitForSeconds(1);

        for (int i = 0; i < 7; i++)
        {
            DrawCard();
            yield return new WaitForSeconds(0.05f);
        }

        if (isPlayer || duelManager.IsSinglePlayer)
        {
            isReady = true;
            IsReady();
        }
    }

    private void IsReady()
    {
        if (duelManager.IsSinglePlayer)
        {
            duelManager.SetPlayerReadyAndTransitionPhase();
        }
        else if (isReady && NetworkManager.Singleton.IsClient)
        {
            duelManager.SetPlayerReadyAndTransitionPhaseServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    public void SetPlayerReady()
    {
        isReady = true;
        IsReady();
    }

    public void ShowNextPhaseButton()
    {
        nextPhaseButton.SetActive(true);
    }

    public void HideNextPhaseButton()
    {
        nextPhaseButton.SetActive(false);
    }

    public void ShowWaitTextGameObject()
    {
        waitTextGameObject.SetActive(true);
    }

    public void HideWaitTextGameObject()
    {
        waitTextGameObject.SetActive(false);
    }

    /// <summary>
    /// Metodo para robar una carta.
    /// </summary>
    public bool DrawCard()
    {
        var wasStolen = false;

        if (card.Count > 0)
        {
            handCardHandler.GetNewCard(card[card.Count - 1]);
            card.RemoveAt(card.Count - 1);

            wasStolen = true;
        }

        if(duelManager.GetCurrentDuelPhase() == DuelPhase.DrawingCards)
        {
            if (isPlayer || duelManager.IsSinglePlayer)
            {
                isReady = true;
                IsReady();
            }
        }
        
        return wasStolen;
    }

    /// <summary>
    /// Muestra las posiciones disponibles en el campo
    /// </summary>
    public void ShowAvailablePositions(Card card)
    {
        if(card.cardSO is HeroCardSO)
        {
            foreach (var position in fieldPositionList)
            {
                if(position.Card == null)
                {
                    position.Highlight();
                }
            }
        }
        else if (card.cardSO is SpellCardSO)
        {
            spellFieldPosition.Highlight();
        }


    }

    /// <summary>
    /// Oculta las posciones disponibles en el campo
    /// </summary>
    public void HideAvailablePositions()
    {
        foreach (var item in fieldPositionList)
        {
            item.RemoveHighlight();
        }
        spellFieldPosition.RemoveHighlight();
    }

    public List<FieldPosition> GetFieldPositionList()
    {
        return fieldPositionList;
    }

    public HandCardHandler GetHandCardHandler()
    {
        return handCardHandler;
    }

    public Transform GetGraveyard()
    {
        return graveyardPosition;
    }

    public List<Card> GetLineForCard(Card card)
    {
        if (card.FieldPosition.PositionIndex < 5)
        {
            return GetLineForIndex(0, 5);
        }
        else if(card.FieldPosition.PositionIndex < 10)
        {
            return GetLineForIndex(5, 10);
        }
        else
        {
            return GetLineForIndex(10, 15);
        }
    }

    private List<Card> GetLineForIndex(int startLine, int endLine)
    {
        List<Card> cards = new List<Card>();

        for (int i = startLine; i < endLine; i++)
        {
            if (fieldPositionList[i].Card != null)
            {
                cards.Add(fieldPositionList[i].Card);
            }
        }

        return cards;
    }

    public List<Card> GetAllCardInField()
    {
        List<Card> cards = new List<Card>();

        foreach(var field in fieldPositionList)
        {
            if(field.Card != null)
            {
                cards.Add(field.Card);
            }
        }

        return cards;
    }

    public void SetPlayerHealt(int playerHealt)
    {
        this.playerHealt = playerHealt;
        UpdateUI();
    }

    public void SetPlayerEnergy(int playerEnergy)
    {
        this.playerEnergy = playerEnergy;
        UpdateUI();
    }

    public void ConsumeEnergy(int amount)
    {
        playerEnergy -= amount;
        UpdateUI();
    }

    public void RechargeEnergy(int amount)
    {
        playerEnergy += amount;
        if (playerEnergy > 100) playerEnergy = 100;
        UpdateUI();
    }
    private void UpdateUI()
    {
        playerHealtText.text = $"{playerHealt}";
        playerEnergyText.text = $"{playerEnergy}";
    }

    public bool ReceiveDamage(int damage)
    {
        //TODO: Ejecutar animacion de daño del jugador.
        playerHealt -= damage;
        if(playerHealt < 0) playerHealt = 0;
        UpdateUI();

        return playerHealt == 0;
    }
}
