using IngameDebugConsole;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public enum GameStrategy { Defensive, Offensive, Balanced }
public enum AIDifficulty { Easy, Normal, Hard, Nightmare }

public class HemeraLegionAI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private DuelManager duelManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private HandCardHandler handCardHandler;

    [Header("Dificultad")]
    [SerializeField] private AIDifficulty difficulty = AIDifficulty.Normal;

    // ---- Internos de dificultad (no mostrar en inspector) ----
    // Copias "base" para poder re-aplicar perfiles sin acumular escalados:
    private Vector2 _basePlayCardDelayRange;
    private Vector2 _baseEnqueueMoveDelayRange;
    private int _baseEnergySoftTarget;
    private float _baseSoftOverflowPenaltyPerPoint;
    private float _baseOverkillPenaltyFactor;
    private float _baseMinHpDamagePercent;

    // Parámetros dinámicos por dificultad:
    private float _delayMul = 1.0f;
    private float _mistakeProb = 0f;         // prob. de elegir un movimiento "peor" intencionalmente
    private float _randomTargetProb = 0f;     // prob. de cambiar el target elegido por uno aleatorio legal
    private float _skipPlayProb = 0f;         // prob. de saltarse una jugada en preparación
    private int _combHardCap = 0;             // recorte de combinaciones (0 = sin límite)

    [Header("Debug")]
    [SerializeField] private bool debugAI = true;
    [SerializeField] private bool debugDeep = false;

    [Header("Energía / Economía")]
    [Tooltip("Objetivo blando: preferimos terminar la fase de batalla con <=50 (luego +50, cap 100).")]
    [SerializeField] private int energySoftTarget = 50;
    [Tooltip("Penalización SUAVE por cada punto de energía por encima del objetivo.")]
    [SerializeField] private float softOverflowPenaltyPerPoint = 5f;

    [Header("Daño / Objetivos")]
    [Tooltip("Penalización por 'overkill' (daño sobrante) en letales.")]
    [SerializeField] private float overkillPenaltyFactor = 0.5f;

    [Header("Umbral de vida")]
    [Tooltip("Umbral mínimo de daño a VIDA (fracción de la vida ACTUAL): 0.35 = 35%.")]
    [Range(0f, 1f)] public float minHpDamagePercent = 0.35f;

    [Header("Delays / Ritmo humano")]
    [SerializeField] private Vector2 playCardDelayRange = new Vector2(0.35f, 0.8f);
    [SerializeField] private Vector2 enqueueMoveDelayRange = new Vector2(0.10f, 0.25f);

    [Header("Slicing de cómputo")]
    [SerializeField] private int combosYieldEvery = 4;

    private List<(Card, int)> combinationAttacks = new List<(Card, int)>();
    private Card cardToAttack = null;
    private int turn;

    private bool isExecuting = false;
    private bool isBusy = false;

    private const int RECHARGE_INDEX = 2; // Recarga SIEMPRE índice 2

    // ---------- logging helpers ----------
    private void Log(string msg) { if (debugAI) Debug.Log($"[HemeraAI] {msg}"); }
    private void LogDeep(string msg) { if (debugAI && debugDeep) Debug.Log($"[HemeraAI:Deep] {msg}"); }
    private string CardName(Card c) => c ? c.cardSO.CardName : "<null>";
    private string MoveName(Card c, int i) { try { return c?.Moves[i]?.MoveSO?.MoveName ?? $"move#{i}"; } catch { return $"move#{i}"; } }
    private string CombToStr(List<(Card, int)> comb) => comb == null || comb.Count == 0 ? "<vacío>" : string.Join(" | ", comb.Select(p => $"{CardName(p.Item1)}[{p.Item2}:{MoveName(p.Item1, p.Item2)}]"));

    private void Awake()
    {
        if (playerManager.isPlayer)
        {
            if(PlayerPrefs.GetInt("isPlayer", 1) == 1)
            {
                return;
            }
            debugAI = PlayerPrefs.GetInt("Player1AIDebug", 0) == 1;
            debugDeep = PlayerPrefs.GetInt("Player1AIDebugDeep", 0) == 1;
            difficulty = Enum.Parse<AIDifficulty>(PlayerPrefs.GetString("AIDifficulty", AIDifficulty.Normal.ToString()));
        }
        else
        {
            debugAI = PlayerPrefs.GetInt("Player2AIDebug", 1) == 1;
            debugDeep = PlayerPrefs.GetInt("Player2AIDebugDeep", 0) == 1;
            difficulty = Enum.Parse<AIDifficulty>(PlayerPrefs.GetString("AIDifficulty2", AIDifficulty.Normal.ToString()));
        }

        // Guardar "base" para poder cambiar dificultad en runtime sin acumulaciones:
        _basePlayCardDelayRange = playCardDelayRange;
        _baseEnqueueMoveDelayRange = enqueueMoveDelayRange;
        _baseEnergySoftTarget = energySoftTarget;
        _baseSoftOverflowPenaltyPerPoint = softOverflowPenaltyPerPoint;
        _baseOverkillPenaltyFactor = overkillPenaltyFactor;
        _baseMinHpDamagePercent = minHpDamagePercent;

        ApplyDifficultyTuning(difficulty);

        duelManager.duelPhase.OnValueChanged += OnDuelPhaseChanged;
        duelManager.OnChangeTurn += DuelManager_OnChangeTurn;
        Log($"Awake: subscrito a eventos. Dificultad={difficulty}");
    }

    public void SetDifficulty(AIDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
        ApplyDifficultyTuning(difficulty);
        Log($"Dificultad cambiada en runtime a {difficulty}");
    }

    private void ApplyDifficultyTuning(AIDifficulty d)
    {
        // Resetear a base
        playCardDelayRange = _basePlayCardDelayRange;
        enqueueMoveDelayRange = _baseEnqueueMoveDelayRange;
        energySoftTarget = _baseEnergySoftTarget;
        softOverflowPenaltyPerPoint = _baseSoftOverflowPenaltyPerPoint;
        overkillPenaltyFactor = _baseOverkillPenaltyFactor;
        minHpDamagePercent = _baseMinHpDamagePercent;

        _delayMul = 1.0f;
        _mistakeProb = 0f;
        _randomTargetProb = 0f;
        _skipPlayProb = 0f;
        _combHardCap = 0;

        switch (d)
        {
            case AIDifficulty.Easy:
                // estrategia más conservadora/errática
                energySoftTarget = Mathf.Clamp(energySoftTarget + 10, 0, 100);
                softOverflowPenaltyPerPoint *= 0.7f;
                minHpDamagePercent = Mathf.Clamp01(minHpDamagePercent + 0.10f);

                // errores controlados
                _mistakeProb = 0.25f;
                _randomTargetProb = 0.15f;
                _skipPlayProb = 0.20f;

                // recorte de combinaciones para simplificar
                _combHardCap = 120;
                break;

            case AIDifficulty.Normal:
                _mistakeProb = 0.05f;
                _randomTargetProb = 0.05f;
                _skipPlayProb = 0.05f;
                // sin cambios al resto
                break;

            case AIDifficulty.Hard:
                energySoftTarget = Mathf.Clamp(energySoftTarget - 10, 0, 100);
                softOverflowPenaltyPerPoint *= 1.2f;
                minHpDamagePercent = Mathf.Clamp01(minHpDamagePercent - 0.05f);

                _mistakeProb = 0f;
                _randomTargetProb = 0f;
                _skipPlayProb = 0f;
                _combHardCap = 0;
                break;

            case AIDifficulty.Nightmare:
                energySoftTarget = Mathf.Clamp(energySoftTarget - 15, 0, 100);
                softOverflowPenaltyPerPoint *= 1.35f;
                minHpDamagePercent = Mathf.Clamp01(minHpDamagePercent - 0.08f);

                _mistakeProb = 0f;
                _randomTargetProb = 0f;
                _skipPlayProb = 0f;
                _combHardCap = 0;
                break;
        }

        LogDeep($"[Diff] delayMul={_delayMul}, mistake={_mistakeProb}, randomTarget={_randomTargetProb}, skipPlay={_skipPlayProb}, combCap={_combHardCap}");
    }


    private void OnDestroy()
    {
        duelManager.duelPhase.OnValueChanged -= OnDuelPhaseChanged;
        duelManager.OnChangeTurn -= DuelManager_OnChangeTurn;
        Log("OnDestroy: desuscrito de eventos.");
    }

    // --------- EVENTOS ---------
    private void DuelManager_OnChangeTurn(object sender, EventArgs e)
    {
        if (!duelManager.IsSinglePlayer)
        {
            Log("OnChangeTurn: no-singleplayer -> detengo IA.");
            duelManager.duelPhase.OnValueChanged -= OnDuelPhaseChanged;
            duelManager.OnChangeTurn -= DuelManager_OnChangeTurn;
            return;
        }

        if (!HasAIHeroInCurrentSubturn())
        {
            Log("OnChangeTurn: no hay héroes de IA en el subturno actual -> no recalculo.");
            return;
        }

        if (isBusy || isExecuting)
        {
            Log("OnChangeTurn: busy/executing -> ignoro (evita bucle).");
            return;
        }

        StartCoroutine(DefineThenExecuteForCurrentSubturn());
    }

    private IEnumerator DefineThenExecuteForCurrentSubturn()
    {
        if (isBusy) yield break;
        isBusy = true;

        Log("== Subturno IA: planificación y ejecución ==");
        yield return StartCoroutine(WaitUntilHeroInTurnReady());
        if (!HasAIHeroInCurrentSubturn())
        {
            Log("Tras espera: no hay héroes IA en este subturno -> salgo.");
            isBusy = false;
            yield break;
        }

        yield return StartCoroutine(DefineActionsForCurrentSubturn());
        yield return StartCoroutine(ExecuteActionForCurrentSubturn());

        isBusy = false;
        Log("== Fin subturno IA ==");
    }

    private IEnumerator WaitUntilHeroInTurnReady()
    {
        int guard = 0;
        while ((duelManager.HeroInTurn == null || duelManager.HeroInTurn.Count == 0) && guard < 10)
        {
            Log($"Esperando HeroInTurn (intento {guard + 1})...");
            guard++;
            yield return null;
        }
        Log($"HeroInTurn listo? {(duelManager.HeroInTurn != null ? duelManager.HeroInTurn.Count : 0)}");
    }

    private void OnDuelPhaseChanged(DuelPhase prev, DuelPhase now)
    {
        if (!duelManager.IsSinglePlayer)
        {
            Log("OnDuelPhaseChanged: no-singleplayer.");
            duelManager.duelPhase.OnValueChanged -= OnDuelPhaseChanged;
            duelManager.OnChangeTurn -= DuelManager_OnChangeTurn;
            return;
        }

        Log($"Fase cambió: {prev} -> {now}");
        if (now == DuelPhase.Preparation)
        {
            turn++;
            Log($"== PREPARATION (Turno {turn}) ==");
            StartCoroutine(PlayCardsHand());
        }
    }

    // -------------------- PREPARATION --------------------
    private IEnumerator PlayCardsHand()
    {
        Log($"Turno AI: {turn} | Energía: {playerManager.PlayerEnergy}");

        // Delay de entrada a la fase de preparación (ajustado por dificultad)
        yield return new WaitForSeconds(Random.Range(playCardDelayRange.x, playCardDelayRange.y));

        while (NeedsMoreHeroes() && GetPlayableHeroes().Count > 0)
        {
            // posibilidad de "saltarse" jugada en dificultades bajas
            if (Chance(_skipPlayProb))
            {
                LogDeep("[Diff] Salto invocación por dificultad.");
                break;
            }

            yield return new WaitForSeconds(Random.Range(playCardDelayRange.x, playCardDelayRange.y));
            if (!PlayHeroCard()) break;
        }

        while (GetPlayableEquipment().Count > 0)
        {
            if (Chance(_skipPlayProb))
            {
                LogDeep("[Diff] Salto equipamiento por dificultad.");
                break;
            }

            yield return new WaitForSeconds(Random.Range(playCardDelayRange.x, playCardDelayRange.y));
            PlayEquipmentCard();
        }

        // Pausa breve antes de ready
        yield return new WaitForSeconds(Random.Range(playCardDelayRange.x * 0.5f, playCardDelayRange.y * 0.5f));

        Log("Preparation listo -> PlayerReady()");
        playerManager.SetPlayerReady();
    }

    private bool PlayHeroCard()
    {
        var heroes = GetPlayableHeroes();
        Card heroToPlay = ChoosingHeroToSummon(heroes);
        if (heroToPlay != null && playerManager.GetFieldPositionList().Any(f => f.Card == null))
        {
            int pos = ChoosePositionFieldIndex(heroToPlay, false);
            Log($"Invocando {CardName(heroToPlay)} en pos {pos}");
            SummonHero(heroToPlay, pos);
            return true;
        }
        Log("No se invoca héroe (ninguno elegible o sin posiciones).");
        return false;
    }

    private void PlayEquipmentCard()
    {
        var playable = GetPlayableEquipment();
        var equipmentToPlay = playable.Count > 0 ? playable[0] : null;
        if (equipmentToPlay == null)
        {
            Log("Sin equipamiento jugable."); return;
        }

        var compat = GetHeroesCompatibleWithEquipment(equipmentToPlay.cardSO as EquipmentCardSO);
        if (compat == null || compat.Count == 0)
        {
            Log("No hay héroes compatibles para equipar."); return;
        }

        var bestHero = compat.OrderByDescending(c => MaxPotentialDamage(c)).First();
        Log($"Equipando {equipmentToPlay.cardSO.name} en {CardName(bestHero)}");
        duelManager.PlaceCardInField(playerManager, playerManager.isPlayer,
            handCardHandler.GetIdexOfCard(equipmentToPlay), bestHero.FieldPosition.PositionIndex);
    }

    private bool NeedsMoreHeroes()
    {
        bool needs = (turn <= 1 || GetAttackPower() < GetAverageDefenseRival() * 1.5f);
        Log($"NeedsMoreHeroes? {needs} | turn={turn} | atk={GetAttackPower()} | rivalAvgDef={GetAverageDefenseRival()}");
        return needs;
    }

    private int GetAttackPower()
    {
        int power = 0;
        foreach (var c in playerManager.GetAllCardInField())
            power += c.Moves.Select(m => m.MoveSO.Damage).DefaultIfEmpty(0).Max();
        return power;
    }

    private int GetAverageDefenseRival()
    {
        var rivals = duelManager.GetOpposingPlayerManager(playerManager).GetAllCardInField();
        if (rivals.Count == 0) return 0;
        int sum = 0; foreach (var c in rivals) sum += c.CurrentDefensePoints;
        return sum / rivals.Count;
    }

    private List<Card> GetPlayableHeroes()
    {
        var list = new List<Card>();
        foreach (var c in handCardHandler.GetCardInHandList())
            if (c.cardSO is HeroCardSO && c.UsableCard(playerManager)) list.Add(c);
        Log($"Heroes jugables: {list.Count}");
        return list;
    }
    private List<Card> GetPlayableEquipment()
    {
        var list = new List<Card>();
        foreach (var c in handCardHandler.GetCardInHandList())
            if (c.cardSO is EquipmentCardSO && c.UsableCard(playerManager)) list.Add(c);
        Log($"Equipamientos jugables: {list.Count}");
        return list;
    }
    private List<Card> GetHeroesCompatibleWithEquipment(EquipmentCardSO eq)
    {
        var list = new List<Card>();
        foreach (var c in playerManager.GetAllCardInField())
            if (c.IsAvailableEquipmentSlot(eq)) list.Add(c);
        return list;
    }

    private Card ChoosingHeroToSummon(List<Card> usable)
    {
        Card pick = null; float best = float.MinValue;
        foreach (var c in usable)
        {
            float s = EvaluateInvocation(c);
            if (s > best) { best = s; pick = c; }
        }
        Log(pick ? $"ChoosingHeroToSummon -> {CardName(pick)} (score={best:F1})" : "ChoosingHeroToSummon -> ninguno");
        return pick;
    }

    private float EvaluateInvocation(Card h)
    {
        float s = 0f;
        s += h.HealtPoint * 1.2f;
        s += h.CurrentDefensePoints * 1.0f;
        s += h.CurrentSpeedPoints * 0.8f;
        if (h.Moves.Count > 0) s += EvaluateMovement(h.Moves[0]);
        if (h.Moves.Count > 1) s += EvaluateMovement(h.Moves[1]);
        if (h.cardSO is HeroCardSO heroSO) s -= heroSO.Energy * 1.5f;
        if (duelManager.GetOpposingPlayerManager(playerManager).GetAllCardInField().Count == 0)
            s += MaxPotentialDamage(h) * 0.5f;
        return s;
    }

    private float EvaluateMovement(Movement m)
    {
        if (m == null || m.MoveSO == null) return 0f;
        float s = 0f;
        s += m.MoveSO.Damage;
        if (m.MoveSO.MoveEffect != null) s += m.MoveSO.MoveEffect.effectScore;
        if (m.MoveSO.EnergyCost > 0) s += (float)m.MoveSO.Damage / Mathf.Max(1, m.MoveSO.EnergyCost);
        return s;
    }

    private void SummonHero(Card c, int i)
    {
        duelManager.PlaceCardInField(playerManager, playerManager.isPlayer,
            handCardHandler.GetIdexOfCard(c), i);
    }

    private int ChoosePositionFieldIndex(Card heroToPlay, bool random)
    {
        var heroClass = (heroToPlay.cardSO as HeroCardSO).HeroClass;
        var available = new List<FieldPosition>();
        foreach (var f in playerManager.GetFieldPositionList())
            if (f.Card == null) available.Add(f);
        if (available.Count == 0) return 0;
        if (random) return available[Random.Range(0, available.Count)].PositionIndex;

        if (heroClass == HeroClass.Warrior || heroClass == HeroClass.Paladin || heroClass == HeroClass.Colossus || (heroClass == HeroClass.Beast && heroToPlay.CurrentDefensePoints >= 50))
            available.RemoveAll(p => p.PositionIndex > 4);
        else if (heroClass == HeroClass.Wizard || heroClass == HeroClass.Necromancer || heroClass == HeroClass.Beast)
        { available.RemoveAll(p => p.PositionIndex < 5); available.RemoveAll(p => p.PositionIndex > 9); }
        else
            available.RemoveAll(p => p.PositionIndex < 10);

        var protectedPos = new List<FieldPosition>();
        foreach (var p in available)
        {
            if (p.PositionIndex - 5 >= 0 && playerManager.GetFieldPositionList()[p.PositionIndex - 5].Card != null) protectedPos.Add(p);
            else if (p.PositionIndex - 10 >= 0 && playerManager.GetFieldPositionList()[p.PositionIndex - 10].Card != null) protectedPos.Add(p);
            else if (p.PositionIndex + 5 < 15 && playerManager.GetFieldPositionList()[p.PositionIndex + 5].Card != null &&
                     (heroClass == HeroClass.Warrior || heroClass == HeroClass.Paladin || heroClass == HeroClass.Colossus || (heroClass == HeroClass.Beast && heroToPlay.CurrentDefensePoints >= 50))) protectedPos.Add(p);
            else if (p.PositionIndex + 10 < 15 && playerManager.GetFieldPositionList()[p.PositionIndex + 10].Card != null &&
                     (heroClass == HeroClass.Warrior || heroClass == HeroClass.Paladin || heroClass == HeroClass.Colossus || (heroClass == HeroClass.Beast && heroToPlay.CurrentDefensePoints >= 50))) protectedPos.Add(p);
        }
        int pick = (protectedPos.Count > 0 ? protectedPos : available)[Random.Range(0, (protectedPos.Count > 0 ? protectedPos : available).Count)].PositionIndex;
        Log($"ChoosePositionFieldIndex: {CardName(heroToPlay)} -> {pick}");
        return pick;
    }

    // =================== BATTLE: SOLO SUBTURNO ACTUAL (plan) + SIMULACIÓN TURNO COMPLETO ===================
    private IEnumerator DefineActionsForCurrentSubturn()
    {
        int guard = 0;
        while ((duelManager.HeroInTurn == null || duelManager.HeroInTurn.Count == 0) && guard < 10)
        {
            Log($"DefineActions: esperando HeroInTurn... [{guard + 1}]");
            guard++; yield return null;
        }
        if (!HasAIHeroInCurrentSubturn())
        {
            Log("DefineActions: no hay héroes IA en este subturno -> skip.");
            yield break;
        }

        var myFieldAll = playerManager.GetAllCardInField();
        if (myFieldAll.Count == 0) { Log("DefineActions: no tengo héroes en campo."); yield break; }

        var heroesThisSubturn = duelManager.HeroInTurn
            .Where(h => myFieldAll.Contains(h) && !h.IsControlled() && !h.turnCompleted)
            .ToList();

        if (heroesThisSubturn.Count == 0)
        {
            Log("DefineActions: no hay héroes IA para actuar en este subturno.");
            yield break;
        }

        Log($"Subturn Heroes: {string.Join(", ", heroesThisSubturn.Select(CardName))}");

        var combinations = GenerateMoveCombinations(heroesThisSubturn);
        Log($"Combinaciones (<= energía) para subturno: {combinations.Count}");
        yield return null;

        cardToAttack = null;
        combinationAttacks.Clear();

        var enemyField = duelManager.GetOpposingPlayerManager(playerManager).GetAllCardInField();
        Log($"Enemigos en campo: {enemyField.Count}");
        yield return null;

        if (enemyField.Count == 0)
        {
            var picked = ChooseBestDirectLifePlanFullTurn(combinations, heroesThisSubturn);
            Log($"Campo vacío rival: combo {CombToStr(picked.combination)} | cost={picked.cost} | directLife={picked.directDamage} | leftover(final)={picked.cost}");
            combinationAttacks = picked.combination;
            cardToAttack = null;
        }
        else
        {
            var result = new BestComboResult();
            yield return StartCoroutine(SelectBestCombinationFullTurn_PriorityRules(
                combinations, heroesThisSubturn, result));

            if (result.combination != null && result.combination.Count > 0)
            {
                combinationAttacks = result.combination;
                cardToAttack = result.target;
                Log($"Plan elegido -> Target {CardName(cardToAttack)} | Combo: {CombToStr(combinationAttacks)}");
            }
            else
            {
                Log("No hay combo ofensivo útil (≥ umbral) -> usaremos utilidad/recarga.");
            }
        }
    }

    private IEnumerator ExecuteActionForCurrentSubturn()
    {
        isExecuting = true;

        // Delay de inicio del subturno (ajustado por dificultad)
        yield return new WaitForSeconds(Random.Range(0.05f, 0.12f) * _delayMul);

        var heroInTurnSnapshot = (duelManager.HeroInTurn != null)
            ? duelManager.HeroInTurn.ToArray()
            : Array.Empty<Card>();

        var myFieldSnapshot = playerManager.GetAllCardInField().ToArray();
        var myFieldSet = new HashSet<Card>(myFieldSnapshot);

        var heroesToExecute = heroInTurnSnapshot
            .Where(c => myFieldSet.Contains(c) && !c.IsControlled() && !c.turnCompleted)
            .ToArray();

        Log($"ExecuteAction (subturno): {string.Join(", ", heroesToExecute.Select(CardName))}");

        var plan = new Dictionary<Card, int>();
        foreach (var pair in combinationAttacks.ToArray())
            plan[pair.Item1] = pair.Item2;

        int currentEnergy = playerManager.PlayerEnergy;

        foreach (var card in heroesToExecute)
        {
            if (duelManager.duelPhase.Value != DuelPhase.Battle) break;

            int attack;
            if (!plan.TryGetValue(card, out attack))
            {
                attack = PickMoveOnTheFly(card, ref currentEnergy);
                Log($"[OnTheFly] {CardName(card)} -> {attack}:{MoveName(card, attack)}");
            }
            else
            {
                // Ocasionalmente cometer un "error" y cambiar el movimiento planificado
                if (Chance(_mistakeProb))
                {
                    int mistaken = PickMistakeMove(card, currentEnergy);
                    if (mistaken != -1)
                    {
                        LogDeep($"[Diff] Error intencional: {CardName(card)} cambia {MoveName(card, attack)} -> {MoveName(card, mistaken)}");
                        attack = mistaken;
                    }
                }

                if (WouldMoveBeLethal(card, attack, out var lt))
                    Log($"Mantengo acción por ser LETAL sobre {CardName(lt)}.");

                // Descontar energía del movimiento elegido (plan o "mistaken")
                int costChosen = Mathf.Max(0, card.Moves[attack].MoveSO.EnergyCost);
                currentEnergy -= costChosen;
            }

            if (!card.UsableMovement(attack, playerManager))
            {
                Log($"Movimiento no usable -> fallback ({CardName(card)}).");
                int fb = FallbackUsableMoveSoftEnergy(card, ref currentEnergy);
                attack = fb;
                Log($"Fallback -> {attack}:{MoveName(card, attack)}");
            }

            yield return new WaitForSeconds(Random.Range(enqueueMoveDelayRange.x, enqueueMoveDelayRange.y));

            var moveSO = card.Moves[attack].MoveSO;

            if (moveSO.NeedTarget)
            {
                var legalTargets = duelManager.ObtainTargets(card, attack);

                if (legalTargets != null && legalTargets.Count > 0)
                {
                    Card desired = cardToAttack;
                    desired = ResolveProtectorIfAnyAmong(legalTargets, desired);

                    Card chosenTarget = null;
                    if (desired != null && legalTargets.Contains(desired))
                        chosenTarget = desired;
                    else
                        chosenTarget = PickBestAccessibleTarget(card, attack, legalTargets);

                    // Objetivo aleatorio a veces (dificultad baja)
                    if (Chance(_randomTargetProb) && legalTargets.Count > 0)
                    {
                        var rnd = legalTargets[Random.Range(0, legalTargets.Count)];
                        LogDeep($"[Diff] Objetivo aleatorio forzado: {CardName(rnd)}");
                        chosenTarget = rnd;
                    }

                    if (chosenTarget != null && chosenTarget.FieldPosition != null)
                    {
                        Log($"UseMovement TARGETED: {CardName(card)} -> {attack}:{MoveName(card, attack)} a {CardName(chosenTarget)}@{chosenTarget.FieldPosition.PositionIndex}");
                        duelManager.UseMovement(attack, card, chosenTarget.FieldPosition.PositionIndex);
                    }
                    else
                    {
                        var enemyMgr = duelManager.GetOpposingPlayerManager(playerManager);
                        bool enemyFieldEmpty = enemyMgr.GetAllCardInField().Count == 0;

                        if (enemyFieldEmpty && moveSO.Damage > 0 && card.UsableMovement(attack, playerManager))
                        {
                            Log($"Sin target válido y campo vacío -> DIRECT LIFE: {CardName(card)} -> {attack}:{MoveName(card, attack)}");
                            duelManager.UseMovement(attack, card, -1);
                        }
                        else
                        {
                            Log("Sin target válido (y no procede directo) → omitido.");
                        }
                    }
                }
                else
                {
                    var enemyMgr = duelManager.GetOpposingPlayerManager(playerManager);
                    bool enemyFieldEmpty = enemyMgr.GetAllCardInField().Count == 0;

                    if (enemyFieldEmpty && moveSO.Damage > 0 && card.UsableMovement(attack, playerManager))
                    {
                        Log($"No hay objetivos y campo enemigo vacío → DIRECT LIFE: {CardName(card)} -> {attack}:{MoveName(card, attack)}");
                        duelManager.UseMovement(attack, card, -1);
                    }
                    else
                    {
                        Log("No hay objetivos y no procede directo → omitido.");
                    }
                }
            }
            else
            {
                // AoE / efectos sin target: DuelManager decidirá los afectados válidos
                Log($"UseMovement NO-TARGET (AoE/positivo): {CardName(card)} -> {attack}:{MoveName(card, attack)}");
                duelManager.UseMovement(attack, card);
            }
        }

        isExecuting = false;
    }

    // ---------- Selección con PRIO ----------
    private class BestComboResult { public List<(Card, int)> combination; public Card target; }

    private IEnumerator SelectBestCombinationFullTurn_PriorityRules(
        List<List<(Card, int)>> combinations,
        List<Card> heroesThisSubturn,
        BestComboResult resultOut)
    {
        int iter = 0;
        int currentSubturnIndex = GetCurrentSubturnIndex();
        var fullScheduleFuture = BuildFutureAISchedule(currentSubturnIndex);

        // PRIO 2: KILL
        int bestKillIdx = -1; Card bestKillTarget = null; float bestKillScore = float.NegativeInfinity; int bestKillCost = int.MaxValue;
        for (int i = 0; i < combinations.Count; i++)
        {
            if (combosYieldEvery > 0 && (++iter % combosYieldEvery) == 0) yield return null;

            var comb = combinations[i];
            var sim = SimulateFullTurnPlan(comb, heroesThisSubturn, fullScheduleFuture);

            foreach (var kv in sim.hpDamagePerEnemy)
            {
                var enemyCard = kv.Key;
                int hpBefore = enemyCard.CurrentHealtPoints;
                int hpDmg = kv.Value;
                if (hpDmg >= hpBefore)
                {
                    float score = 100000f - (Mathf.Max(0, hpDmg - hpBefore) * overkillPenaltyFactor);
                    if (score > bestKillScore || (Mathf.Approximately(score, bestKillScore) && sim.totalEnergyCost < bestKillCost))
                    { bestKillScore = score; bestKillIdx = i; bestKillTarget = enemyCard; bestKillCost = sim.totalEnergyCost; }
                }
            }
        }
        if (bestKillIdx != -1)
        {
            Log($"PRIO 2: Kill -> combo#{bestKillIdx} a {CardName(bestKillTarget)} (cost={bestKillCost})");
            resultOut.combination = new List<(Card, int)>(combinations[bestKillIdx]);
            resultOut.target = bestKillTarget;
            yield break;
        }

        // PRIO 3: Máximo daño a VIDA (≥ umbral)
        int bestIdx = -1; Card bestTarget = null; int bestHpDamage = -1; int bestCostAtDamage = int.MaxValue;
        float bestPenaltyTie = float.PositiveInfinity;

        for (int i = 0; i < combinations.Count; i++)
        {
            if (combosYieldEvery > 0 && (++iter % combosYieldEvery) == 0) yield return null;

            var comb = combinations[i];
            var sim = SimulateFullTurnPlan(comb, heroesThisSubturn, fullScheduleFuture);

            Card localBestTarget = null; int localBestHp = -1;
            foreach (var kv in sim.hpDamagePerEnemy)
            {
                if (kv.Value > localBestHp) { localBestHp = kv.Value; localBestTarget = kv.Key; }
            }
            if (localBestTarget == null || localBestHp <= 0) continue;

            int threshold = Mathf.CeilToInt(Mathf.Max(1, localBestTarget.CurrentHealtPoints) * minHpDamagePercent);
            if (localBestHp < threshold) continue;

            int cost = sim.totalEnergyCost;
            int leftover = sim.finalEnergy;
            float softPenalty = (leftover > energySoftTarget) ? (leftover - energySoftTarget) * softOverflowPenaltyPerPoint : 0f;

            bool better =
                (localBestHp > bestHpDamage) ||
                (localBestHp == bestHpDamage && cost < bestCostAtDamage) ||
                (localBestHp == bestHpDamage && cost == bestCostAtDamage && softPenalty < bestPenaltyTie);

            if (better)
            {
                bestHpDamage = localBestHp;
                bestIdx = i;
                bestTarget = localBestTarget;
                bestCostAtDamage = cost;
                bestPenaltyTie = softPenalty;
            }
        }

        if (bestIdx != -1)
        {
            Log($"PRIO 3: Máx daño a vida -> combo#{bestIdx} ({bestHpDamage}) a {CardName(bestTarget)} (cost={bestCostAtDamage})");
            resultOut.combination = new List<(Card, int)>(combinations[bestIdx]);
            resultOut.target = bestTarget;
            yield break;
        }

        // Nada supera umbral → utilidad/recarga
        Log("Ningún plan supera el umbral de vida → utilidad/recarga.");
        var utilPlan = ChooseBestUtilityPlanFullTurn(combinations, heroesThisSubturn, null);
        resultOut.combination = utilPlan;
        resultOut.target = null;
    }

    // ====== GENERACIÓN DE COMBINACIONES ======
    private List<List<(Card, int)>> GenerateMoveCombinations(List<Card> actingHeroes)
    {
        var usable = new List<List<(Card, int)>>();
        var heroes = actingHeroes.Where(h => !h.IsControlled()).ToList();
        var combinations = GetHeroAttackCombinations(heroes);
        Log($"GetHeroAttackCombinations: total={combinations.Count} (antes de filtrar por energía).");

        foreach (var comb in combinations)
        {
            int energy = GetTotalEnergyOfCombination(comb);
            if (energy <= playerManager.PlayerEnergy) usable.Add(comb);
        }

        // Recorte por dificultad (para CPU o para "equivocarse" un poco)
        if (_combHardCap > 0 && usable.Count > _combHardCap)
        {
            LogDeep($"[Diff] Limito combinaciones por dificultad: {usable.Count} → {_combHardCap}");
            usable = usable.OrderBy(_ => Random.value).Take(_combHardCap).ToList();
        }

        Log($"GenerateMoveCombinations: usables={usable.Count} (energía actual={playerManager.PlayerEnergy}).");
        return usable;
    }

    static List<List<(Card, int)>> GetHeroAttackCombinations(List<Card> heroes)
    {
        var all = new List<List<(Card, int)>>();
        GenerateHeroMoveCombinations(heroes, new List<(Card, int)>(), 0, all);
        return all;
    }

    static void GenerateHeroMoveCombinations(List<Card> heroes, List<(Card, int)> current, int index, List<List<(Card, int)>> result)
    {
        if (index == heroes.Count)
        {
            result.Add(new List<(Card, int)>(current));
            return;
        }
        var hero = heroes[index];
        int movesCount = Mathf.Max(0, hero.Moves.Count);
        for (int i = 0; i < movesCount; i++)
        {
            current.Add((hero, i));
            GenerateHeroMoveCombinations(heroes, current, index + 1, result);
            current.RemoveAt(current.Count - 1);
        }
    }

    // ============================ SIMULACIÓN CERRADA ============================
    // (Se mantiene tu simulación completa; no se modifica nada aquí)

    // ---------- Tipos de simulación ----------
    private class SimEffect
    {
        public int AbsorbPerHit;
        public bool IsProtector;
        public Card ProtectorCasterReal;
        public int AttackModContribution;
        public int SubturnsLeft;

        public bool FlagBurn;
        public bool FlagFullReflect;
        public bool FlagLethargy;
        public bool FlagPhantomShield;
        public bool FlagRangedImmune;
        public bool FlagMeleeImmune;

        public bool IsActive => SubturnsLeft > 0;
    }

    private class SimCardState
    {
        public Card Orig;
        public bool IsMine;
        public bool Alive;
        public int PosIndex;
        public int HP;
        public int DEF;
        public int EnergyBonus;
        public bool HasWeaponMove3;
        public int AttackModifierSnapshot;
        public List<SimEffect> Effects = new List<SimEffect>();
    }

    private class SimSnapshot
    {
        public Dictionary<Card, SimCardState> Map = new Dictionary<Card, SimCardState>();
        public List<SimCardState> Mine = new List<SimCardState>();
        public List<SimCardState> Enemy = new List<SimCardState>();

        public int EnergyCurrent;
        public int EnergyBanked;
        public const int EnergyCap = 100;

        public int SubturnCursor = 0;

        public void TickToNextSubturn()
        {
            if (EnergyBanked > 0)
            {
                EnergyCurrent = Mathf.Clamp(EnergyCurrent + EnergyBanked, 0, EnergyCap);
                EnergyBanked = 0;
            }
            foreach (var kv in Map)
            {
                foreach (var ef in kv.Value.Effects)
                    if (ef.SubturnsLeft > 0) ef.SubturnsLeft--;
            }
            SubturnCursor++;
        }
    }

    private interface ISimCallbacks
    {
        int GetRawDamage(Card attacker, int moveIdx, Card target, SimSnapshot snap);
        int GetDefenseIgnored(Card attacker, int moveIdx, Card target, SimSnapshot snap);
        int GetAbsorb(Card target, SimSnapshot snap);
        bool CheckMoveCondition(Card attacker, Card target, ScriptableObject effectCondition, SimSnapshot snap);
    }

    private class RealisticSimCallbacks : ISimCallbacks
    {
        private readonly HemeraLegionAI _ai;
        public RealisticSimCallbacks(HemeraLegionAI ai) { _ai = ai; }

        public bool CheckMoveCondition(Card attacker, Card target, ScriptableObject effectCondition, SimSnapshot snap)
        {
            if (effectCondition == null) return false;
            var m = effectCondition.GetType().GetMethod("CheckCondition", new[] { typeof(Card), typeof(Card) });
            if (m != null)
            {
                try { return (bool)m.Invoke(effectCondition, new object[] { attacker, target }); }
                catch { return false; }
            }
            return false;
        }

        public int GetAbsorb(Card target, SimSnapshot snap)
        {
            if (target == null || !snap.Map.TryGetValue(target, out var st) || !st.Alive) return 0;
            int sum = 0;
            foreach (var ef in st.Effects)
                if (ef.IsActive) sum += Mathf.Max(0, ef.AbsorbPerHit);
            return sum;
        }

        private int SumAttackModFromActiveEffects(Card attacker, SimSnapshot snap, out bool hadPerEffectBreakdown)
        {
            hadPerEffectBreakdown = false;
            if (attacker == null || !snap.Map.TryGetValue(attacker, out var sa) || !sa.Alive) return 0;

            int sum = 0;
            foreach (var ef in sa.Effects)
            {
                if (!ef.IsActive) continue;
                if (ef.AttackModContribution != 0)
                {
                    sum += ef.AttackModContribution;
                    hadPerEffectBreakdown = true;
                }
            }
            return sum;
        }

        public int GetRawDamage(Card attacker, int moveIdx, Card target, SimSnapshot snap)
        {
            if (attacker == null || attacker.Moves == null || moveIdx < 0 || moveIdx >= attacker.Moves.Count) return 0;
            var move = attacker.Moves[moveIdx].MoveSO;
            if (move == null) return 0;

            if (move.MoveEffect is DestroyDefense)
            {
                if (target == null || !snap.Map.TryGetValue(target, out var st)) return 0;
                int absorb = GetAbsorb(target, snap);
                return Mathf.Max(0, st.DEF) + Mathf.Max(0, absorb);
            }

            int dmg = Mathf.Max(0, move.Damage);

            int perEffect = SumAttackModFromActiveEffects(attacker, snap, out bool hadBreakdown);
            if (hadBreakdown) dmg += perEffect;
            else
            {
                if (snap.Map.TryGetValue(attacker, out var sa)) dmg += Mathf.Max(0, sa.AttackModifierSnapshot);
            }

            if (move.MoveEffect is IncreaseAttackDamage atkMod)
            {
                bool cond = CheckMoveCondition(attacker, target, move.EffectCondition, snap);
                if (cond) dmg += Mathf.Max(0, atkMod.Amount);
            }

            if (target != null && snap.Map.TryGetValue(target, out var st2))
            {
                try
                {
                    int eff = CardSO.GetEffectiveness(move.Element, target.GetElement());
                    dmg += eff;
                }
                catch { }
            }

            return Mathf.Max(0, dmg);
        }

        public int GetDefenseIgnored(Card attacker, int moveIdx, Card target, SimSnapshot snap)
        {
            if (attacker == null || attacker.Moves == null || moveIdx < 0 || moveIdx >= attacker.Moves.Count) return 0;
            var move = attacker.Moves[moveIdx].MoveSO;
            if (move == null) return 0;

            if (move.MoveEffect is IgnoredDefense ignored)
            {
                if (ignored.Amount == move.Damage)
                {
                    int atkBonus = 0;

                    int perEffect = SumAttackModFromActiveEffects(attacker, snap, out bool hadBreakdown);
                    if (hadBreakdown) atkBonus = perEffect;
                    else if (snap.Map.TryGetValue(attacker, out var sa))
                        atkBonus = Mathf.Max(0, sa.AttackModifierSnapshot);

                    int elem = 0;
                    if (target != null && snap.Map.TryGetValue(target, out var st))
                    {
                        try { elem = CardSO.GetEffectiveness(move.Element, target.GetElement()); } catch { }
                    }

                    return Mathf.Max(0, ignored.Amount + atkBonus + elem);
                }
                return Mathf.Max(0, ignored.Amount);
            }
            return 0;
        }
    }

    // ---------- Helpers de snapshot ----------
    private SimSnapshot BuildSimSnapshot()
    {
        var snap = new SimSnapshot
        {
            EnergyCurrent = Mathf.Clamp(playerManager.PlayerEnergy, 0, SimSnapshot.EnergyCap),
            EnergyBanked = 0
        };

        foreach (var c in playerManager.GetAllCardInField())
        {
            var s = MakeSimCard(c, true);
            snap.Map[c] = s; snap.Mine.Add(s);
        }

        var enemyMgr = duelManager.GetOpposingPlayerManager(playerManager);
        foreach (var c in enemyMgr.GetAllCardInField())
        {
            var s = MakeSimCard(c, false);
            snap.Map[c] = s; snap.Enemy.Add(s);
        }

        return snap;
    }

    private SimCardState MakeSimCard(Card c, bool isMine)
    {
        var s = new SimCardState
        {
            Orig = c,
            IsMine = isMine,
            Alive = c != null,
            PosIndex = c?.FieldPosition?.PositionIndex ?? -1,
            HP = c?.CurrentHealtPoints ?? 0,
            DEF = c?.CurrentDefensePoints ?? 0,
            EnergyBonus = c?.GetEnergyBonus() ?? 0,
            HasWeaponMove3 = (c != null && c.Moves != null && c.Moves.Count > 3),
            AttackModifierSnapshot = c?.GetAttackModifier() ?? 0,
            Effects = new List<SimEffect>()
        };

        if (c?.ActiveEffects != null)
        {
            foreach (var e in c.ActiveEffects)
            {
                if (e == null) continue;

                var se = new SimEffect
                {
                    AbsorbPerHit = SafeAbsorb(e),
                    IsProtector = SafeHasProtector(e),
                    ProtectorCasterReal = SafeCaster(e),
                    AttackModContribution = TryGetAttackModFromEffect(e),
                    SubturnsLeft = TryGetRemainingSubturns(e, defaultValue: 1)
                };

                ClassifyEffectFlags(e, se);
                s.Effects.Add(se);
            }
        }

        return s;

        static int SafeAbsorb(Effect e) { try { return Mathf.Max(0, e.GetDamageAbsorbed()); } catch { return 0; } }
        static bool SafeHasProtector(Effect e) { try { return e.HasProtector(); } catch { return false; } }
        static Card SafeCaster(Effect e) { try { return e.casterHero; } catch { return null; } }
    }

    private void ClassifyEffectFlags(Effect e, SimEffect se)
    {
        var typeName = e.GetType().Name;
        string n = typeName.ToLowerInvariant();
        se.FlagBurn |= n.Contains("burn");
        se.FlagFullReflect |= n.Contains("reflection");
        se.FlagLethargy |= n.Contains("letharg");
        se.FlagPhantomShield |= n.Contains("phantomshield");
        se.FlagRangedImmune |= n.Contains("rangedimmun");
        se.FlagMeleeImmune |= n.Contains("meleeimmun");
    }

    private int TryGetAttackModFromEffect(Effect e)
    {
        if (e == null) return 0;
        var m = e.GetType().GetMethod("GetAttackModifier", Type.EmptyTypes);
        if (m != null && m.ReturnType == typeof(int))
        {
            try { return Mathf.Max(0, (int)m.Invoke(e, null)); } catch { }
        }
        var p = e.GetType().GetProperty("AttackModifier", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (p != null && p.PropertyType == typeof(int) && p.CanRead)
        {
            try { return Mathf.Max(0, (int)p.GetValue(e)); } catch { }
        }
        return 0;
    }

    private int TryGetRemainingSubturns(Effect e, int defaultValue)
    {
        if (e == null) return defaultValue;
        var p = e.GetType().GetProperty("RemainingSubturns", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
             ?? e.GetType().GetProperty("TurnsLeft")
             ?? e.GetType().GetProperty("DurationSubturns");
        if (p != null && p.PropertyType == typeof(int))
        {
            try { return Mathf.Max(0, (int)p.GetValue(e)); } catch { return defaultValue; }
        }
        return defaultValue;
    }

    private static bool SimIsBurned(SimCardState sc) => sc.Effects.Any(e => e.IsActive && e.FlagBurn);
    private static bool SimHasFullDamageReflection(SimCardState sc) => sc.Effects.Any(e => e.IsActive && e.FlagFullReflect);
    private static bool SimIsInLethargy(SimCardState sc) => sc.Effects.Any(e => e.IsActive && e.FlagLethargy);
    private static bool SimHasPhantomShield(SimCardState sc) => sc.Effects.Any(e => e.IsActive && e.FlagPhantomShield);
    private static bool SimHasRangedImmunity(SimCardState sc) => sc.Effects.Any(e => e.IsActive && e.FlagRangedImmune);
    private static bool SimHasMeleeImmunity(SimCardState sc) => sc.Effects.Any(e => e.IsActive && e.FlagMeleeImmune);

    private SimCardState SimResolveProtector(SimSnapshot snap, SimCardState target)
    {
        if (target == null || !target.Alive) return null;
        foreach (var ef in target.Effects)
        {
            if (!ef.IsActive || !ef.IsProtector || ef.ProtectorCasterReal == null) continue;
            if (!snap.Map.TryGetValue(ef.ProtectorCasterReal, out var prot)) continue;
            if (prot != null && prot.Alive) return prot;
        }
        return null;
    }

    private int SimReceiveDamage(
        SimSnapshot snap,
        ISimCallbacks rules,
        Card attackerReal,
        MoveType moveType,
        SimCardState target,
        int amountDamage,
        int ignoredDefense,
        FullPlanSim acc,
        bool reflectionCall = false)
    {
        if (target == null || !target.Alive) return 0;

        if (attackerReal != null && SimIsBurned(target))
            amountDamage += 10;

        if (attackerReal != null && attackerReal.cardSO is HeroCardSO && SimHasFullDamageReflection(target))
        {
            if (snap.Map.TryGetValue(attackerReal, out var attackerSim) && attackerSim.Alive)
            {
                SimReceiveDamage(snap, rules, null, moveType, attackerSim, amountDamage, ignoredDefense, acc, reflectionCall: true);
            }
        }

        if (SimIsInLethargy(target) ||
            SimHasPhantomShield(target) ||
            (moveType == MoveType.RangedAttack && SimHasRangedImmunity(target)) ||
            (moveType == MoveType.MeleeAttack && SimHasMeleeImmunity(target)))
        {
            amountDamage = 0;
        }

        if (attackerReal != null && attackerReal.cardSO is HeroCardSO)
        {
            var prot = SimResolveProtector(snap, target);
            if (prot != null)
            {
                return SimReceiveDamage(snap, rules, attackerReal, moveType, prot, amountDamage, ignoredDefense, acc, reflectionCall);
            }
        }

        amountDamage -= Mathf.Max(0, rules.GetAbsorb(target.Orig, snap));
        if (amountDamage < 0) amountDamage = 0;

        ignoredDefense = Mathf.Min(ignoredDefense, amountDamage);

        int toShield = Mathf.Max(0, amountDamage - ignoredDefense);
        int usedOnShield = Mathf.Min(target.DEF, toShield);
        target.DEF = Mathf.Max(0, target.DEF - usedOnShield);

        int hpDamage = (toShield - usedOnShield) + ignoredDefense;
        int inflicted = 0;
        if (hpDamage > 0)
        {
            inflicted = Math.Min(hpDamage, Math.Max(0, target.HP));
            target.HP = Math.Max(0, target.HP - hpDamage);
            if (target.HP <= 0) target.Alive = false;

            if (!target.IsMine)
            {
                if (!acc.hpDamagePerEnemy.ContainsKey(target.Orig)) acc.hpDamagePerEnemy[target.Orig] = 0;
                acc.hpDamagePerEnemy[target.Orig] += inflicted;
            }
        }

        return inflicted;
    }

    private void SimApplyAction(
        SimSnapshot snap,
        ISimCallbacks rules,
        Card attackerReal,
        int moveIdx,
        bool isCurrentSubturn,
        FullPlanSim acc)
    {
        if (attackerReal == null || !snap.Map.TryGetValue(attackerReal, out var attacker) || !attacker.Alive) return;
        if (attackerReal.Moves == null || moveIdx < 0 || moveIdx >= attackerReal.Moves.Count) return;

        var m = attackerReal.Moves[moveIdx].MoveSO;
        if (m == null) return;

        if (moveIdx == RECHARGE_INDEX)
        {
            int inc = 10 + Mathf.Max(0, attacker.EnergyBonus);
            snap.EnergyBanked = Mathf.Clamp(snap.EnergyBanked + inc, 0, SimSnapshot.EnergyCap);
            return;
        }

        int cost = Mathf.Max(0, m.EnergyCost);
        if (cost > snap.EnergyCurrent) return;

        snap.EnergyCurrent = Mathf.Clamp(snap.EnergyCurrent - cost, 0, SimSnapshot.EnergyCap);
        acc.totalEnergyCost += cost;

        bool enemyEmpty = snap.Enemy.TrueForAll(e => !e.Alive);

        if (!m.NeedTarget)
        {
            if (!enemyEmpty)
            {
                var affected = duelManager.ObtainTargets(attackerReal, moveIdx);
                if (affected == null || affected.Count == 0) return;

                foreach (var tReal in affected)
                {
                    if (tReal == null || !snap.Map.TryGetValue(tReal, out var tSim) || !tSim.Alive) continue;
                    if (tSim.IsMine) continue;

                    var prot = SimResolveProtector(snap, tSim) ?? tSim;

                    int raw = Mathf.Max(0, rules.GetRawDamage(attackerReal, moveIdx, prot.Orig, snap));
                    int ign = Mathf.Max(0, rules.GetDefenseIgnored(attackerReal, moveIdx, prot.Orig, snap));

                    SimReceiveDamage(snap, rules, attackerReal, m.MoveType, prot, raw, ign, acc);
                }
            }
            else
            {
                int dmg = Mathf.Max(0, rules.GetRawDamage(attackerReal, moveIdx, null, snap));
                acc.directLifeDamage += dmg;
            }
            return;
        }

        var legals = duelManager.ObtainTargets(attackerReal, moveIdx);
        if (legals == null || legals.Count == 0)
        {
            if (enemyEmpty && m.Damage > 0)
            {
                int dmg = Mathf.Max(0, rules.GetRawDamage(attackerReal, moveIdx, null, snap));
                acc.directLifeDamage += dmg;
            }
            return;
        }

        SimCardState bestT = null; int bestHp = -1;

        foreach (var tReal in legals)
        {
            if (tReal == null || !snap.Map.TryGetValue(tReal, out var tSim) || !tSim.Alive) continue;
            if (tSim.IsMine) continue;

            var prot = SimResolveProtector(snap, tSim) ?? tSim;

            int raw = Mathf.Max(0, rules.GetRawDamage(attackerReal, moveIdx, prot.Orig, snap));
            int ign = Mathf.Max(0, rules.GetDefenseIgnored(attackerReal, moveIdx, prot.Orig, snap));

            int savedDEF = prot.DEF, savedHP = prot.HP; bool savedAlive = prot.Alive;
            var tempAcc = new FullPlanSim();

            int inflicted = SimReceiveDamage(snap, rules, attackerReal, m.MoveType, prot, raw, ign, tempAcc, reflectionCall: false);

            prot.DEF = savedDEF; prot.HP = savedHP; prot.Alive = savedAlive;

            if (inflicted > bestHp) { bestHp = inflicted; bestT = prot; }
        }

        if (bestT == null) return;

        {
            int raw = Mathf.Max(0, rules.GetRawDamage(attackerReal, moveIdx, bestT.Orig, snap));
            int ign = Mathf.Max(0, rules.GetDefenseIgnored(attackerReal, moveIdx, bestT.Orig, snap));
            SimReceiveDamage(snap, rules, attackerReal, m.MoveType, bestT, raw, ign, acc);
        }
    }

    private int SimPickBestMoveForCard(SimSnapshot snap, ISimCallbacks rules, Card hero, int energyLeft)
    {
        if (hero == null || hero.Moves == null) return -1;

        int bestIdx = -1;
        int bestScore = -1;

        for (int i = 0; i < hero.Moves.Count; i++)
        {
            var m = hero.Moves[i].MoveSO;
            if (m == null) continue;

            if (i != RECHARGE_INDEX)
            {
                int cost = Mathf.Max(0, m.EnergyCost);
                if (cost > energyLeft) continue;
            }

            int score = 0;
            if (i == RECHARGE_INDEX)
            {
                score = 1;
            }
            else
            {
                if (!m.NeedTarget)
                {
                    int sum = 0;
                    var affected = duelManager.ObtainTargets(hero, i) ?? new List<Card>();
                    foreach (var tReal in affected)
                    {
                        if (!snap.Map.TryGetValue(tReal, out var tSim) || !tSim.Alive || tSim.IsMine) continue;
                        var prot = SimResolveProtector(snap, tSim) ?? tSim;

                        int raw = Mathf.Max(0, rules.GetRawDamage(hero, i, prot.Orig, snap));
                        int ign = Mathf.Max(0, rules.GetDefenseIgnored(hero, i, prot.Orig, snap));

                        int savedDEF = prot.DEF, savedHP = prot.HP; bool savedAlive = prot.Alive;
                        var tempAcc = new FullPlanSim();
                        int inflicted = SimReceiveDamage(snap, rules, hero, m.MoveType, prot, raw, ign, tempAcc, reflectionCall: false);
                        prot.DEF = savedDEF; prot.HP = savedHP; prot.Alive = savedAlive;

                        sum += inflicted;
                    }
                    score = sum;
                }
                else
                {
                    int bestLocal = 0;
                    var legals = duelManager.ObtainTargets(hero, i) ?? new List<Card>();
                    foreach (var tReal in legals)
                    {
                        if (!snap.Map.TryGetValue(tReal, out var tSim) || !tSim.Alive || tSim.IsMine) continue;
                        var prot = SimResolveProtector(snap, tSim) ?? tSim;

                        int raw = Mathf.Max(0, rules.GetRawDamage(hero, i, prot.Orig, snap));
                        int ign = Mathf.Max(0, rules.GetDefenseIgnored(hero, i, prot.Orig, snap));

                        int savedDEF = prot.DEF, savedHP = prot.HP; bool savedAlive = prot.Alive;
                        var tempAcc = new FullPlanSim();
                        int inflicted = SimReceiveDamage(snap, rules, hero, m.MoveType, prot, raw, ign, tempAcc, reflectionCall: false);
                        prot.DEF = savedDEF; prot.HP = savedHP; prot.Alive = savedAlive;

                        if (inflicted > bestLocal) bestLocal = inflicted;
                    }
                    score = bestLocal;
                }
            }

            if (score > bestScore) { bestScore = score; bestIdx = i; }
        }

        return bestIdx;
    }

    private class FullPlanSim
    {
        public Dictionary<Card, int> hpDamagePerEnemy = new Dictionary<Card, int>();
        public int totalEnergyCost = 0;
        public int directLifeDamage = 0;
        public int finalEnergy = 0;
    }

    private FullPlanSim SimulateFullTurnPlan(
        List<(Card, int)> combCurrentSubturn,
        List<Card> heroesThisSubturnOrder,
        List<List<Card>> futureSchedule)
    {
        var sim = new FullPlanSim();
        var snap = BuildSimSnapshot();
        var rules = new RealisticSimCallbacks(this);

        foreach (var e in snap.Enemy) if (e.Alive) sim.hpDamagePerEnemy[e.Orig] = 0;

        var orderedThis = combCurrentSubturn.OrderBy(p => heroesThisSubturnOrder.IndexOf(p.Item1)).ToList();
        foreach (var (hero, moveIdx) in orderedThis)
        {
            SimApplyAction(snap, rules, hero, moveIdx, isCurrentSubturn: true, sim);
        }

        if (futureSchedule != null)
        {
            foreach (var lane in futureSchedule)
            {
                snap.TickToNextSubturn();

                foreach (var c in lane)
                {
                    if (c == null) continue;
                    if (!snap.Map.TryGetValue(c, out var sc) || !sc.Alive) continue;

                    int pick = SimPickBestMoveForCard(snap, rules, c, snap.EnergyCurrent);
                    if (pick < 0) continue;

                    SimApplyAction(snap, rules, c, pick, isCurrentSubturn: false, sim);
                }
            }
        }

        sim.finalEnergy = Mathf.Clamp(snap.EnergyCurrent + snap.EnergyBanked, 0, SimSnapshot.EnergyCap);
        return sim;
    }

    private List<List<Card>> BuildFutureAISchedule(int startIndexExclusive)
    {
        var schedule = new List<List<Card>>();
        if (duelManager.Turns == null || duelManager.Turns.Length == 0) return schedule;

        var mySet = new HashSet<Card>(playerManager.GetAllCardInField());

        for (int i = startIndexExclusive + 1; i < duelManager.Turns.Length; i++)
        {
            var slot = duelManager.Turns[i];
            if (slot == null) continue;

            var lane = new List<Card>();
            foreach (var c in slot)
            {
                if (c == null) continue;
                if (!mySet.Contains(c)) continue;
                if (c.turnCompleted || c.IsControlled()) continue;
                lane.Add(c);
            }
            if (lane.Count > 0) schedule.Add(lane);
        }
        return schedule;
    }

    private (List<(Card, int)> combination, int cost, int directDamage) ChooseBestDirectLifePlanFullTurn(
        List<List<(Card, int)>> combinations, List<Card> heroesThisSubturn)
    {
        int bestIdx = -1, bestCost = int.MaxValue, bestDirect = -1; float bestTiePenalty = float.PositiveInfinity;
        int currentSub = GetCurrentSubturnIndex();
        var futureSchedule = BuildFutureAISchedule(currentSub);

        for (int i = 0; i < combinations.Count; i++)
        {
            var comb = combinations[i];
            var sim = SimulateFullTurnPlan(comb, heroesThisSubturn, futureSchedule);
            int cost = sim.totalEnergyCost;
            int direct = sim.directLifeDamage;

            int leftover = sim.finalEnergy;
            float softPenalty = (leftover > energySoftTarget) ? (leftover - energySoftTarget) * softOverflowPenaltyPerPoint : 0f;

            bool better = (direct > bestDirect) ||
                          (direct == bestDirect && cost < bestCost) ||
                          (direct == bestDirect && cost == bestCost && softPenalty < bestTiePenalty);

            if (better) { bestIdx = i; bestCost = cost; bestDirect = direct; bestTiePenalty = softPenalty; }
        }

        if (bestIdx != -1) return (new List<(Card, int)>(combinations[bestIdx]), bestCost, bestDirect);
        return (new List<(Card, int)>(), 0, 0);
    }

    private List<(Card, int)> ChooseBestUtilityPlanFullTurn(
        List<List<(Card, int)>> combinations, List<Card> heroesThisSubturn, List<Card> _unused)
    {
        int currentSub = GetCurrentSubturnIndex();
        var futureSchedule = BuildFutureAISchedule(currentSub);

        int bestIdx = -1; float bestScore = float.NegativeInfinity;

        foreach (var (comb, idx) in combinations.Select((c, i) => (c, i)))
        {
            var sim = SimulateFullTurnPlan(comb, heroesThisSubturn, futureSchedule);

            float util = 0f;
            foreach (var (h, m) in comb)
            {
                var moveSO = h?.Moves[m]?.MoveSO;
                if (moveSO?.MoveEffect != null) util += moveSO.MoveEffect.effectScore;
            }
            int hpSum = sim.hpDamagePerEnemy.Values.Sum();
            util += hpSum * 0.25f;

            int leftover = sim.finalEnergy;
            if (leftover > energySoftTarget) util -= (leftover - energySoftTarget) * softOverflowPenaltyPerPoint;

            if (util > bestScore) { bestScore = util; bestIdx = idx; }
        }

        return (bestIdx != -1) ? new List<(Card, int)>(combinations[bestIdx]) : new List<(Card, int)>();
    }

    // ============================ FIN SIMULACIÓN ============================

    private int GetCurrentSubturnIndex()
    {
        if (duelManager.Turns == null || duelManager.Turns.Length == 0) return 0;
        var current = new HashSet<Card>(duelManager.HeroInTurn ?? new List<Card>());
        for (int i = 0; i < duelManager.Turns.Length; i++)
        {
            var slot = duelManager.Turns[i];
            if (slot != null && slot.Any(h => current.Contains(h))) return i;
        }
        return 0;
    }

    private bool HasAIHeroInCurrentSubturn()
    {
        if (duelManager.HeroInTurn == null || duelManager.HeroInTurn.Count == 0) return false;
        var mine = playerManager.GetAllCardInField();
        foreach (var h in duelManager.HeroInTurn)
            if (mine.Contains(h) && !h.IsControlled() && !h.turnCompleted) return true;
        return false;
    }

    private Card ResolveProtectorIfAnyAmong(IList<Card> legalTargets, Card preferred)
    {
        if (preferred?.ActiveEffects == null) return preferred;
        foreach (var e in preferred.ActiveEffects)
        {
            if (e != null && e.HasProtector() && e.casterHero != null)
            {
                if (legalTargets != null && legalTargets.Contains(e.casterHero))
                    return e.casterHero;
            }
        }
        return preferred;
    }

    private Card PickBestAccessibleTarget(Card attacker, int moveIdx, IList<Card> legalTargets)
    {
        if (legalTargets == null || legalTargets.Count == 0) return null;
        var move = attacker.Moves[moveIdx].MoveSO;
        if (move.MoveType == MoveType.PositiveEffect)
        {
            if (legalTargets.Contains(attacker)) return attacker;
            return legalTargets.OrderByDescending(MaxPotentialDamage).First();
        }
        Card best = null; int bestHp = -1;
        foreach (var t in legalTargets)
        {
            var realT = ResolveProtectorForSim(t) ?? t;

            int raw = duelManager.CalculateAttackDamage(attacker, moveIdx, realT);
            int absorbed = realT.GetDamageAbsorbed();
            int effective = Mathf.Max(0, raw - absorbed);
            if (effective <= 0) continue;

            int defRem = realT.CurrentDefensePoints;
            int defIgn = duelManager.CalculateDefenseIgnored(attacker, realT, moveIdx);
            int defCons = Mathf.Max(0, defRem - Mathf.Max(0, defIgn));
            int usedOnDef = Mathf.Min(defCons, effective);
            int hp = (effective - usedOnDef);

            if (hp > bestHp) { bestHp = hp; best = t; }
        }
        return best ?? legalTargets[0];
    }

    private Card ResolveProtectorForSim(Card tgt)
    {
        if (tgt?.ActiveEffects == null) return tgt;
        foreach (var e in tgt.ActiveEffects)
        {
            if (e != null && e.HasProtector() && e.casterHero != null)
                return e.casterHero;
        }
        return tgt;
    }

    private int GetTotalEnergyOfCombination(List<(Card, int)> comb)
    { int e = 0; foreach (var (h, i) in comb) e += h.Moves[i].MoveSO.EnergyCost; return e; }

    private int MaxPotentialDamage(Card c)
    { return c.Moves.Select(m => m.MoveSO.Damage).DefaultIfEmpty(0).Max(); }

    private bool WouldMoveBeLethal(Card attacker, int moveIdx, out Card lethalTarget)
    {
        lethalTarget = null;
        var moveSO = attacker.Moves[moveIdx].MoveSO;
        if (!attacker.UsableMovement(moveIdx, playerManager)) return false;
        if (!moveSO.NeedTarget) return false;

        var legalTargets = duelManager.ObtainTargets(attacker, moveIdx);
        if (legalTargets == null || legalTargets.Count == 0) return false;

        foreach (var t in legalTargets)
        {
            var realT = ResolveProtectorForSim(t) ?? t;

            int raw = duelManager.CalculateAttackDamage(attacker, moveIdx, realT);
            int absorbed = realT.GetDamageAbsorbed();
            int effective = Mathf.Max(0, raw - absorbed);
            if (effective <= 0) continue;

            int defRem = realT.CurrentDefensePoints;
            int defIgn = duelManager.CalculateDefenseIgnored(attacker, realT, moveIdx);
            int defCons = Mathf.Max(0, defRem - Mathf.Max(0, defIgn));
            int usedOnDef = Mathf.Min(defCons, effective);
            int hp = effective - usedOnDef;

            if (hp >= realT.CurrentHealtPoints)
            {
                lethalTarget = t;
                return true;
            }
        }
        return false;
    }

    private bool ShouldRechargeEvenIfAboveSoftTarget(int currentEnergy)
    {
        if (currentEnergy >= 100) return false;

        int afterRecharge = Mathf.Min(100, currentEnergy + 10);
        int overflowAfterRecharge = afterRecharge - energySoftTarget;
        if (overflowAfterRecharge <= 0) return false;

        var myAll = playerManager.GetAllCardInField();
        var currentSet = new HashSet<Card>(duelManager.HeroInTurn ?? new List<Card>());

        foreach (var h in myAll)
        {
            if (h == null || h.turnCompleted || currentSet.Contains(h) || h.IsControlled()) continue;

            for (int i = 0; i < h.Moves.Count; i++)
            {
                if (i == RECHARGE_INDEX) continue;
                if (!h.UsableMovement(i, playerManager)) continue;

                int cost = Mathf.Max(0, h.Moves[i].MoveSO.EnergyCost);
                if (cost >= overflowAfterRecharge) return true;
            }
        }

        return false;
    }

    private int FallbackUsableMoveSoftEnergy(Card card, ref int currentEnergy)
    {
        for (int i = 0; i < card.Moves.Count; i++)
        {
            if (!card.UsableMovement(i, playerManager)) continue;
            int cost = Mathf.Max(0, card.Moves[i].MoveSO.EnergyCost);
            if (cost > currentEnergy) continue;
            if (WouldMoveBeLethal(card, i, out var _))
            {
                currentEnergy -= cost;
                return i;
            }
        }

        int bestIdx = -1; float bestScore = float.NegativeInfinity;
        for (int i = 0; i < card.Moves.Count; i++)
        {
            if (!card.UsableMovement(i, playerManager)) continue;

            if (i == RECHARGE_INDEX && currentEnergy > energySoftTarget && !ShouldRechargeEvenIfAboveSoftTarget(currentEnergy))
                continue;

            var m = card.Moves[i].MoveSO;
            if (m.EnergyCost > currentEnergy) continue;

            float baseScore = 0f;

            if (!m.NeedTarget)
            {
                var enemyMgr = duelManager.GetOpposingPlayerManager(playerManager);
                bool enemyFieldEmpty = enemyMgr.GetAllCardInField().Count == 0;

                if (!enemyFieldEmpty)
                {
                    var affected = duelManager.ObtainTargets(card, i);
                    if (affected != null && affected.Count > 0)
                    {
                        int totalHp = 0;
                        foreach (var e in affected)
                        {
                            var realT = ResolveProtectorForSim(e) ?? e;

                            int raw = duelManager.CalculateAttackDamage(card, i, realT);
                            int absorbed = realT.GetDamageAbsorbed();
                            int effective = Mathf.Max(0, raw - absorbed);
                            if (effective <= 0) continue;

                            int defRem = realT.CurrentDefensePoints;
                            int defIgn = duelManager.CalculateDefenseIgnored(card, realT, i);
                            int defCons = Mathf.Max(0, defRem - Mathf.Max(0, defIgn));
                            int usedOnDef = Mathf.Min(defCons, effective);
                            int hp = effective - usedOnDef;

                            if (hp > 0) totalHp += hp;
                        }
                        baseScore = Mathf.Max(baseScore, totalHp);
                    }
                }
                else
                {
                    if (m.Damage > 0 && card.UsableMovement(i, playerManager))
                    {
                        int dmg = duelManager.CalculateAttackDamage(card, i, null);
                        baseScore = Mathf.Max(baseScore, dmg);
                    }
                }
            }
            else
            {
                var legal = duelManager.ObtainTargets(card, i);
                bool didHp = false;

                if (legal != null && legal.Count > 0)
                {
                    foreach (var enemy in legal)
                    {
                        var realT = ResolveProtectorForSim(enemy) ?? enemy;

                        int raw = duelManager.CalculateAttackDamage(card, i, realT);
                        int absorbed = realT.GetDamageAbsorbed();
                        int effective = Mathf.Max(0, raw - absorbed);
                        if (effective <= 0) continue;

                        int defRem = realT.CurrentDefensePoints;
                        int defIgn = duelManager.CalculateDefenseIgnored(card, realT, i);
                        int defCons = Mathf.Max(0, defRem - Mathf.Max(0, defIgn));
                        int usedOnDef = Mathf.Min(defCons, effective);
                        int hp = effective - usedOnDef;

                        if (hp > 0)
                        {
                            float util = hp;
                            if (m.MoveEffect != null) util += m.MoveEffect.effectScore * 0.6f;
                            if (util > baseScore) baseScore = util;
                            didHp = true;
                        }
                    }
                }
                else
                {
                    var enemyMgr = duelManager.GetOpposingPlayerManager(playerManager);
                    bool enemyFieldEmpty = enemyMgr.GetAllCardInField().Count == 0;

                    if (enemyFieldEmpty && m.Damage > 0 && card.UsableMovement(i, playerManager))
                    {
                        int dmg = duelManager.CalculateAttackDamage(card, i, null);
                        baseScore = Mathf.Max(baseScore, dmg);
                        didHp = true;
                    }
                }

                if (!didHp && m.MoveEffect != null) baseScore += m.MoveEffect.effectScore;
            }

            int leftover = currentEnergy - Mathf.Max(0, m.EnergyCost);
            if (leftover > energySoftTarget) baseScore -= (leftover - energySoftTarget) * softOverflowPenaltyPerPoint;

            if (baseScore > bestScore) { bestScore = baseScore; bestIdx = i; }
        }

        if (bestIdx != -1)
        {
            currentEnergy -= Mathf.Max(0, card.Moves[bestIdx].MoveSO.EnergyCost);
            return bestIdx;
        }

        if (currentEnergy <= energySoftTarget && card.Moves.Count > RECHARGE_INDEX && card.UsableMovement(RECHARGE_INDEX, playerManager))
            return RECHARGE_INDEX;

        int cheapest = -1, bestCost = int.MaxValue;
        for (int i = 0; i < card.Moves.Count; i++)
        {
            if (i == RECHARGE_INDEX) continue;
            if (!card.UsableMovement(i, playerManager)) continue;
            int cost = Mathf.Max(0, card.Moves[i].MoveSO.EnergyCost);
            if (cost >= 0 && cost < bestCost) { bestCost = cost; cheapest = i; }
        }
        return (cheapest != -1) ? cheapest : Mathf.Min(card.Moves.Count - 1, 0);
    }

    private int PickMoveOnTheFly(Card card, ref int currentEnergy)
    {
        int lethalIdx = -1; float lethalScore = float.NegativeInfinity;
        for (int i = 0; i < card.Moves.Count; i++)
        {
            if (!card.UsableMovement(i, playerManager)) continue;
            var move = card.Moves[i].MoveSO;
            if (move.EnergyCost > currentEnergy) continue;

            if (move.NeedTarget)
            {
                var legalTargets = duelManager.ObtainTargets(card, i);
                if (legalTargets == null || legalTargets.Count == 0) continue;

                foreach (var enemy in legalTargets)
                {
                    var realT = ResolveProtectorForSim(enemy) ?? enemy;

                    int raw = duelManager.CalculateAttackDamage(card, i, realT);
                    int absorbed = realT.GetDamageAbsorbed();
                    int effective = Mathf.Max(0, raw - absorbed);
                    if (effective <= 0) continue;

                    int defRem = realT.CurrentDefensePoints;
                    int defIgn = duelManager.CalculateDefenseIgnored(card, realT, i);
                    int defCons = Mathf.Max(0, defRem - Mathf.Max(0, defIgn));
                    int usedOnDef = Mathf.Min(defCons, effective);
                    int hp = effective - usedOnDef;

                    if (hp >= realT.CurrentHealtPoints)
                    {
                        float s = 100000f - (hp - realT.CurrentHealtPoints) * overkillPenaltyFactor;
                        if (s > lethalScore) { lethalScore = s; lethalIdx = i; }
                    }
                }
            }
            else
            {
                var enemyMgr = duelManager.GetOpposingPlayerManager(playerManager);
                bool enemyFieldEmpty = enemyMgr.GetAllCardInField().Count == 0;

                if (!enemyFieldEmpty)
                {
                    var affected = duelManager.ObtainTargets(card, i);
                    if (affected != null && affected.Count > 0)
                    {
                        int totalHp = 0;
                        foreach (var e in affected)
                        {
                            var realT = ResolveProtectorForSim(e) ?? e;

                            int raw = duelManager.CalculateAttackDamage(card, i, realT);
                            int absorbed = realT.GetDamageAbsorbed();
                            int effective = Mathf.Max(0, raw - absorbed);
                            if (effective <= 0) continue;

                            int defRem = realT.CurrentDefensePoints;
                            int defIgn = duelManager.CalculateDefenseIgnored(card, realT, i);
                            int defCons = Mathf.Max(0, defRem - Mathf.Max(0, defIgn));
                            int usedOnDef = Mathf.Min(defCons, effective);
                            int hp = effective - usedOnDef;

                            if (hp > 0) totalHp += hp;
                        }
                    }
                }
            }
        }
        if (lethalIdx != -1)
        {
            currentEnergy -= Mathf.Max(0, card.Moves[lethalIdx].MoveSO.EnergyCost);
            return lethalIdx;
        }

        int bestIdx = -1; float bestScore = float.NegativeInfinity;
        for (int i = 0; i < card.Moves.Count; i++)
        {
            if (!card.UsableMovement(i, playerManager)) continue;

            if (i == RECHARGE_INDEX && currentEnergy > energySoftTarget && !ShouldRechargeEvenIfAboveSoftTarget(currentEnergy))
                continue;

            var m = card.Moves[i].MoveSO;
            if (m.EnergyCost > currentEnergy) continue;

            float baseScore = 0f;

            if (!m.NeedTarget)
            {
                var enemyMgr = duelManager.GetOpposingPlayerManager(playerManager);
                bool enemyFieldEmpty = enemyMgr.GetAllCardInField().Count == 0;

                if (!enemyFieldEmpty)
                {
                    var affected = duelManager.ObtainTargets(card, i);
                    if (affected != null && affected.Count > 0)
                    {
                        int totalHp = 0;
                        foreach (var e in affected)
                        {
                            var realT = ResolveProtectorForSim(e) ?? e;

                            int raw = duelManager.CalculateAttackDamage(card, i, realT);
                            int absorbed = realT.GetDamageAbsorbed();
                            int effective = Mathf.Max(0, raw - absorbed);
                            if (effective <= 0) continue;

                            int defRem = realT.CurrentDefensePoints;
                            int defIgn = duelManager.CalculateDefenseIgnored(card, realT, i);
                            int defCons = Mathf.Max(0, defRem - Mathf.Max(0, defIgn));
                            int usedOnDef = Mathf.Min(defCons, effective);
                            int hp = effective - usedOnDef;

                            if (hp > 0) totalHp += hp;
                        }
                        baseScore = Mathf.Max(baseScore, totalHp);
                    }
                }
                else
                {
                    if (m.Damage > 0 && card.UsableMovement(i, playerManager))
                    {
                        int dmg = duelManager.CalculateAttackDamage(card, i, null);
                        baseScore = Mathf.Max(baseScore, dmg);
                    }
                }
            }
            else
            {
                var legal = duelManager.ObtainTargets(card, i);
                bool didHp = false;

                if (legal != null && legal.Count > 0)
                {
                    foreach (var enemy in legal)
                    {
                        var realT = ResolveProtectorForSim(enemy) ?? enemy;

                        int raw = duelManager.CalculateAttackDamage(card, i, realT);
                        int absorbed = realT.GetDamageAbsorbed();
                        int effective = Mathf.Max(0, raw - absorbed);
                        if (effective <= 0) continue;

                        int defRem = realT.CurrentDefensePoints;
                        int defIgn = duelManager.CalculateDefenseIgnored(card, realT, i);
                        int defCons = Mathf.Max(0, defRem - Mathf.Max(0, defIgn));
                        int usedOnDef = Mathf.Min(defCons, effective);
                        int hp = effective - usedOnDef;

                        if (hp > 0)
                        {
                            float util = hp;
                            if (m.MoveEffect != null) util += m.MoveEffect.effectScore * 0.6f;
                            if (util > baseScore) baseScore = util;
                            didHp = true;
                        }
                    }
                }
                else
                {
                    var enemyMgr = duelManager.GetOpposingPlayerManager(playerManager);
                    bool enemyFieldEmpty = enemyMgr.GetAllCardInField().Count == 0;

                    if (enemyFieldEmpty && m.Damage > 0 && card.UsableMovement(i, playerManager))
                    {
                        int dmg = duelManager.CalculateAttackDamage(card, i, null);
                        baseScore = Mathf.Max(baseScore, dmg);
                        didHp = true;
                    }
                }

                if (!didHp && m.MoveEffect != null) baseScore += m.MoveEffect.effectScore;
            }

            int leftover = currentEnergy - Mathf.Max(0, m.EnergyCost);
            if (leftover > energySoftTarget) baseScore -= (leftover - energySoftTarget) * softOverflowPenaltyPerPoint;

            if (baseScore > bestScore) { bestScore = baseScore; bestIdx = i; }
        }

        if (bestIdx == -1)
        {
            if (currentEnergy <= energySoftTarget && card.Moves.Count > RECHARGE_INDEX && card.UsableMovement(RECHARGE_INDEX, playerManager))
                bestIdx = RECHARGE_INDEX;
            else
            {
                int cheapest = -1, bestCost = int.MaxValue;
                for (int i = 0; i < card.Moves.Count; i++)
                {
                    if (i == RECHARGE_INDEX) continue;
                    if (!card.UsableMovement(i, playerManager)) continue;
                    int cost = Mathf.Max(0, card.Moves[i].MoveSO.EnergyCost);
                    if (cost >= 0 && cost < bestCost) { bestCost = cost; cheapest = i; }
                }
                bestIdx = (cheapest != -1) ? cheapest : 0;
            }
        }

        currentEnergy -= Mathf.Max(0, card.Moves[bestIdx].MoveSO.EnergyCost);
        return bestIdx;
    }

    // ---------- Pequeñas utilidades de dificultad ----------
    private bool Chance(float p) => p > 0f && Random.value < p;

    private int PickMistakeMove(Card card, int currentEnergyAvailable)
    {
        // Preferir "Recarga" si es posible (y la energía no está topeada)
        if (card.Moves.Count > RECHARGE_INDEX && card.UsableMovement(RECHARGE_INDEX, playerManager) && currentEnergyAvailable <= 90)
            return RECHARGE_INDEX;

        // Si no, elegir el movimiento usable de MENOR coste (y, a igualdad, menor daño)
        int pick = -1;
        int bestCost = int.MaxValue;
        int bestDamage = int.MaxValue;

        for (int i = 0; i < card.Moves.Count; i++)
        {
            if (!card.UsableMovement(i, playerManager)) continue;
            var m = card.Moves[i].MoveSO;
            int cost = Mathf.Max(0, m.EnergyCost);
            if (cost > currentEnergyAvailable) continue;

            int dmg = Mathf.Max(0, m.Damage);
            if (cost < bestCost || (cost == bestCost && dmg < bestDamage))
            {
                bestCost = cost; bestDamage = dmg; pick = i;
            }
        }

        return pick;
    }
}
