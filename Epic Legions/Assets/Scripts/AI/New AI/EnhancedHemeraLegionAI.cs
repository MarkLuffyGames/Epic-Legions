using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

enum AIDifficulty
{
    Easy,
    Normal,
    Hard,
    Nightmare
}
public class EnhancedHemeraLegionAI : MonoBehaviour
{
    [Header("Card Playing AI")]
    [SerializeField] private bool enableCardPlayingAI = true;
    [SerializeField] private float cardDecisionDelay = 0.5f;
    [SerializeField] private int maxCardsToPlayPerTurn = 1;
    [SerializeField] private float minimumCardScoreToPlay = 10f;

    
    private int cardsPlayedThisTurn = 0;
    private bool isPlayingCard = false;
    private CardPlacementEvaluator placementEvaluator;

    [Header("AI Configuration")]
    [SerializeField] private bool enableEnhancedAI = true;
    [SerializeField] private float decisionDelay = 0.5f;

    [Header("Performance Settings")]
    [SerializeField] private float maxMillisecondsPerFrame = 4f;
    [SerializeField] private float maxCombinationsPerTurn = Mathf.Infinity;

    // Estado de la corrutina de IA
    private Coroutine aiDecisionCoroutine;
    private bool isAITakingDecision = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showDebugDeepLogs = false;

    [SerializeField] private DuelManager duelManager;
    [SerializeField] private PlayerManager aiPlayerManager;
    [SerializeField] private PlayerManager humanPlayerManager;
    private EnhancedAISimulation simulation;
    private MovementSimulator movementSimulator;
    private PlanGenerator planGenerator;
    HeroValueEvaluator heroValueEvaluator;
    HeroExposureEvaluator heroExposureEvaluator;
    ProtectionDecisionMaker protectionDecisionMaker;
    private void Awake()
    {
        duelManager.OnStartSinglePlayerDuel += OnDuelStarted;
    }

    private void OnDuelStarted(object sender, EventArgs e)
    {
        bool isplayer1 = aiPlayerManager.isPlayer;

        if (isplayer1)
        {
            enableCardPlayingAI = PlayerPrefs.GetInt("Player1Control", 0) == 1;
            enableEnhancedAI = PlayerPrefs.GetInt("Player1Control", 0) == 1;

            showDebugLogs = PlayerPrefs.GetInt("Player1AIDebug", 0) == 1;
            showDebugDeepLogs = PlayerPrefs.GetInt("Player1AIDebugDeep", 0) == 1;
        }
        else
        {
            enableCardPlayingAI = true;
            enableEnhancedAI = true;

            showDebugLogs = PlayerPrefs.GetInt("Player2AIDebug", 0) == 1;
            showDebugDeepLogs = PlayerPrefs.GetInt("Player2AIDebugDeep", 0) == 1;
        }

        simulation = new EnhancedAISimulation(aiPlayerManager, humanPlayerManager, duelManager, showDebugLogs);
        movementSimulator = new MovementSimulator(showDebugLogs, showDebugDeepLogs);
        planGenerator = new PlanGenerator(showDebugLogs, showDebugDeepLogs, movementSimulator);
        heroValueEvaluator = new HeroValueEvaluator();
        heroExposureEvaluator = new HeroExposureEvaluator();
        protectionDecisionMaker = new ProtectionDecisionMaker();

        // Suscribirse a eventos del duelo
        duelManager.duelPhase.OnValueChanged += OnDuelPhaseChanged;
        duelManager.OnChangeTurn += OnTurnChanged;

        Log("IA inicializada");
    }

    private void OnDuelPhaseChanged(DuelPhase previousValue, DuelPhase newValue)
    {
        if (duelManager.GetCurrentDuelPhase() == DuelPhase.Preparation)
        {
            cardsPlayedThisTurn = 0;
            HandlePreparationPhase();
        }
    }

    private void HandlePreparationPhase()
    {
        if (!enableCardPlayingAI) return;

        Log("🎴 IA evaluando jugar cartas en fase de preparación...");

        AnalyzePreparationPhase();

        // Verificar si es el turno de la IA para jugar cartas
        if (!IsItAITurnInPreparation())
        {
            Log("No es el turno de la IA para jugar cartas o no hay cartas disponibles");

            PassTurn();
            return;
        }

        // Iniciar proceso de decisión
        StartCoroutine(DecideCardPlay());
    }

    private bool IsItAITurnInPreparation()
    {
        // En single player, la IA es siempre el jugador 2
        // Verificamos que estemos en fase de preparación
        if (duelManager.GetCurrentDuelPhase() != DuelPhase.Preparation)
            return false;

        // Verificar que la IA tenga energía y cartas
        if (aiPlayerManager.PlayerEnergy <= 0)
            return false;

        if (aiPlayerManager.GetHandCardHandler().GetCardInHandList().Count == 0)
            return false;

        return true;
    }

    private void AnalyzePreparationPhase()
    {
        LogDeep("=== [AI] ANALYZE PREPARATION PHASE ===");

        foreach (var hero in aiPlayerManager.GetAllCardInField())
        {
            if (hero == null)
                continue;

            var heroSO = hero.cardSO as HeroCardSO;

            int pos = hero.FieldPosition.PositionIndex;
            int row = pos / 5;
            int col = pos % 5;

            double value = heroValueEvaluator.EvaluateHeroValue(
                hero,
                aiPlayerManager,
                humanPlayerManager);

            ExposureLevel exposure = heroExposureEvaluator.EvaluateExposure(
                hero,
                aiPlayerManager,
                humanPlayerManager);

            ProtectionActionType protection =
                protectionDecisionMaker.DecideProtection(
                    hero,
                    exposure,
                    value,
                    aiPlayerManager.PlayerEnergy);

            LogDeep(
                $"[AI][Hero Analysis] {heroSO.CardName} | " +
                $"Pos: (R{row},C{col}) | " +
                $"Value: {value:F1} | " +
                $"Exposure: {exposure} | " +
                $"Decision: {protection}"
            );
        }

        LogDeep("=== [AI] END ANALYSIS ===");
    }


    private IEnumerator DecideCardPlay()
    {
        if (isPlayingCard) yield break;

        isPlayingCard = true;

        try
        {
            // Evaluar si debería jugar una carta
            if (ShouldPlayCardThisTurn())
            {
                var bestPlay = FindBestCardToPlay();

                if (bestPlay.card != null && bestPlay.score >= minimumCardScoreToPlay)
                {
                    Log($"🎯 IA decide jugar: {bestPlay.card.cardSO.CardName} en posición {bestPlay.position} (score: {bestPlay.score:F1})");

                    yield return new WaitForSeconds(cardDecisionDelay);
                    PlayHeroCard(bestPlay.card, bestPlay.position);
                    cardsPlayedThisTurn++;
                }
                else
                {
                    Log($"⏭️ IA decide pasar (mejor score: {bestPlay.score:F1}, mínimo requerido: {minimumCardScoreToPlay})");

                    yield return new WaitForSeconds(cardDecisionDelay);
                    PassTurn();
                }
            }
            else
            {
                Log("⏭️ IA decide pasar (no debe jugar carta este turno)");

                yield return new WaitForSeconds(cardDecisionDelay);
                PassTurn();
            }
        }
        finally
        {
            isPlayingCard = false;
        }
    }

    private bool ShouldPlayCardThisTurn()
    {
        // Verificar límite de cartas por turno
        if (cardsPlayedThisTurn >= maxCardsToPlayPerTurn)
            return false;

        // Verificar energía mínima
        int minEnergyNeeded = GetMinimumEnergyForAnyCard();
        if (aiPlayerManager.PlayerEnergy < minEnergyNeeded)
            return false;

        // Verificar espacio en el campo
        int availableSlots = CountAvailableFieldSlots();
        if (availableSlots <= 0)
            return false;

        return true;
    }

    private (Card card, int position, double score) FindBestCardToPlay()
    {
        var hand = aiPlayerManager.GetHandCardHandler().GetCardInHandList();
        Card bestCard = null;
        int bestPosition = -1;
        double bestScore = 0;

        if (placementEvaluator == null)
            placementEvaluator = new CardPlacementEvaluator(showDebugLogs);

        foreach (var card in hand)
        {
            if (card.cardSO is HeroCardSO heroSO)
            {
                // Verificar si podemos pagar el costo
                if (heroSO.Energy > aiPlayerManager.PlayerEnergy)
                    continue;

                // Evaluar la carta
                double cardScore = EvaluateHeroCard(heroSO);

                // Encontrar mejor posición para esta carta
                var (position, positionScore) = placementEvaluator.EvaluateBestPositionForHero(heroSO, aiPlayerManager);

                if (position == -1) continue; // No hay posición disponible

                double totalScore = cardScore + positionScore;

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestCard = card;
                    bestPosition = position;
                }

                LogDeep($"   {heroSO.CardName}: Score={totalScore:F1} (carta={cardScore:F1}, posición={positionScore:F1})");
            }
        }

        return (bestCard, bestPosition, bestScore);
    }

    private double EvaluateHeroCard(HeroCardSO hero)
    {
        double score = 0;

        // 1. Estadísticas básicas
        score += hero.Health * 0.8;      // HP es importante
        score += hero.Defense * 0.6;     // Defensa moderadamente importante
        score += hero.Speed * 1.2;       // Velocidad muy importante

        // 2. Costo de energía (penalización)
        score -= hero.Energy * 1.5;

        // 3. Evaluar movimientos
        foreach (var move in hero.Moves)
        {
            // Daño
            score += move.Damage * 0.3;

            // Efectos especiales
            if (move.MoveEffect != null)
            {
                score += 5; // Bonus base por tener efecto

                // Bonus adicional por efectos poderosos
                if (move.MoveEffect is HeroControl) score += 10;
                if (move.MoveEffect is Recharge) score += 8;
                if (move.MoveEffect is Heal) score += 6;
            }
        }

        // 4. Bonus por clase
        switch (hero.HeroClass)
        {
            case HeroClass.Hunter:
                score += 15; // Muy valioso por ataque a distancia
                break;
            case HeroClass.Assassin:
                score += 10; // Valioso por flanqueo
                break;
            default:
                score += 8;  // Bonus base para otras clases
                break;
        }

        return score;
    }

    private int GetMinimumEnergyForAnyCard()
    {
        int minEnergy = int.MaxValue;
        var hand = aiPlayerManager.GetHandCardHandler().GetCardInHandList();

        foreach (var card in hand)
        {
            if (card.cardSO is HeroCardSO hero)
            {
                minEnergy = Mathf.Min(minEnergy, hero.Energy);
            }
        }

        return minEnergy == int.MaxValue ? 0 : minEnergy;
    }

    private int CountAvailableFieldSlots()
    {
        int occupied = 0;
        var fieldPositions = aiPlayerManager.GetFieldPositionList();

        foreach (var pos in fieldPositions)
        {
            if (pos.Card != null)
                occupied++;
        }

        return fieldPositions.Count - occupied;
    }

    private int GetCardIndexInHand(Card card)
    {
        var hand = aiPlayerManager.GetHandCardHandler().GetCardInHandList();
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] == card)
                return i;
        }
        return -1;
    }

    private void PlayHeroCard(Card card, int position)
    {
        if (card == null) return;

        int cardIndex = GetCardIndexInHand(card);
        if (cardIndex == -1)
        {
            Debug.LogError($"No se encontró la carta {card.cardSO.CardName} en la mano de la IA");
            return;
        }

        Log($"▶️ IA jugando {card.cardSO.CardName} en posición {position}");

        duelManager.PlaceCardInField(aiPlayerManager, false, cardIndex, position);

        // Programar siguiente decisión (pasar turno o jugar otra carta)
        Invoke(nameof(ContinuePreparationPhase), 0.5f);
    }

    private void ContinuePreparationPhase()
    {
        // Después de jugar una carta, decidir si jugar otra o pasar
        if (cardsPlayedThisTurn < maxCardsToPlayPerTurn && ShouldPlayCardThisTurn())
        {
            StartCoroutine(DecideCardPlay());
        }
        else
        {
            PassTurn();
        }
    }

    private void PassTurn()
    {
        Log("⏭️ IA pasando turno en fase de preparación");

        // Resetear contador para el próximo turno
        cardsPlayedThisTurn = 0;

        // Marcar como listo para avanzar de fase
        aiPlayerManager.isReady = true;
        duelManager.SetPlayerReadyAndTransitionPhase();
    }

    private void OnTurnChanged(object sender, System.EventArgs e)
    {
        if (!enableEnhancedAI) return;

        // Verificar si es el turno de la IA
        if (IsAITurn())
        {
            Log("Turno de IA detectado, programando decisión...");

            // Programar la decisión con un pequeño delay
            Invoke(nameof(MakeAIDecision), decisionDelay);
        }
    }

    private bool IsAITurn()
    {
        // Verificar si algún héroe controlado por la IA está en turno
        foreach (var hero in duelManager.HeroInTurn)
        {
            if (IsHeroControlledByAI(hero))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsHeroControlledByAI(Card hero)
    {
        var manager = duelManager.GetPlayerManagerForCard(hero);
        if (manager == aiPlayerManager && !hero.IsControlled())
            return true;

        if (hero.IsControlled())
            return true;
        

        return false;
    }

    private void MakeAIDecision()
    {
        if (!enableEnhancedAI || duelManager.GetCurrentDuelPhase() != DuelPhase.Battle)
            return;

        // Cancelar decisión anterior si está en curso
        if (isAITakingDecision && aiDecisionCoroutine != null)
        {
            StopCoroutine(aiDecisionCoroutine);
            Log("🔄 Cancelando decisión anterior de IA...");
        }

        Log("🧠 IA iniciando decisión...");

        // Iniciar nueva decisión asíncrona
        aiDecisionCoroutine = StartCoroutine(MakeAIDecisionTimeSliced());
        
    }

    private IEnumerator MakeAIDecisionTimeSliced()
    {
        isAITakingDecision = true;

        try
        {
            // 1. Obtener TODOS los héroes de IA que NO han actuado en todo el turno
            var allPendingAIHeroes = GetAllAIPendingHeroes();

            if (allPendingAIHeroes.Count == 0)
            {
                Log("No hay héroes de IA pendientes por actuar en todo el turno, pasando...");

                isAITakingDecision = false;
                yield break;
            }

            // 2. Los héroes que deben actuar EN ESTE SUBTURNO son heroInTurn
            var heroesInThisSubTurn = duelManager.HeroInTurn.Where(IsHeroControlledByAI).ToList();

            Log($"Héroes pendientes en todo el turno: {allPendingAIHeroes.Count}");
            Log($"Héroes que actúan en ESTE subturno: {heroesInThisSubTurn.Count}");

            // 3. Construir snapshot del estado actual
            var currentSnapshot = simulation.BuildSimSnapshot();

            // 4. Generar y evaluar combinaciones con TODOS los héroes pendientes
            //var bestPlan = FindBestPlan(currentSnapshot, allPendingAIHeroes);
            FullPlanSim bestPlan = null;
            yield return StartCoroutine(FindBestPlan(currentSnapshot, allPendingAIHeroes,
                (plan) => {
                    bestPlan = plan;
                }));

            // 5. Ejecutar solo las acciones de los héroes de ESTE subturno
            if (bestPlan != null)
            {
                ExecutePlanForCurrentSubTurn(bestPlan, heroesInThisSubTurn);
            }
            else
            {
                Debug.LogError("No se encontró plan viable...");
            }
        }
        finally
        {
            isAITakingDecision = false;
        }
    }

    private List<Card> GetAllAIPendingHeroes()
    {
        var allPendingHeroes = new List<Card>();

        // Revisar TODOS los héroes en el campo de la IA
        var allAIFieldHeroes = aiPlayerManager.GetAllCardInField();

        foreach (var hero in allAIFieldHeroes)
        {
            if (IsHeroControlledByAI(hero) && !hero.turnCompleted && hero.CurrentHealthPoints > 0)
            {
                allPendingHeroes.Add(hero);
            }
        }

        // También incluir héroes controlados del rival que no han actuado
        var allEnemyFieldHeroes = humanPlayerManager.GetAllCardInField()
            .Where(h => h.cardSO is HeroCardSO && h.IsControlled())
            .ToList();

        foreach (var hero in allEnemyFieldHeroes)
        {
            if (!hero.turnCompleted && hero.CurrentHealthPoints > 0)
            {
                allPendingHeroes.Add(hero);
            }
        }

        LogDeep($"Héroes IA pendientes encontrados: {allPendingHeroes.Count}");
        foreach (var hero in allPendingHeroes)
        {
            LogDeep($"  - {hero.cardSO.CardName} (turnCompleted: {hero.turnCompleted})");
        }

        return allPendingHeroes;
    }


    private void ExecutePlanForCurrentSubTurn(FullPlanSim plan, List<Card> heroesInThisSubTurn)
    {
        Log($"EJECUTANDO PLAN COMPLETO - {plan.Actions.Count} acciones totales");
        Log($"Héroes en subturno actual: {heroesInThisSubTurn.Count}");

        // Mostrar plan completo con información clara
        foreach (var (hero, moveIndex, targetPosition) in plan.Actions)
        {
            var move = hero.moves[moveIndex];
            string targetInfo = GetTargetInfoString(move, targetPosition);
            Log($"  - {hero.OriginalCard.cardSO.CardName} -> {move.MoveSO.MoveName} {targetInfo}");
        }

        int actionsExecuted = 0;

        foreach (var heroInTurn in heroesInThisSubTurn)
        {
            var plannedAction = plan.Actions.FirstOrDefault(a => a.hero.OriginalCard == heroInTurn);

            if (plannedAction.hero != null && plannedAction.moveIndex >= 0)
            {
                if (IsMoveUsable(plannedAction.hero.OriginalCard, plannedAction.moveIndex))
                {
                    // Log claro de lo que se va a ejecutar
                    var move = plannedAction.hero.moves[plannedAction.moveIndex];
                    string actionType = move.MoveSO.NeedTarget ?
                        (plannedAction.targetPosition == -1 ? "ATAQUE DIRECTO A VIDA" : "ATAQUE A OBJETIVO") :
                        "EFECTO AUTO-APLICADO";

                    Log($"Ejecutando: {heroInTurn.cardSO.CardName} -> {move.MoveSO.MoveName} [{actionType}]");

                    duelManager.UseMovement(plannedAction.moveIndex, plannedAction.hero.OriginalCard, plannedAction.targetPosition);
                    actionsExecuted++;
                }
            }
        }

        Log($"Acciones ejecutadas en este subturno: {actionsExecuted}");
    }

    private IEnumerator FindBestPlan(SimSnapshot snapshot, List<Card> aiHeroes, Action<FullPlanSim> onComplete)
    {
        yield return null; // Esperar un frame para no bloquear
        List<SimCardState> aiHeroesStates = aiHeroes
            .Select(hero => snapshot.CardStates[hero])
            .ToList();

        FullPlanSim bestPlan = planGenerator.GenerateBestPlan(snapshot);

        Log($"🧪 Plan encontrado con score {bestPlan.Score:F2} y {bestPlan.Actions.Count} acciones:");
        Log(GetCombinationString(bestPlan.Actions));
        onComplete?.Invoke(bestPlan);
    }

    private string GetCombinationString(List<(SimCardState hero, int moveIndex, int targetPosition)> combination)
    {
        string result = "";
        foreach (var (hero, moveIndex, targetPosition) in combination)
        {
            var move = hero.moves[moveIndex];
            string moveName = move.MoveSO.MoveName;
            string targetInfo = move.MoveSO.NeedTarget ?
                (targetPosition == -1 ? "→[VIDA]" : $"→[POS{targetPosition}]") : "→[AUTO]";

            result += $"{hero.OriginalCard.cardSO.CardName}:{moveName}{targetInfo} ";
        }
        return result.Trim();
    }

    private string GetTargetInfoString(Movement move, int targetPosition)
    {
        var moveSO = move.MoveSO;

        if (!moveSO.NeedTarget)
        {
            return "(auto-aplicado)";
        }
        else if (targetPosition == -1)
        {
            return "→ [ATAQUE DIRECTO A VIDA]";
        }
        else
        {
            return $"→ objetivo posición {targetPosition}";
        }
    }

    private bool IsMoveUsable(Card hero, int moveIndex)
    {
        var move = hero.Moves[moveIndex];

        // Verificar energía
        if (move.MoveSO.EnergyCost > aiPlayerManager.PlayerEnergy || aiPlayerManager.FreeAbilityCost())
        {
            LogDeep($"Movimiento {moveIndex} de {hero.cardSO.CardName} requiere {move.MoveSO.EnergyCost} energía, pero solo hay {aiPlayerManager.PlayerEnergy}");
            return false;
        }

        // Verificar si el héroe puede actuar
        if (hero.IsActionBlocked())
        {
            LogDeep($"Héroe {hero.cardSO.CardName} no puede actuar (bloqueado)");
            return false;
        }

        // Para movimientos que necesitan objetivo, verificar si hay objetivos
        if (move.MoveSO.NeedTarget)
        {
            var targets = duelManager.ObtainTargets(hero, moveIndex);
            if (targets.Count == 0 && humanPlayerManager.GetAllCardInField().Count > 0)
            {
                LogDeep($"Movimiento {moveIndex} de {hero.cardSO.CardName} no tiene objetivos disponibles");
                return false;
            }
        }

        return true;
    }

    private void OnDestroy()
    {
        if (duelManager != null)
        {
            duelManager.OnChangeTurn -= OnTurnChanged;
        }
    }

    public void Log(string message)
    {
        if (showDebugLogs) Debug.Log(message);
    }

    public void LogDeep(string message)
    {
        if (showDebugDeepLogs) Debug.Log(message);
    }
}