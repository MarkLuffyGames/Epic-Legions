using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

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
        yield return new WaitForSeconds(Random.Range(1, 3));

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
            heroToPlay = usableHeroesCards.OrderByDescending(h => h.Moves[0].MoveSO.Damage).First();
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
        yield return new WaitForEndOfFrame();
        heroesInTurn.Clear();

        foreach (var card in duelManager.HeroInTurn)
        {
            if(playerManager.GetAllCardInField().Contains(card)) heroesInTurn.Add(card);
        }

        var attackingHeroes = ChooseCombinations();

        foreach (var card in heroesInTurn)
        {
            if (attackingHeroes.Contains((card, card.Moves[0])) || attackingHeroes.Contains((card, card.Moves[1])))
            {
                if (card.Moves[0].MoveSO.NeedTarget)
                {
                    duelManager.UseMovement(0, card, ChooseTargetIndex(card, 0));
                }
                else
                {
                    duelManager.UseMovement(0, card);
                }
            }
            else
            {
                duelManager.UseMovement(2, card);
            }
            
        }
    }

    private List<(Card, Movement)> ChooseCombinations()
    {
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
        var combination = new List<(Card, Movement)>();

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

    private List<List<(Card Hero, Movement Attack)>> GenerateMoveCombinations()
    {
        var heroes = playerManager.GetAllCardInField();
        var usableCombinations = new List<List<(Card, Movement)>>();

        // Generar combinaciones desde 1 hasta el total de héroes
        for (int r = 1; r <= heroes.Count; r++)
        {
            List<List<(Card, Movement)>> combinations = GetHeroAttackCombinations(heroes, r);

            foreach (var comb in combinations)
            {
                int energy = 0;

                foreach (var (hero, attack) in comb)
                {
                    energy += attack.MoveSO.EnergyCost;
                }

                if (energy <= playerManager.PlayerEnergy)
                {
                    usableCombinations.Add(comb);
                }
            }
        }

        return usableCombinations;
    }

    // Generar combinaciones de héroes con ataques
    static List<List<(Card, Movement)>> GetHeroAttackCombinations(List<Card> heroes, int r)
    {
        List<(Card, Movement)> heroMoves = new List<(Card, Movement)>();

        // Crear lista de héroes con cada una de sus opciones de ataque
        foreach (var hero in heroes)
        {
            foreach (var move in hero.Moves)
            {
                heroMoves.Add((hero, move));
            }
        }

        return GetCombinations(heroMoves, r);
    }

    // Función para generar combinaciones de 'r' elementos
    static List<List<T>> GetCombinations<T>(List<T> elements, int r)
    {
        List<List<T>> result = new List<List<T>>();
        GenerateCombinations(elements, new List<T>(), 0, r, result);
        return result;
    }

    // Función recursiva para generar combinaciones
    static void GenerateCombinations<T>(List<T> elements, List<T> current, int index, int r, List<List<T>> result)
    {
        if (current.Count == r)
        {
            result.Add(new List<T>(current));
            return;
        }

        for (int i = index; i < elements.Count; i++)
        {
            current.Add(elements[i]);
            GenerateCombinations(elements, current, i + 1, r, result);
            current.RemoveAt(current.Count - 1); // Backtracking
        }
    }

}
