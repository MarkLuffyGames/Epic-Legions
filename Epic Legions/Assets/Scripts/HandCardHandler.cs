using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandCardHandler : MonoBehaviour
{
    [SerializeField] private List<Card> cardsList = new List<Card>();
    [SerializeField] private float maxDistanceCards = 1.5f;
    [SerializeField] private bool IsPlayer;
    [SerializeField] private GameObject hideCardButton;

    public bool isHideCards;

    /// <summary>
    /// Establece las posiciones de las cartas en la mano
    /// </summary>
    private void SetCardsPosition()
    {
        if (cardsList.Count == 0)
        {
            if (IsPlayer) hideCardButton.SetActive(false);
            return;
        }
        else
        {
            if(IsPlayer)hideCardButton.SetActive(!isHideCards);
            for (int i = 0; i < cardsList.Count; i++)
            {
                if(cardsList[i].IsDragging())continue;
                if(cardsList[i].waitForServer)continue;

                float distance = 6.0f / (cardsList.Count - 1);
                if (distance > maxDistanceCards) distance = maxDistanceCards;

                var x = ((i + 1) * distance) - distance;
                x -= (distance * (cardsList.Count - 1)) / 2f;
                StartCoroutine(cardsList[i].MoveToPosition(new Vector3(x, i * 0.001f, isHideCards ? -1.1f : 0), Card.cardMovementSpeed, isHideCards ? true : false, true));

                var rotationX = IsPlayer ? 53 : -90;
                cardsList[i].RotateToAngle(new Vector3(rotationX, 0, 0), Card.cardMovementSpeed, false);

                cardsList[i].SetSortingOrder(i + 100);
            }
        }
    }

    /// <summary>
    /// Agraga una nueva carta a la mano.
    /// </summary>
    /// <param name="card">Carta que se quiere agregar</param>
    public void GetNewCard(Card card)
    {
        cardsList.Add(card);
        card.transform.parent = transform;
        if(IsPlayer)card.isVisible = true;
        SetCardsPosition();
    }

    /// <summary>
    /// Quita una carta de la mano
    /// </summary>
    /// <param name="card">Carta que se quiere quitar</param>
    public void QuitCard(Card card)
    {
        cardsList.Remove(card);
        SetCardsPosition();
    }

    /// <summary>
    /// Metodo para comprobar si la carta esta en la mano del jugador
    /// </summary>
    /// <param name="card">Carta que queremos comprobar</param>
    /// <returns>Devuelve true si la carta esta en la mano</returns>
    public bool CardInThePlayerHand(Card card)
    {
        return cardsList.Contains(card);
    }

    /// <summary>
    /// Muestra las cartas de la mano
    /// </summary>
    public void ShowHandCard()
    {
        if (isHideCards)
        {
            /*foreach (Card card in cardsList)
            {
                if (!card.IsDragging())
                {
                    StartCoroutine(card.MoveToPosition(new Vector3(
                        card.transform.localPosition.x, card.transform.localPosition.y, 0), 20, false, true));
                    if (card.IsHighlight()) card.RemoveHighlight();
                }
            }*/

            hideCardButton.SetActive(true);
            isHideCards = false;

            SetCardsPosition();
        }
    }
    
    /// <summary>
    /// Oculta las cartas de la mano
    /// </summary>
    public void HideHandCard()
    {
        if (!isHideCards)
        {
            /*foreach (Card card in cardsList)
            {
                if (!card.IsDragging())
                {
                    StartCoroutine(card.MoveToPosition(new Vector3(
                        card.transform.localPosition.x, card.transform.localPosition.y, -1.3f), 20, true, true));
                    if(card.IsHighlight())card.RemoveHighlight();
                }
            }*/

            hideCardButton.SetActive(false);
            isHideCards = true;

            SetCardsPosition();
        }
    }

    public List<Card> GetCardInHandList()
    {
        return cardsList;
    }

    public int GetIdexOfCard(Card card)
    {
        return cardsList.IndexOf(card);
    }

    private static GameObject selectedButtonObject;
    public static bool IsMouseOverButton()
    {
        // Obtén el objeto debajo del mouse
        GameObject newSelectedButtonObject = EventSystem.current.currentSelectedGameObject;

        if(newSelectedButtonObject != null)
        {
            if(newSelectedButtonObject != selectedButtonObject)
            {
                selectedButtonObject = newSelectedButtonObject;
                //Comprueba que es un boton
                if (selectedButtonObject.TryGetComponent<Button>(out Button button))
                {
                    return true;
                }
            }

            return true;
        }

        return false;
    }
}
