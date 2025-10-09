using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IATutorial : MonoBehaviour
{
    [SerializeField] private Tutorial duelManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private HandCardHandler handCardHandler;

    public List<Card> heroesInTurn = new List<Card>();
    int turnCount = 0;

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
        if (newValue == DuelPhase.Preparation)
        {
            turnCount++;
            StartCoroutine(PlayCardsHand());
        }
        else if (newValue == DuelPhase.Battle)
        {
            
        }
    }

    private IEnumerator PlayCardsHand()
    {
        if (turnCount == 1)
        {
            SummonHero(handCardHandler.GetCardInHandList()[0], 2);
        }
        else if (turnCount == 2)
        {
            SummonHero(handCardHandler.GetCardInHandList()[0], 7);
        }

        yield return new WaitForSeconds(1);

        playerManager.SetPlayerReady();
    }

    private IEnumerator DefineActions()
    {
        yield return new WaitForSeconds(1);
        heroesInTurn.Clear();

        foreach (var card in duelManager.HeroInTurn)
        {
            if (playerManager.GetAllCardInField().Contains(card) && !card.IsControlled()) heroesInTurn.Add(card);
        }
        if(heroesInTurn.Count == 0) yield break;

        if (turnCount == 1 || turnCount == 2)
        {
            duelManager.UseMovement(2, heroesInTurn[0]);
        }
    }

    private void SummonHero(Card heroToPlay, int positionIndex)
    {
        duelManager.PlaceCardInField(playerManager, playerManager.isPlayer,
                    handCardHandler.GetIdexOfCard(heroToPlay), positionIndex);
    }
}
