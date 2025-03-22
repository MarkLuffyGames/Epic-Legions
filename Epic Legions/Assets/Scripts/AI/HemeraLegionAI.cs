using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public enum GameStrategy { Defensive, Offensive, Balanced }
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
            PlanPlay();
        }
        else if(newValue == DuelPhase.Battle)
        {

        }
    }

    private void PlanPlay()
    {
        if(turn == 1)
        {
            //Invocar Heroes.
            Card heroToPlay = ChoosingHeroToSummon(GetPlayableHeroes(), GameStrategy.Defensive);
            SummonHero(heroToPlay, ChoosePositionFieldIndex(heroToPlay));
        }
        else
        {

        }

        playerManager.SetPlayerReady();
    }

    private List<Card> GetPlayableHeroes()
    {
        List<Card> usableHeroesCards = new List<Card>();

        foreach (var card in handCardHandler.GetCardInHandList())
        {
            if (card.cardSO is HeroCardSO && card.UsableCard(playerManager)) usableHeroesCards.Add(card);
        }

        return usableHeroesCards;
    }

    private Card ChoosingHeroToSummon(List<Card> usableHeroesCards, GameStrategy strategy)
    {
        Card heroToPlay = null;

        if(usableHeroesCards.Count != 0)
        {
            if(strategy == GameStrategy.Defensive)
            {
                heroToPlay = usableHeroesCards.OrderByDescending(h => h.CurrentDefensePoints).First();
            }
        }

        return heroToPlay;
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
        yield return new WaitForEndOfFrame();
        heroesInTurn.Clear();

        foreach (var card in duelManager.HeroInTurn)
        {
            if(playerManager.GetAllCardInField().Contains(card)) heroesInTurn.Add(card);
        }

        var attackingHeroes = ChooseCombinations();

        if (attackingHeroes == null) yield break;

        foreach (var (hero, attack) in attackingHeroes)
        {
            Debug.Log($"Hero: {hero.cardSO.CardName} Attack: {attack}");
        }

        foreach (var card in heroesInTurn)
        {
            foreach (var (hero, attack) in attackingHeroes)
            {
                if(hero == card)
                {
                    if (card.Moves[attack].MoveSO.NeedTarget)
                    {
                        duelManager.UseMovement(attack, card, ChooseTargetIndex(card, attack));
                    }
                    else
                    {
                        duelManager.UseMovement(attack, card);
                    }
                }
            } 
        }
    }

    private List<(Card, int)> ChooseCombinations()
    {
        if(playerManager.GetAllCardInField().Count == 0) return null;

        var combinations = GenerateMoveCombinations();
        var totalDamage = 0;

        var combinationIndex = -1;

        for (int i = 0; i < combinations.Count; i++)
        {
            int damage = 0;

            foreach (var card in combinations[i])
            {
                damage += card.Hero.Moves[0].MoveSO.Damage;
            }

            if (damage > totalDamage)
            {
                totalDamage = damage;
                combinationIndex = i;
            }
        }
        var combination = new List<(Card, int)>();

        foreach (var card in combinations[combinationIndex])
        {
            combination.Add(card);
        }

        return combination;
    }

    private int ChooseTargetIndex(Card card, int movementToUse)
    {
        var targets = duelManager.ObtainTargets(card, movementToUse);

        return targets[0].FieldPosition.PositionIndex;
    }

    /*private bool CanBreakDefense(Hero target)
    {
        int availableEnergy = gameController.AIEnergy;
        var bestCombination = GetBestMoveCombination(availableEnergy);
        int totalDamage = bestCombination.Sum(m => m.Damage);
        return totalDamage > target.Defense;
    }*/


    private List<List<(Card Hero, int Attack)>> GenerateMoveCombinations()
    {
        var heroes = playerManager.GetAllCardInField();
        var usableCombinations = new List<List<(Card, int)>>();

        // Obtener combinaciones asegurando que todos los héroes tienen una acción
        List<List<(Card, int)>> combinations = GetHeroAttackCombinations(heroes);

        foreach (var comb in combinations)
        {
            int energy = 0;

            foreach (var (hero, attack) in comb)
            {
                energy += hero.Moves[attack].MoveSO.EnergyCost;
            }

            if (energy <= playerManager.PlayerEnergy)
            {
                usableCombinations.Add(comb);
            }
        }

        return usableCombinations;
    }

    // Generar combinaciones asegurando que cada héroe tenga una acción (ataque o recargar)
    static List<List<(Card, int)>> GetHeroAttackCombinations(List<Card> heroes)
    {
        List<List<(Card, int)>> allCombinations = new List<List<(Card, int)>>();

        GenerateHeroMoveCombinations(heroes, new List<(Card, int)>(), 0, allCombinations);

        return allCombinations;
    }


    // Función recursiva para generar combinaciones de ataques
    static void GenerateHeroMoveCombinations(List<Card> heroes, List<(Card, int)> currentCombination, int index, List<List<(Card, int)>> result)
    {
        if (index == heroes.Count)
        {
            result.Add(new List<(Card, int)>(currentCombination));
            return;
        }

        var hero = heroes[index];

        for (int i = 0; i < hero.Moves.Count; i++)
        {
            currentCombination.Add((hero, i));
            GenerateHeroMoveCombinations(heroes, currentCombination, index + 1, result);
            currentCombination.RemoveAt(currentCombination.Count - 1);
        }
    }

}
