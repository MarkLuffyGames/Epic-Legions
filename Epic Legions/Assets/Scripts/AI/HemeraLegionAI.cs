using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;
using Unity.Burst.Intrinsics;

public class HemeraLegionAI : MonoBehaviour
{
    [SerializeField] private DuelManager duelManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private HandCardHandler handCardHandler;

    private int turn;

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
            turn++;
            StartCoroutine(PlayCard());
        }
    }

    private IEnumerator PlayCard()
    {
        yield return new WaitForSeconds(Random.Range(3, 6));

        if(handCardHandler.GetCardInHandList().Count > 0)
        {
            List<Card> usableHeroesCards = new List<Card>();

            foreach (var card in handCardHandler.GetCardInHandList())
            {
                if(card.cardSO is HeroCardSO && card.UsableCard(playerManager)) usableHeroesCards.Add(card);
            }
             
            if (usableHeroesCards.Count > 0)
            {
                ChooseWhetherSummonHero(usableHeroesCards);
            }

            playerManager.SetPlayerReady();
        }
    }

    private void ChooseWhetherSummonHero(List<Card> usableHeroesCards)
    {
        Card heroToPlay = null;

        if (turn == 1)
        {
            heroToPlay = usableHeroesCards.OrderByDescending(h => h.CurrentDefensePoints).First();
        }
        else
        {

        }

        int positionToPlay = ChoosePositionFieldIndex(heroToPlay);

        if (heroToPlay != null && positionToPlay != -1)
        {
            SummonHero(heroToPlay, positionToPlay);
        }

        
    }

    private void SummonHero(Card heroToPlay, int positionIndex)
    {
        duelManager.PlaceCardInField(playerManager, playerManager.isPlayer,
                    handCardHandler.GetIdexOfCard(heroToPlay), positionIndex);
    }

    private int ChoosePositionFieldIndex(Card heroToPlay)
    {
        List<FieldPosition> availablePositions = new List<FieldPosition>();

        foreach(var field in playerManager.GetFieldPositionList())
        {
            if(field.Card == null) availablePositions.Add(field);
        }

        if(heroToPlay.CurrentDefensePoints >= 60)
        {
            availablePositions.RemoveAll(p => p.PositionIndex > 4);
        }
        else if(heroToPlay.CurrentDefensePoints >= 30 && heroToPlay.CurrentDefensePoints < 60)
        {
            availablePositions.RemoveAll(p => p.PositionIndex < 5 && p.PositionIndex > 9);
        }
        else
        {
            availablePositions.RemoveAll(p => p.PositionIndex < 10);
        }

        if(availablePositions.Count > 0)
        {
            return availablePositions[Random.Range(0, availablePositions.Count)].PositionIndex;
        }

        return -1;
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
