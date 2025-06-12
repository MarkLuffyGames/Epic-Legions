using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using Random = UnityEngine.Random;

public enum GameStrategy { Defensive, Offensive, Balanced }
public class HemeraLegionAI : MonoBehaviour
{
    [SerializeField] private DuelManager duelManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private HandCardHandler handCardHandler;

    [SerializeField] List<(Card, int)> combinationAttacks = new List<(Card, int)>();
    public List<Card> heroesInTurn = new List<Card>();
    Card cardToAttack = null;

    private int turn;

    private void Awake()
    {
        duelManager.duelPhase.OnValueChanged += OnDuelPhaseChanged;
        duelManager.OnChangeTurn += DuelManager_OnChangeTurn;
    }

    private void DuelManager_OnChangeTurn(object sender, EventArgs e)
    {
        if (!duelManager.IsSinglePlayer)
        {
            duelManager.duelPhase.OnValueChanged -= OnDuelPhaseChanged;
            duelManager.OnChangeTurn -= DuelManager_OnChangeTurn;
            return;
        }
        if (executeActionCoroutine != null)
            StopCoroutine(executeActionCoroutine);

        executeActionCoroutine = StartCoroutine(ExecuteAction());
    }

    /// <summary>
    /// Esta funcion se encarga de manejar el cambio de fase del duelo.
    /// </summary>
    /// <param name="previousValue"></param>
    /// <param name="newValue"></param>
    private void OnDuelPhaseChanged(DuelPhase previousValue, DuelPhase newValue)
    {
        if (!duelManager.IsSinglePlayer)
        {
            duelManager.duelPhase.OnValueChanged -= OnDuelPhaseChanged;
            duelManager.OnChangeTurn -= DuelManager_OnChangeTurn;
            return;
        }

        if (newValue == DuelPhase.Preparation)
        {
            turn++;
            StartCoroutine(PlanPlay());
        }
        else if (newValue == DuelPhase.Battle)
        {
            StartCoroutine(DefineActions());
        }
    }

    /// <summary>
    /// Esta funcion se encarga de planear las acciones del AI en el turno actual.
    /// </summary>
    /// <returns></returns>
    private IEnumerator PlanPlay()
    {
        if (turn == 1)
        {
            while (GetPlayableHeroes().Count > 0)
            {
                //Invocar Heroes.
                Card heroToPlay = ChoosingHeroToSummon(GetPlayableHeroes(), GameStrategy.Defensive);
                if (heroToPlay != null) SummonHero(heroToPlay, ChoosePositionFieldIndex(heroToPlay));

                yield return new WaitForSeconds(1);
            }

            StartCoroutine(DefineActions());
        }
        else
        {

            yield return new WaitForSeconds(1);

            StartCoroutine(DefineActions());
        }

        playerManager.SetPlayerReady();
    }

    /// <summary>
    /// Esta funcion se encarga de elegir los heroes que pueden ser invocados en el turno actual.
    /// </summary>
    /// <returns></returns>
    private List<Card> GetPlayableHeroes()
    {
        List<Card> usableHeroesCards = new List<Card>();

        foreach (var card in handCardHandler.GetCardInHandList())
        {
            if (card.cardSO is HeroCardSO && card.UsableCard(playerManager)) usableHeroesCards.Add(card);
        }

        return usableHeroesCards;
    }

    /// <summary>
    /// Esta funcion se encarga de elegir el mejor heroe para invocar en el turno actual.
    /// </summary>
    /// <param name="usableHeroesCards"></param>
    /// <param name="strategy"></param>
    /// <returns></returns>
    private Card ChoosingHeroToSummon(List<Card> usableHeroesCards, GameStrategy strategy)
    {
        Card heroToPlay = null;

        float bestScore = 0;
        foreach (var card in usableHeroesCards)
        {
            float score = EvaluarInvocacion(card);
            if (score > bestScore)
            {
                bestScore = score;
                heroToPlay = card;
            }
        }

        return heroToPlay;
    }

    /// <summary>
    /// Esta funcion se encarga de evaluar el potencial de invocacion de un heroe.
    /// </summary>
    /// <param name="heroCard"></param>
    /// <returns></returns>
    private float EvaluarInvocacion(Card heroCard)
    {
        float score = 0;

        //Evaluacion basica de estadisticas.
        score += heroCard.HealtPoint * 1.2f;
        score += heroCard.CurrentDefensePoints * 1.0f;
        score += heroCard.CurrentSpeedPoints * 0.8f;

        //Evaluacion del potencial de los movimientos.
        score += EvaluarMovimineto(heroCard.Moves[0]);
        score += EvaluarMovimineto(heroCard.Moves[1]);

        //Sinergias o afinidades especiales
        // score += EvaluarSinergia(heroCard); 

        //Penalizacion por coste.
        if (heroCard.cardSO is HeroCardSO heroCardSO)
        {
            score -= heroCardSO.Energy * 1.5f;
        }

        return score;
    }

    /// <summary>
    /// Esta funcion se encarga de evaluar el potencial de un movimiento.
    /// </summary>
    /// <param name="movement"></param>
    /// <returns></returns>
    private float EvaluarMovimineto(Movement movement)
    {
        float score = 0;

        //Daño del movimineto.
        score += movement.MoveSO.Damage;

        //Efectos adicionales
        if (movement.MoveSO.MoveEffect != null)
            score += movement.MoveSO.MoveEffect.effectScore;

        return score;
    }

    private void SummonHero(Card heroToPlay, int positionIndex)
    {
        duelManager.PlaceCardInField(playerManager, playerManager.isPlayer,
                    handCardHandler.GetIdexOfCard(heroToPlay), positionIndex);
    }

    /// <summary>
    /// Esta funcion se encarga de elegir la posicion en el campo donde se invocara el heroe.
    /// </summary>
    /// <param name="heroToPlay"></param>
    /// <returns></returns>
    private int ChoosePositionFieldIndex(Card heroToPlay)
    {
        List<FieldPosition> availablePositions = new List<FieldPosition>();

        foreach(var field in playerManager.GetFieldPositionList())
        {
            if(field.Card == null) availablePositions.Add(field);
        }

        if(heroToPlay.CurrentDefensePoints >= 40)
        {
            availablePositions.RemoveAll(p => p.PositionIndex > 4);
        }
        else if(heroToPlay.CurrentDefensePoints >= 20 && heroToPlay.CurrentDefensePoints < 40)
        {
            availablePositions.RemoveAll(p => p.PositionIndex < 5);
            availablePositions.RemoveAll(p => p.PositionIndex > 9);
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

    private IEnumerator DefineActions()
    {
        if (playerManager.GetAllCardInField().Count > 0)
        {
            var combination = GenerateMoveCombinations();
            cardToAttack = null;
            combinationAttacks.Clear();

            (int healt, int energyCost, int combIndex) combiData = (201, 200, -1);

            if(duelManager.duelPhase.Value == DuelPhase.Battle && duelManager.GetOpposingPlayerManager(playerManager).GetAllCardInField().Count == 0)
            {
                combinationAttacks = ChooseCombinations();

                if (executeActionCoroutine != null)
                    StopCoroutine(executeActionCoroutine);

                executeActionCoroutine = StartCoroutine(ExecuteAction());

                yield break;
            }
            else
            {
                foreach (Card card in duelManager.GetOpposingPlayerManager(playerManager).GetAllCardInField())
                {

                    var damage = 0;
                    var energy = 100;
                    var combIndex = -1;

                    for (int i = 0; i < combination.Count; i++)
                    {
                        var combData = GetDamageAndEnergyFromCombinationToHero(combination[i], card);
                        if (combData.damage > damage || (combData.damage == damage && combData.energy < energy))
                        {
                            damage = combData.damage;
                            energy = combData.energy;
                            combIndex = i;
                        }
                    }

                    var remainingDamage = card.CurrentHealtPoints + card.CurrentDefensePoints - damage;
                    if ((remainingDamage < combiData.healt || (combiData.healt == remainingDamage && energy < combiData.energyCost))
                        && damage > card.CurrentDefensePoints + (card.CurrentHealtPoints / 2))
                    {
                        combiData = (remainingDamage, energy, combIndex);
                        cardToAttack = card;
                    }
                }
            }

            foreach (var card in playerManager.GetAllCardInField())
            {
                combinationAttacks.Add((card, 2));
            }

            if (playerManager.PlayerEnergy >= combiData.energyCost)
            {
                combinationAttacks.Clear();
                Debug.Log($"Atacar con la combinacion {combiData.combIndex}, al objetivo en la pocion {cardToAttack.FieldPosition.PositionIndex}");
                combination[combiData.combIndex].ForEach(x => combinationAttacks.Add(x));
            }
            
            if (combiData.combIndex == -1 && duelManager.duelPhase.Value == DuelPhase.Preparation)
            {
                Debug.Log("Intentar invocar un heroe para tener mas fuerza de ataque");

                Card heroToPlay = ChoosingHeroToSummon(GetPlayableHeroes(), GameStrategy.Defensive);

                if (heroToPlay != null) 
                { 
                    SummonHero(heroToPlay, ChoosePositionFieldIndex(heroToPlay));
                    yield return new WaitForSeconds(Random.Range(2, 3));
                    StartCoroutine(DefineActions());
                    yield break;
                }
            }

            if(duelManager.duelPhase.Value == DuelPhase.Battle)
            {
                if (executeActionCoroutine != null)
                    StopCoroutine(executeActionCoroutine);
                executeActionCoroutine = StartCoroutine(ExecuteAction());
            }
                
        }
        else if (duelManager.duelPhase.Value == DuelPhase.Preparation)
        {
            Debug.Log("Intentar invocar un heroe");

            Card heroToPlay = ChoosingHeroToSummon(GetPlayableHeroes(), GameStrategy.Defensive);

            if (heroToPlay != null)
            {
                SummonHero(heroToPlay, ChoosePositionFieldIndex(heroToPlay));
                yield return new WaitForSeconds(1);
                StartCoroutine(DefineActions());
                yield break;
            }
        }
    }

    Coroutine executeActionCoroutine;
    private IEnumerator ExecuteAction()
    {
        yield return new WaitForSeconds(1);
        heroesInTurn.Clear();// Limpia la lista 

        foreach (var card in duelManager.HeroInTurn)
        {
            if(playerManager.GetAllCardInField().Contains(card) && !card.IsControlled()) heroesInTurn.Add(card); // Agrega a la lista los heroes que deben realizar acciones en este turno
        }

        foreach (var card in heroesInTurn)// Ejecutar accion predefinida para cade heroe
        {
            foreach (var (hero, attack) in combinationAttacks)
            {
                if(hero == card)
                {

                    yield return new WaitForSeconds(1);

                    if (card.Moves[attack].MoveSO.NeedTarget)
                    {
                        if(cardToAttack != null)
                        {
                            if(cardToAttack.FieldPosition != null)
                            {
                                duelManager.UseMovement(attack, card, cardToAttack.FieldPosition.PositionIndex);
                            }
                            else
                            {
                                StartCoroutine(DefineActions());
                                yield break;
                            }
                        }
                        else
                        {
                            duelManager.UseMovement(attack, card, -1);
                        }
                    }
                    else
                    {
                        duelManager.UseMovement(attack, card);
                    }
                    continue;
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

        heroes.RemoveAll(hero => hero.IsControlled()); // Eliminar heroes bajo efecto de control

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

    private (int damage, int energy) GetDamageAndEnergyFromCombinationToHero(List<(Card Hero, int Attack)> values, Card hero)
    {
        int damage = 0;
        int energy = 0;


        foreach ((Card Hero, int Attack) in values)
        {
            if(duelManager.ObtainTargets(Hero, Attack).Contains(hero))
            {
                damage += Hero.Moves[Attack].MoveSO.Damage - hero.GetDamageAbsorbed();
            }

            energy += Hero.Moves[Attack].MoveSO.EnergyCost;
        }

        return (damage, energy);
    }
}
