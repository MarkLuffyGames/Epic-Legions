using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class PlanGenerator
{
    bool _showDebugInfo;
    Dictionary<SimSnapshot, FullPlanSim> plans = new();
    private MovementSimulator movementSimulator = new MovementSimulator();
    public PlanGenerator(bool showDebugInfo, MovementSimulator movementSimulator)
    {
        _showDebugInfo = showDebugInfo;
        this.movementSimulator = movementSimulator;
    }

    private void Log(string message)
    {
        if (_showDebugInfo) Debug.Log(message);
    }
    public FullPlanSim GenerateBestPlan(SimSnapshot originalSnapshot)
    {
        var snap = originalSnapshot.Clone();

        var (allSubturns, turnMapping) = BuildFutureTurnScheduleWithMapping(snap);

        //Encontrar el índice correcto para empezar
        int startOurIndex = 0;
        if (turnMapping.ContainsKey(snap.CurrentSubTurn))
        {
            startOurIndex = turnMapping[snap.CurrentSubTurn];
        }
        else
        {
            // Buscar el siguiente subturno válido después del CurrentSubTurn
            for (int i = snap.CurrentSubTurn + 1; i < 21; i++)
            {
                if (turnMapping.ContainsKey(i))
                {
                    startOurIndex = turnMapping[i];
                    break;
                }
            }
        }

        Log($"📊 Mapeo - DuelManager subturn {snap.CurrentSubTurn} -> mi índice {startOurIndex}");
        Log($"📊 Total grupos velocidad: {allSubturns.Count}");

        Debug.Log(allSubturns[0].Count);

        GenerateInitialPlans(snap.Clone(), allSubturns[0]);

        for (int ourIndex = startOurIndex; ourIndex < allSubturns.Count; ourIndex++)
        {
            if (ourIndex != startOurIndex) ContinuePlans(allSubturns[ourIndex]);
            SimSubturns(ourIndex, allSubturns[ourIndex], turnMapping);
        }

        foreach (var plan in plans)
        {
            var result = plan.Value;
            result.FinalEnergy = snap.MyEnergy;
            result.CalculateScore(0, _showDebugInfo);
        }

        return plans.OrderByDescending(p => p.Value.Score).First().Value;
    }

    private void GenerateInitialPlans(SimSnapshot snap, List<SimCardState> currentSubturnHeroes)
    {
        var actions = new List<List<(SimCardState hero, int moveIndex, int targetPosition)>>();

        for (int i = 0; i < currentSubturnHeroes.Count; i++)
        {
            if (snap.MyControlledHeroes.FirstOrDefault(a => a.OriginalCard == currentSubturnHeroes[i].OriginalCard) == null)
                continue;
            if (!currentSubturnHeroes[i].CanAct())
                continue;

            var heroActions = GetValidActionsForHero(snap, currentSubturnHeroes[i]);
            actions.Add(heroActions);
        }
        Debug.Log($"📊 Acciones válidas obtenidas para héroes del subturno inicial: {actions.Count}");

        var combs = GenerateActionCombinations(actions);
        Debug.Log($"📊 Combinaciones iniciales generadas: {combs.Count}");

        foreach (var comb in combs)
        {
            var sim = new FullPlanSim();
            foreach (var action in comb)
            {
                sim.AddAction(action.hero, action.moveIndex, action.targetPosition);
            }
            plans[snap.Clone()] = sim;
        }

        Debug.Log($"📊 Planes iniciales generados: {plans.Count}");
    }

    private void ContinuePlans(List<SimCardState> currentSubturnHeroes)
    {
        foreach (var planEntry in plans)
        {
            var snap = planEntry.Key;
            var plan = planEntry.Value;

            var actions = new List<List<(SimCardState hero, int moveIndex, int targetPosition)>>();

            for (int i = 0; i < currentSubturnHeroes.Count; i++)
            {
                if (snap.MyControlledHeroes.FirstOrDefault(a => a.OriginalCard == currentSubturnHeroes[i].OriginalCard) == null)
                    continue;
                if (!currentSubturnHeroes[i].CanAct())
                    continue;

                var heroActions = GetValidActionsForHero(snap, currentSubturnHeroes[i]);
                actions.Add(heroActions);
            }

            var combs = GenerateActionCombinations(actions);

            for (int i = 0; i < combs.Count; i++)
            {
                foreach (var action in combs[i])
                {
                    if (i == 0)// Reutilizar el plan existente para la primera combinación
                    {
                        plan.AddAction(action.hero, action.moveIndex, action.targetPosition);
                    }
                    else// Clonar el plan existente para las demás combinaciones
                    {
                        var newPlan = plan.Clone();
                        newPlan.AddAction(action.hero, action.moveIndex, action.targetPosition);
                        plans[snap.Clone()] = newPlan;
                    }
                }
            }
        }
    }

    private void SimSubturns(int ourIndex, List<SimCardState> currentSubturn, Dictionary<int, int> turnMapping)
    {
        foreach (var planEntry in plans)
        {
            var snap = planEntry.Key;
            var plan = planEntry.Value;

            plan.totalActionsExecuted = 0;
            plan.invalidActions = 0;

            //Actualizar el CurrentSubTurn con el valor correspondiente del mapeo inverso
            snap.CurrentSubTurn = GetDuelManagerTurnFromOurIndex(ourIndex, turnMapping);

            // AVANZAR SUBTURNO antes de procesar acciones
            AdvanceSubTurn(snap);

            Log($"🔄 Subturno simulado {snap.CurrentSubTurn} " +
                         $"(Total pasados: {snap.SubTurnsPassedInSimulation})");

            Log($"🔄 Grupo velocidad {ourIndex}: {currentSubturn.Count} héroes");


            // Buscar y separar acciones para este subturno
            var positiveActions = new List<(SimCardState hero, int moveIndex, int targetPosition)>();
            var otherActions = new List<(SimCardState hero, int moveIndex, int targetPosition)>();

            foreach (var heroState in currentSubturn)
            {
                if (!heroState.ControllerIsMine || !heroState.Alive) continue;

                var plannedAction = plan.Actions.FirstOrDefault(a => a.hero == heroState);

                if (plannedAction.hero != null && plannedAction.moveIndex >= 0)
                {
                    var move = plannedAction.hero.moves[plannedAction.moveIndex];

                    if (move.MoveSO.MoveType == MoveType.PositiveEffect)
                    {
                        positiveActions.Add(plannedAction);
                    }
                    else
                    {
                        otherActions.Add(plannedAction);
                    }
                }
            }

            Log($"  - Efectos positivos: {positiveActions.Count}, Otros: {otherActions.Count}");

            // Ejecutar acciones positivas
            foreach (var action in positiveActions)
            {
                ExecuteAction(snap, action, plan, movementSimulator);
            }

            // Ejecutar otras acciones
            foreach (var action in otherActions)
            {
                ExecuteAction(snap, action, plan, movementSimulator);
            }
        }
    }

    private bool ExecuteAction(SimSnapshot snap, (SimCardState hero, int moveIndex, int targetPosition) action,
                                      FullPlanSim result, MovementSimulator movementSimulator)
    {
        var (hero, moveIndex, targetPosition) = action;
        var move = hero.moves[moveIndex];

        string targetInfo = move.MoveSO.NeedTarget ?
                (targetPosition == -1 ? "VIDA" : $"POS{targetPosition}") : "AUTO";
        Log($"  🎮 {hero.OriginalCard.cardSO.CardName} -> {move.MoveSO.MoveName} [{targetInfo}]");

        var energyBefore = snap.MyEnergy;
        var actionsBefore = result.Actions.Count;
        var enemyLifeBefore = snap.EnemyLife;

        movementSimulator.SimUseMovement(snap, hero, moveIndex, targetPosition, result);

        bool actionExecuted = (snap.MyEnergy != energyBefore) || (result.Actions.Count != actionsBefore);

        if (actionExecuted)
        {
            result.totalActionsExecuted++;
            int damageDealt = enemyLifeBefore - snap.EnemyLife;
            if (damageDealt > 0)
                Log($"    💥 Daño infligido: {damageDealt}");
        }
        else
        {
            result.invalidActions++;
            Log($"    ❌ ACCIÓN DESCARTADA");
            return true; // Detener si la acción no fue válida.
        }

        return snap.EnemyLife <= 0;
    }

    private void AdvanceSubTurn(SimSnapshot snap)
    {
        snap.SubTurnsPassedInSimulation = snap.CurrentSubTurn - snap.OriginalSubTurn;

        // Actualizar efectos temporales
        UpdateTemporalEffects(snap);
    }

    private void UpdateTemporalEffects(SimSnapshot snap)
    {
        foreach (var kvp in snap.CardStates)
        {
            var card = kvp.Key;
            var effects = kvp.Value;

            // Remover efectos expirados
            effects.ManageEffects(snap.SubTurnsPassedInSimulation);
        }
    }

    private List<(SimCardState hero, int moveIndex, int targetPosition)> GetValidActionsForHero(
        SimSnapshot snap, SimCardState hero)
    {
        var validAction = new List<(SimCardState hero, int moveIndex, int targetPosition)>();

        for (int moveIndex = 0; moveIndex < hero.moves.Count; moveIndex++)
        {
            var move = hero.moves[moveIndex];
            
            if (move.MoveSO.EnergyCost > snap.MyEnergy)
                continue;

            if (move.MoveSO.NeedTarget)
            {
                var targets = SimObtainTargets(snap, hero, moveIndex);

                if(targets.Count == 0)
                {
                    Log($"No hay enemigos en el campo, atacar directo a vida");
                    validAction.Add((hero, moveIndex, -1));
                    continue;
                }
                foreach (var target in targets)
                {
                    validAction.Add((hero, moveIndex, target.FieldIndex));
                }
            }
            else
            {
                validAction.Add((hero, moveIndex, -1));
            }
        }

        return validAction;
    }

    public List<SimCardState> SimObtainTargets(SimSnapshot snap, SimCardState attacker, int moveIndex)
    {
        var targets = new List<SimCardState>();
        var move = attacker.moves[moveIndex];

        Log($"Buscando objetivos para {attacker.OriginalCard.cardSO.CardName} -> {move.MoveSO.MoveName}");

        // Determinar qué jugador es el objetivo
        List<SimCardState> potentialTargets;

        if (move.MoveSO.MoveType != MoveType.PositiveEffect)
        {
            // Ataque: buscar enemigos
            potentialTargets = snap.EnemyHeroes.Where(e => e.Alive).ToList();
            Log($"Potenciales objetivos enemigos: {potentialTargets.Count}");
        }
        else
        {
            // Efecto positivo: buscar aliados (excluyéndose a sí mismo)
            potentialTargets = snap.MyControlledHeroes.Where(a => a.Alive && a != attacker).ToList();
            Log($"Potenciales objetivos aliados: {potentialTargets.Count}");
        }

        // Filtrar por línea de visión
        foreach (var target in potentialTargets)
        {
            if (IsValidSimTarget(snap, attacker, target, moveIndex))
            {
                targets.Add(target);
            }
        }

        Log($"Objetivos válidos encontrados: {targets.Count}");

        return targets;
    }

    public bool IsValidSimTarget(SimSnapshot snap, SimCardState attacker, SimCardState target, int moveIndex)
    {
        var move = attacker.moves[moveIndex];
        var moveSO = move.MoveSO;

        if (moveSO.TargetsCondition == null
            || !moveSO.TargetsCondition.CheckCondition(attacker, target)) return false;

        // Verificar que el objetivo esté vivo
        if (!target.Alive) return false;
        Log("Objetivo vivo");

        // 1. Verificar si el atacante es de clase Hunter y el movimiento es de rango
        bool isHunterRanged = IsHunterRangedAttack(attacker, moveSO);
        Log($"Atacante es Cazador: {isHunterRanged}");

        // 2. Verificar si el atacante es de clase Assassin (puede atacar por detrás)
        bool isAssassin = IsAssassin(attacker);
        Log($"Atacante es Asesino: {isHunterRanged}");

        // 3. Verificar línea de visión (si hay héroes delante protegiendo)
        bool hasLineOfSight = HasLineOfSightToTarget(snap, attacker, target, isHunterRanged, isAssassin);
        Log($"Línea de visión al objetivo: {hasLineOfSight}");

        return hasLineOfSight;
    }

    private bool IsHunterRangedAttack(SimCardState attacker, MoveSO moveSO)
    {
        var heroSO = attacker.OriginalCard.cardSO as HeroCardSO;
        return heroSO != null &&
               heroSO.HeroClass == HeroClass.Hunter &&
               moveSO.MoveType == MoveType.RangedAttack;
    }

    private bool IsAssassin(SimCardState attacker)
    {
        var heroSO = attacker.OriginalCard.cardSO as HeroCardSO;
        return heroSO != null && heroSO.HeroClass == HeroClass.Assassin;
    }

    private bool HasLineOfSightToTarget(SimSnapshot snap, SimCardState attacker, SimCardState target, bool isHunterRanged, bool isAssassin)
    {
        int targetPos = target.FieldIndex;

        // Si es Hunter con ataque a distancia, puede atacar a cualquier objetivo
        if (isHunterRanged) return true;

        // Para Assassin: puede atacar desde cualquier dirección
        if (isAssassin)
        {
            return CanAttackFromFront(snap, targetPos) || CanAttackFromBehind(snap, targetPos);
        }

        // Lógica normal: solo ataque frontal
        return CanAttackFromFront(snap, targetPos);
    }

    private bool CanAttackFromFront(SimSnapshot snap, int targetPos)
    {
        int targetRow = targetPos / 5;

        if (targetRow == 0) return true; // Fila delantera - siempre visible

        if (targetRow == 1) // Fila media
        {
            int frontPosition = targetPos - 5;
            return !IsPositionOccupied(snap, frontPosition);
        }

        if (targetRow == 2) // Fila trasera
        {
            int midPosition = targetPos - 5;
            int frontPosition = targetPos - 10;
            return !IsPositionOccupied(snap, midPosition) && !IsPositionOccupied(snap, frontPosition);
        }

        return false;
    }

    private bool CanAttackFromBehind(SimSnapshot snap, int targetPos)
    {
        int targetRow = targetPos / 5;

        if (targetRow == 2) return true; // Fila trasera - siempre visible desde atrás

        if (targetRow == 1) // Fila media
        {
            int backPosition = targetPos + 5;
            return !IsPositionOccupied(snap, backPosition);
        }

        if (targetRow == 0) // Fila delantera
        {
            int midPosition = targetPos + 5;
            int backPosition = targetPos + 10;
            return !IsPositionOccupied(snap, midPosition) && !IsPositionOccupied(snap, backPosition);
        }

        return false;
    }

    private bool IsPositionOccupied(SimSnapshot snap, int position)
    {
        // Verificar que la posición sea válida
        if (position < 0 || position > 14) return false;

        // Buscar si hay algún héroe vivo en esta posición
        foreach (var state in snap.CardStates.Values)
        {
            if (state.Alive && state.FieldIndex == position)
            {
                return true;
            }
        }
        return false;
    }

    private SimCardState FindCardByPosition(SimSnapshot snap, int positionIndex, MoveType moveType)
    {
        if (moveType != MoveType.PositiveEffect)
        {
            foreach (var hero in snap.EnemyHeroes)
            {
                if (hero.FieldIndex == positionIndex && hero.Alive)
                    return hero;
            }
        }
        else
        {
            foreach (var hero in snap.MyControlledHeroes)
            {
                if (hero.FieldIndex == positionIndex && hero.Alive)
                    return hero;
            }
        }

        return null;
    }

    private (List<List<SimCardState>> schedule, Dictionary<int, int> turnMapping) BuildFutureTurnScheduleWithMapping(SimSnapshot snap)
    {
        var schedule = new List<List<SimCardState>>();
        var turnMapping = new Dictionary<int, int>(); // Mapeo: DuelManager subturn -> nuestro índice

        var speedGroups = new Dictionary<int, List<SimCardState>>();

        var allAliveHeroes = snap.MyControlledHeroes
            .Where(h => h.Alive)
            .Concat(snap.EnemyHeroes.Where(h => h.Alive))
            .ToList();

        // Calcular el turno para cada héroe usando la MISMA fórmula que DuelManager
        foreach (var heroState in allAliveHeroes)
        {
            int turnIndex = (100 - heroState.GetEffectiveSpeed()) / 5;
            turnIndex = Math.Clamp(turnIndex, 0, 20);

            if (!speedGroups.ContainsKey(turnIndex))
                speedGroups[turnIndex] = new List<SimCardState>();

            speedGroups[turnIndex].Add(heroState);
        }

        // Crear mapeo y schedule
        int ourIndex = 0;
        for (int duelManagerTurn = 0; duelManagerTurn < 21; duelManagerTurn++)
        {
            if (speedGroups.ContainsKey(duelManagerTurn) && speedGroups[duelManagerTurn].Count > 0)
            {
                schedule.Add(speedGroups[duelManagerTurn]);
                turnMapping[duelManagerTurn] = ourIndex;
                ourIndex++;
            }
        }

        return (schedule, turnMapping);
    }

    private List<List<(SimCardState hero, int moveIndex, int targetPosition)>> GenerateActionCombinations(
        List<List<(SimCardState hero, int moveIndex, int targetPosition)>> heroActionsList)
    {
        var combinations = new List<List<(SimCardState, int, int)>>();

        // Generar combinaciones usando enfoque iterativo
        if (heroActionsList.Count == 0)
            return combinations;

        // Inicializar contadores (uno por héroe)
        int[] indices = new int[heroActionsList.Count];

        while (true)
        {
            // Crear combinación actual
            var currentCombination = new List<(SimCardState, int, int)>();
            for (int i = 0; i < heroActionsList.Count; i++)
            {
                currentCombination.Add(heroActionsList[i][indices[i]]);
            }
            combinations.Add(currentCombination);

            // Encontrar el próximo índice a incrementar
            int j = heroActionsList.Count - 1;
            while (j >= 0)
            {
                indices[j]++;
                if (indices[j] < heroActionsList[j].Count)
                    break;

                indices[j] = 0;
                j--;
            }

            // Si j < 0, hemos terminado todas las combinaciones
            if (j < 0)
                break;
        }

        return combinations;
    }

    private int GetDuelManagerTurnFromOurIndex(int ourIndex, Dictionary<int, int> turnMapping)
    {
        foreach (var mapping in turnMapping)
        {
            if (mapping.Value == ourIndex)
                return mapping.Key;
        }
        return ourIndex; // Fallback
    }
}
