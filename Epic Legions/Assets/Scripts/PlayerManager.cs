using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private List<CardSO> deck;
    [SerializeField] private Transform deckPosition;
    [SerializeField] private List<FieldPosition> fieldPositionList;
    [SerializeField] private HandCardHandler handCardHandler;
    [SerializeField] private DuelManager duelManager;
    [SerializeField] private PlayerManager rivalPlayerManager;
    [SerializeField] private GameObject nextPhaseButton;
    [SerializeField] private GameObject waitTextGameObject;

    public GameObject cardPrafab;

    private List<Card> card = new List<Card>();
    public int playerRole;

    public bool isReady;

    public void SetPlayerRole(int role)
    {
        playerRole = role;
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
            card[i].SetCard(deck[i]);
        }

        isReady = true;
        IsReady();
    }

    /// <summary>
    /// Roba las cartas iniciales del duelo
    /// </summary>
    /// <returns></returns>
    public void DrawStartCards()
    {
        Debug.Log("Robando cartas de inicio.");
        StartCoroutine(DrawStartCardsCoroutine());
    }

    private IEnumerator DrawStartCardsCoroutine()
    {
        yield return new WaitForSeconds(1);

        for (int i = 0; i < 7; i++)
        {
            DrawCard();
            Debug.Log("Carta robada");
            yield return new WaitForSeconds(0.05f);
        }

        isReady = true;
        IsReady();
    }

    private void IsReady()
    {
        if (isReady && rivalPlayerManager.isReady && NetworkManager.Singleton.IsClient)
        {
            DuelManager.instance.SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    public void SetPlayerReady()
    {
        DuelManager.instance.SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        isReady = true;
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
    public void DrawCard()
    {
        handCardHandler.GetNewCard(card[card.Count - 1]);
        card.RemoveAt(card.Count - 1);
    }

    /// <summary>
    /// Muestra las posiciones disponibles en el campo
    /// </summary>
    public void ShowAvailablePositions()
    {
        foreach (var item in fieldPositionList)
        {
            item.Highlight();
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
    }

    public List<FieldPosition> GetFieldPositionList()
    {
        return fieldPositionList;
    }

    public HandCardHandler GetHandCardHandler()
    {
        return handCardHandler;
    }
}
