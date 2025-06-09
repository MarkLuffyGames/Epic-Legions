using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.GPUSort;

public class SampleCard : MonoBehaviour
{
    [SerializeField] private List<Card> cards = new List<Card>();
    [SerializeField] private List<Vector3> positions = new List<Vector3>();

    private bool isEnlarged = false;
    private Card card;

    public bool IsEnlarged => isEnlarged;
    public Card Card => card;

    private void Awake()
    {
        foreach (var card in cards)
        {
            card.gameObject.SetActive(false);
        }
    }
    public void Enlarge(Card card)
    {
        this.card = card;
        isEnlarged = true;
        EnlargeCard(card, 0, positions[0]);

        if (card.GetEquipmentCounts() > 0)
        {
            for (int i = 0; i < card.EquipmentCard.Length; i++)
            {
                if(card.EquipmentCard[i] != null)
                {
                    EnlargeCard(card.EquipmentCard[i], i + 1, positions[i + 1]);
                }
            }
        }

        if(card.graveyard != null)
        {
            if (card.graveyard.GetCards().Count > 1)
            {
                EnlargeCard(card.graveyard.GetCards()[1], 2, positions[2]);
            }
        }
    }

    public void EnlargeCard(Card card, int cardIndex, Vector3 position)
    {
        cards[cardIndex].gameObject.SetActive(true);
        SetCard(cards[cardIndex], card, card.DuelManager);
        cards[cardIndex].transform.localScale = Vector3.one;
        cards[cardIndex].transform.position = card.transform.position;
        cards[cardIndex].transform.rotation = card.transform.rotation;
        StartCoroutine(cards[cardIndex].MoveToPosition(position, Card.cardMovementSpeed, true, true));
        cards[cardIndex].RotateToAngle(Vector3.right * 53, Card.cardMovementSpeed, true);
        cards[cardIndex].ChangedSortingOrder(110);
        cards[cardIndex].EnableActions(card.isPlayer && !card.actionIsReady && card.isMyTurn);
    }

    public void ResetSize()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (!cards[i].gameObject.activeSelf) continue;
            StartCoroutine(ResetCardSize(i));
        }

        isEnlarged = false;
    }

    public IEnumerator ResetCardSize( int cardIndex)
    {
        cards[cardIndex].transform.rotation = card.transform.rotation;
        StartCoroutine(cards[cardIndex].ScaleCard(Vector3.zero, Card.cardMovementSpeed, true));
        yield return cards[cardIndex].MoveToPosition(card.transform.position, Card.cardMovementSpeed, true, false);

        card = null;
        cards[cardIndex].CleanCard();
        cards[cardIndex].gameObject.SetActive(false);
    }
    public void SetCard(Card cardToSet, Card cardToCopy, DuelManager duelManager)
    {
        cardToSet.CopyCard(cardToCopy, duelManager);
    }

    public void OnClick(Card card)
    {
        
        if (card == null || !CardWasClicked(card))
        {
            ResetSize();
        }
        else
        {
            if (card.transform.localPosition == positions[1])
            {
                ShiftCardsRight();
            }
            else if (card.transform.localPosition == positions[2])
            {
                ShiftCardsLeft();
            }
        }
    }

    public bool CardWasClicked(Card card)
    {
        bool wasClicked = false;

        foreach (var c in cards)
        {
            if (c.gameObject.activeSelf && c == card)
            {
                wasClicked = true;
                break;
            }
        }

        return wasClicked;
    }

    private void ShiftCardsRight()
    {
        if (card.graveyard != null)
        {
            int predecessor = card.graveyard.GetCardIndex(card) - 2;
            if (predecessor >= 0)
            {
                Debug.Log("Enlarging predecessor card: " + predecessor + " at position: " + GetCardInPosition(positions[3]));
                EnlargeCard(card.graveyard.GetCard(predecessor), GetCardInPosition(positions[3]), positions[3]);
            }
            else
            {
                Debug.Log("No predecessor card found, deactivating position 3");
                //cards[GetCardInPosition(positions[3])].gameObject.SetActive(false);
            }

            foreach (var card in cards)
            {
                if (card.gameObject.activeSelf)
                {
                    ShiftCardRigth(card);
                }
            }
        }
        
    }

    private void ShiftCardsLeft()
    {
        if (card.graveyard != null)
        {
            int successor = card.graveyard.GetCardIndex(cards[GetCardInPosition(positions[0])]) + 2;
            if (successor < card.graveyard.GetCards().Count)
            {
                Debug.Log("Enlarging successor card: " + successor + " at position: " + GetCardInPosition(positions[3]));

                Card cardToSet = cards[GetCardInPosition(positions[3])];
                cardToSet.gameObject.SetActive(true);
                SetCard(cardToSet, card, card.DuelManager);
                cardToSet.transform.localScale = Vector3.one;
                cardToSet.transform.position = positions[3];
                cardToSet.transform.rotation = Quaternion.Euler(Vector3.right * 53);
                cardToSet.ChangedSortingOrder(110);
                StartCoroutine(cardToSet.MoveToPosition(positions[3], Card.cardMovementSpeed, true, true));
            }
            else
            {
                Debug.Log("No successor card found, deactivating position 3");
                //cards[GetCardInPosition(positions[3])].gameObject.SetActive(false);
            }

                foreach (var card in cards)
                {
                    if (card.gameObject.activeSelf)
                    {
                        ShiftCardLeft(card);
                    }
                }
        }
            
    }

    private void ShiftCardRigth(Card card)
    {
        card.EnableActions(false);
        if (card.transform.localPosition == positions[0])
        {
            StartCoroutine(card.MoveToPosition(positions[2], Card.cardMovementSpeed, true, true));
        }
        else if (card.transform.localPosition == positions[1])
        {
            StartCoroutine(card.MoveToPosition(positions[0], Card.cardMovementSpeed, true, true));
            card.EnableActions(this.card.isPlayer && !this.card.actionIsReady && this.card.isMyTurn);
        }
        else if (card.transform.localPosition == positions[2])
        {
            StartCoroutine(card.MoveToPosition(positions[3], Card.cardMovementSpeed, true, true));
        }
        else if (card.transform.localPosition == positions[3])
        {
            StartCoroutine(card.MoveToPosition(positions[1], Card.cardMovementSpeed, true, true));
        }
    }

    private void ShiftCardLeft(Card card)
    {
        card.EnableActions(false);
        if (card.transform.localPosition == positions[0])
        {
            StartCoroutine(card.MoveToPosition(positions[1], Card.cardMovementSpeed, true, true));
        }
        else if (card.transform.localPosition == positions[1])
        {
            StartCoroutine(card.MoveToPosition(positions[3], Card.cardMovementSpeed, true, true));
        }
        else if (card.transform.localPosition == positions[2])
        {
            StartCoroutine(card.MoveToPosition(positions[0], Card.cardMovementSpeed, true, true));
            card.EnableActions(this.card.isPlayer && !this.card.actionIsReady && this.card.isMyTurn);
        }
        else if (card.transform.localPosition == positions[3])
        {
            StartCoroutine(card.MoveToPosition(positions[2], Card.cardMovementSpeed, true, true));
        }
    }

    public int GetCardInPosition(Vector3 position)
    {
        foreach (var card in cards)
        {
            Debug.Log("Checking card at position: " + card.transform.localPosition + " against target position: " + position);
            if (card.targetLocalPosition == position || card.transform.localPosition == position)
            {
                return cards.IndexOf(card);
            }
        }
        return -1;
    }
}
