using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class HemeraLegionAI : MonoBehaviour
{
    [SerializeField] private DuelManager duelManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private HandCardHandler handCardHandler;

    private void Awake()
    {
        duelManager.duelPhase.OnValueChanged += OnDuelPhaseChanged;
        duelManager.OnChangeTurn += DuelManager_OnChangeTurn;
    }

    private void DuelManager_OnChangeTurn(object sender, EventArgs e)
    {
        StartCoroutine(DefineActions());
    }

    private void OnDuelPhaseChanged(DuelPhase previousValue, DuelPhase newValue)
    {
        if(newValue == DuelPhase.Preparation)
        {
            StartCoroutine(PlayCard());
        }
    }

    private IEnumerator PlayCard()
    {
        yield return new WaitForSeconds(Random.Range(3, 6));

        if(handCardHandler.GetCardInHandList().Count > 0)
        {
            List<Card> usableCards = new List<Card>();

            foreach (var card in handCardHandler.GetCardInHandList())
            {
                if(card.cardSO is HeroCardSO && card.UsableCard(playerManager)) usableCards.Add(card);
            }

            if (usableCards.Count > 0)
            {
                duelManager.PlaceCardInField(playerManager, playerManager.isPlayer,
                    handCardHandler.GetIdexOfCard(usableCards[Random.Range(0, usableCards.Count)]), ChoosePositionFieldIndex());
            }
            playerManager.SetPlayerReady();
        }
    }

    private int ChoosePositionFieldIndex()
    {
        List<FieldPosition> availablePositions = new List<FieldPosition>();

        foreach(var field in playerManager.GetFieldPositionList())
        {
            if(field.Card == null) availablePositions.Add(field);
        }

        return availablePositions[Random.Range(0, availablePositions.Count)].PositionIndex;
    }
    public List<Card> heroesInTurn = new List<Card>();
    private IEnumerator DefineActions()
    {
        yield return new WaitForSeconds(Random.Range(3, 6));
        heroesInTurn.Clear();

        foreach (var card in duelManager.HeroInTurn)
        {
            if(playerManager.GetAllCardInField().Contains(card)) heroesInTurn.Add(card);
        }

        foreach (var card in heroesInTurn)
        {
            var movementToUse = ChooseMovemetIndex(card);
            if (card.Moves[movementToUse].MoveSO.NeedTarget)
            {
                duelManager.UseMovement(movementToUse, card, ChooseTargetIndex(card, movementToUse));
            }
            else
            {

                duelManager.UseMovement(movementToUse, card);
            }
        }
    }

    private int ChooseMovemetIndex(Card card)
    {
        int random = Random.Range(0, 3);

        if (card.UsableMovement(random, playerManager))
        {
            return random;
        }
        
        return ChooseMovemetIndex(card);
    }

    private int ChooseTargetIndex(Card card, int movementToUse)
    {
        var targets = duelManager.ObtainTargets(card, movementToUse);

        return targets[Random.Range(0, targets.Count)].FieldPosition.PositionIndex;
    }
}
