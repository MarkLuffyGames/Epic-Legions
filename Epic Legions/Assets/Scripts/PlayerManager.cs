using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private List<CardSO> deck;
    [SerializeField] private Transform deckPosition;
    [SerializeField] private List<FieldPosition> fieldPositionList;
    [SerializeField] private HandCardHandler handCardHandler;
    [SerializeField] private DuelManager duelManager;
    [SerializeField] private PlayerManager rivalPlayerManager;

    public GameObject cardPrafabTest;

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
            var newCard = Instantiate(cardPrafabTest, deckPosition);
            newCard.transform.rotation = Quaternion.Euler(-90, 0, 0);
            newCard.transform.localPosition = new Vector3(0, i * 0.02f, 0);
            card.Add(newCard.GetComponent<Card>());
            card[i].SetCard(deck[i]);
        }

        isReady = true;
        if (isReady && rivalPlayerManager.isReady)
        {
            DuelManager.instance.SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId, true);
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

        isReady = true;
        if(isReady && rivalPlayerManager.isReady)
        {
            DuelManager.instance.SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId, true);
        }
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


}
