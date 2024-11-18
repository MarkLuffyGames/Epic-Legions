using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class HandCardHandler : MonoBehaviour
{
    [SerializeField] private List<Card> cardsList = new List<Card>();
    [SerializeField] private float maxDistanceCards = 1.5f;
    [SerializeField] private bool IsPlayer;

    private bool isHideCard;

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    /// <summary>
    /// Establece las posiciones de las cartas en la mano
    /// </summary>
    private void SetCardsPosition()
    {
        if (cardsList.Count == 0) return;

        if (cardsList.Count == 1)
        {
            cardsList[0].transform.localPosition = Vector3.zero;
        }
        else
        {
            for (int i = 0; i < cardsList.Count; i++)
            {
                float distance = 6.0f / (cardsList.Count - 1);
                if (distance > maxDistanceCards) distance = maxDistanceCards;

                var x = ((i + 1) * distance) - distance;
                x -= (distance * (cardsList.Count - 1)) / 2f;
                cardsList[i].MoveToPosition(new Vector3(x, i * 0.001f, 0), 20, false, true);

                var rotationX = IsPlayer ? 70 : -90;
                cardsList[i].RotateToAngle(new Vector3(rotationX, 0, 0), 20);

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
    public bool IsCardOwnedByPlayer(Card card)
    {
        return cardsList.Contains(card);
    }

    /// <summary>
    /// Muestra las cartas de la mano
    /// </summary>
    public void ShowHandCard()
    {
        if (isHideCard)
        {
            SetCardsPosition();
            isHideCard = false;
        }
    }
    
    /// <summary>
    /// Oculta las cartas de la mano
    /// </summary>
    public void HideHandCard()
    {
        if (!isHideCard)
        {
            foreach (Card card in cardsList)
            {
                if (!card.IsDragging())
                {
                    card.MoveToPosition(card.transform.localPosition + Vector3.back * 1.3f, 20, true, true);
                }
            }

            isHideCard = true;

        }
    }
}
