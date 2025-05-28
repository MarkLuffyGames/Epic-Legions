using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleCard : MonoBehaviour
{
    [SerializeField] private List<Card> cards = new List<Card>();
    [SerializeField] private Vector3 position1;
    [SerializeField] private Vector3 position2;
    [SerializeField] private Vector3 position3;
    [SerializeField] private Vector3 position4;

    private void Awake()
    {
        foreach (var card in cards)
        {
            card.gameObject.SetActive(false);
        }
    }
    public void Enlarge(Card card, DuelManager duelManager)
    {
        cards[0].gameObject.SetActive(true);
        SetCard(cards[0], card, duelManager);
        cards[0].transform.position = card.transform.position;
        cards[0].transform.rotation = card.transform.rotation;
        StartCoroutine(cards[0].MoveToPosition(position1, Card.cardMovementSpeed, true, true));
        cards[0].RotateToAngle(Vector3.right * 53, Card.cardMovementSpeed, true);
        cards[0].ChangedSortingOrder(110);
        cards[0].EnableActions(card.isPlayer && !card.actionIsReady && card.isMyTurn);
    }

    public IEnumerator ResetSize(Card card)
    {
        cards[0].transform.rotation = card.transform.rotation;
        yield return cards[0].MoveToPosition(card.transform.position, Card.cardMovementSpeed, true, false);

        cards[0].CleanCard();
        cards[0].gameObject.SetActive(false);
    }

    public void SetCard(Card cardToSet, Card cardToCopy, DuelManager duelManager)
    {
        cardToSet.CopyCard(cardToCopy, duelManager);
    }
}
