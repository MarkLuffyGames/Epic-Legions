using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TurnSimulator
{
    private EnhancedHemeraLegionAI ai;
    private bool showDebugLogs;
    public TurnSimulator(EnhancedHemeraLegionAI ai, bool showDebugLogs)
    {
        this.ai = ai;
        this.showDebugLogs = showDebugLogs;
    }

    public FullPlanSim SimulateFullTurn(SimSnapshot initialSnap, List<(SimCardState hero, int moveIndex, int targetPosition)> plannedActions,
MovementSimulator movementSimulator, EnhancedHemeraLegionAI ai)
    {
        var snap = initialSnap.Clone();
        var result = new FullPlanSim();

        if (showDebugLogs)
        {
            Debug.Log($"⏰ INICIANDO SIMULACIÓN - Subturno: {snap.CurrentSubTurn} " +
                     $"(Original: {snap.OriginalSubTurn})");
            Debug.Log($"Estado inicial - Vida IA: {snap.MyLife}, Vida Enemigo: {snap.EnemyLife}, Energía: {snap.MyEnergy}");
            Debug.Log($"Héroes controlados: {snap.MyControlledHeroes.Count}, Héroes enemigos: {snap.EnemyHeroes.Count}");
            Debug.Log($"Acciones planificadas: {plannedActions.Count}");
        }

        //Organizar por subturnos con mapeo correcto
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

        if (showDebugLogs)
        {
            Debug.Log($"📊 Mapeo - DuelManager subturn {snap.CurrentSubTurn} -> nuestro índice {startOurIndex}");
            Debug.Log($"📊 Total grupos velocidad: {allSubturns.Count}");
        }

        int totalActionsExecuted = 0;
        int invalidActions = 0;

        // Usar nuestro índice mapeado
        for (int ourIndex = startOurIndex; ourIndex < allSubturns.Count; ourIndex++)
        {
            if (snap.EnemyLife <= 0)
            {
                if (showDebugLogs) Debug.Log("🎉 VICTORIA - Enemigo derrotado");
                break;
            }

            //Actualizar el CurrentSubTurn con el valor correspondiente del mapeo inverso
            snap.CurrentSubTurn = GetDuelManagerTurnFromOurIndex(ourIndex, turnMapping);

            // AVANZAR SUBTURNO antes de procesar acciones
            AdvanceSubTurn(snap);

            if (showDebugLogs)
            {
                Debug.Log($"🔄 Subturno simulado {snap.CurrentSubTurn} " +
                         $"(Total pasados: {snap.SubTurnsPassedInSimulation})");
            }


            var currentSubturn = allSubturns[ourIndex];

            if (showDebugLogs)
            {
                Debug.Log($"🔄 Grupo velocidad {ourIndex}: {currentSubturn.Count} héroes");
            }

            // Buscar y separar acciones para este subturno
            var positiveActions = new List<(SimCardState hero, int moveIndex, int targetPosition)>();
            var otherActions = new List<(SimCardState hero, int moveIndex, int targetPosition)>();

            foreach (var heroState in currentSubturn)
            {
                if (!heroState.ControllerIsMine || !heroState.Alive) continue;

                var plannedAction = plannedActions.FirstOrDefault(a => a.hero == heroState);

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

            if (showDebugLogs)
                Debug.Log($"  - Efectos positivos: {positiveActions.Count}, Otros: {otherActions.Count}");

            // Ejecutar acciones positivas
            foreach (var action in positiveActions)
            {
                if (ExecuteActionWithLogs(snap, action, result, movementSimulator, ref totalActionsExecuted, ref invalidActions))
                    break; // Si la acción causó victoria o no es valida.
            }

            // Ejecutar otras acciones
            foreach (var action in otherActions)
            {
                if (ExecuteActionWithLogs(snap, action, result, movementSimulator, ref totalActionsExecuted, ref invalidActions))
                    break; // Si la acción causó victoria o no es valida.
            }

            if (invalidActions > 0) break; // Si hubo una acción inválida, detener la simulación.
        }

        if (showDebugLogs)
        {
            Debug.Log($"📊 RESUMEN SIMULACIÓN:");
            Debug.Log($"  - Acciones ejecutadas: {totalActionsExecuted}");
            Debug.Log($"  - Acciones inválidas: {invalidActions}");
            Debug.Log($"  - Vida enemigo final: {snap.EnemyLife}");
            Debug.Log($"  - Energía final: {snap.MyEnergy}");
        }

        result.FinalEnergy = snap.MyEnergy;
        result.CalculateScore(showDebugLogs);

        return result;
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

    private int GetDuelManagerTurnFromOurIndex(int ourIndex, Dictionary<int, int> turnMapping)
    {
        foreach (var mapping in turnMapping)
        {
            if (mapping.Value == ourIndex)
                return mapping.Key;
        }
        return ourIndex; // Fallback
    }

    private bool ExecuteActionWithLogs(SimSnapshot snap, (SimCardState hero, int moveIndex, int targetPosition) action,
                                      FullPlanSim result, MovementSimulator movementSimulator,
                                      ref int totalActionsExecuted, ref int invalidActions)
    {
        var (hero, moveIndex, targetPosition) = action;
        var move = hero.moves[moveIndex];

        if (showDebugLogs)
        {
            string targetInfo = move.MoveSO.NeedTarget ?
                (targetPosition == -1 ? "VIDA" : $"POS{targetPosition}") : "AUTO";
            Debug.Log($"  🎮 {hero.OriginalCard.cardSO.CardName} -> {move.MoveSO.MoveName} [{targetInfo}]");
        }

        var energyBefore = snap.MyEnergy;
        var actionsBefore = result.Actions.Count;
        var enemyLifeBefore = snap.EnemyLife;

        movementSimulator.SimUseMovement(snap, hero, moveIndex, targetPosition, result);

        bool actionExecuted = (snap.MyEnergy != energyBefore) || (result.Actions.Count != actionsBefore);

        if (actionExecuted)
        {
            totalActionsExecuted++;
            if (showDebugLogs)
            {
                int damageDealt = enemyLifeBefore - snap.EnemyLife;
                if (damageDealt > 0)
                    Debug.Log($"    💥 Daño infligido: {damageDealt}");
            }
        }
        else
        {
            invalidActions++;
            if (showDebugLogs)
                Debug.Log($"    ❌ ACCIÓN DESCARTADA");
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
}