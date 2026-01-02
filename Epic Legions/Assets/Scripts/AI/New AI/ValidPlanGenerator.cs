using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ValidPlanGenerator
{
    private bool showDebugLogs;
    private MovementSimulator movementSimulator;

    // Configurable: cuántas opciones por héroe
    private const int MAX_ACTIONS_PER_HERO = 3;

    public ValidPlanGenerator(bool showDebugLogs)
    {
        this.showDebugLogs = showDebugLogs;
        this.movementSimulator = new MovementSimulator(showDebugLogs);
    }

    public List<List<(SimCardState hero, int moveIndex, int targetPosition)>>
        GeneratePlans(SimSnapshot initialSnapshot)
    {
        var plans = new List<List<(SimCardState, int, int)>>();

        // 1. Obtener orden real de actuación
        var orderedHeroes = GetHeroesInTurnOrder(initialSnapshot);

        if (orderedHeroes.Count == 0)
            return plans;

        Log($"🧠 Generando planes para {orderedHeroes.Count} héroes");

        // 2. Construcción incremental de planes
        plans.Add(new List<(SimCardState, int, int)>());

        foreach (var hero in orderedHeroes)
        {
            var newPlans = new List<List<(SimCardState, int, int)>>();

            foreach (var existingPlan in plans)
            {
                // Snapshot reducido solo para validar energía y reglas
                var simSnap = BuildLightSnapshot(initialSnapshot, existingPlan);

                var possibleActions = GetValidActionsForHero(simSnap, hero);

                foreach (var action in possibleActions)
                {
                    var extendedPlan = new List<(SimCardState, int, int)>(existingPlan)
                    {
                        action
                    };

                    newPlans.Add(extendedPlan);
                }
            }

            plans = newPlans;

            Log($"📊 Planes después de {hero.OriginalCard.cardSO.CardName}: {plans.Count}");
        }

        Log($"✅ Generación finalizada: {plans.Count} planes válidos");
        return plans;
    }

    // ===========================
    // HERO ORDER
    // ===========================
    private List<SimCardState> GetHeroesInTurnOrder(SimSnapshot snapshot)
    {
        var speedGroups = new Dictionary<int, List<SimCardState>>();

        foreach (var hero in snapshot.MyControlledHeroes.Where(h => h.Alive))
        {
            int idx = Math.Clamp((100 - hero.GetEffectiveSpeed()) / 5, 0, 20);

            if (!speedGroups.ContainsKey(idx))
                speedGroups[idx] = new List<SimCardState>();

            speedGroups[idx].Add(hero);
        }

        var ordered = new List<SimCardState>();

        for (int i = 0; i <= 20; i++)
        {
            if (speedGroups.ContainsKey(i))
            {
                ordered.AddRange(speedGroups[i]
                    .OrderBy(h => h.FieldIndex));
            }
        }

        return ordered;
    }

    // ===========================
    // ACTION GENERATION
    // ===========================
    private List<(SimCardState hero, int moveIndex, int targetPosition)>
        GetValidActionsForHero(SimSnapshot snapshot, SimCardState hero)
    {
        var actions = new List<(SimCardState, int, int)>();

        bool hasAliveEnemies = snapshot.EnemyHeroes.Any(e => e.Alive);

        // 1️⃣ ATAQUES
        var attackActions = new List<(SimCardState hero, int moveIndex, int targetPosition, float score)>();

        for (int i = 0; i < hero.moves.Count; i++)
        {
            var move = hero.moves[i];
            if (move.MoveSO.Damage <= 0) continue;
            if (!CanUseMove(snapshot, hero, i)) continue;

            if (move.MoveSO.NeedTarget)
            {
                foreach (var enemy in snapshot.EnemyHeroes.Where(e => e.Alive))
                {
                    if (!movementSimulator.IsValidSimTarget(snapshot, hero, enemy, i))
                        continue;

                    float score = EstimateAttackValue(snapshot, hero, i, enemy);
                    attackActions.Add((hero, i, enemy.FieldIndex, score));
                }
            }
            else
            {
                // Daño directo a vida (solo si no hay enemigos)
                if (!hasAliveEnemies)
                {
                    float score = move.MoveSO.Damage * 5f;
                    attackActions.Add((hero, i, -1, score));
                }
            }
        }

        foreach (var atk in attackActions
            .OrderByDescending(a => a.score)
            .Take(MAX_ACTIONS_PER_HERO))
        {
            actions.Add((atk.hero, atk.moveIndex, atk.targetPosition));
        }

        // 2️⃣ SOPORTE (buff / debuff)
        if (actions.Count < MAX_ACTIONS_PER_HERO)
        {
            for (int i = 0; i < hero.moves.Count; i++)
            {
                var move = hero.moves[i];
                if (move.MoveSO.Damage > 0) continue;
                if (move.MoveSO.MoveEffect is Recharge) continue;
                if (!CanUseMove(snapshot, hero, i)) continue;

                if (!move.MoveSO.NeedTarget)
                {
                    actions.Add((hero, i, -1));
                }
                else
                {
                    foreach (var ally in snapshot.MyControlledHeroes.Where(h => h.Alive))
                    {
                        actions.Add((hero, i, ally.FieldIndex));
                    }
                }

                if (actions.Count >= MAX_ACTIONS_PER_HERO)
                    break;
            }
        }

        // 3️⃣ RECARGA (SIEMPRE DISPONIBLE)
        if (actions.Count == 0 || actions.Count < MAX_ACTIONS_PER_HERO)
        {
            if (hero.moves.Count > 2 &&
                hero.moves[2].MoveSO.MoveEffect is Recharge)
            {
                actions.Add((hero, 2, -1));
            }
        }

        if (actions.Count == 0)
        {
            Log($"⚠️ {hero.OriginalCard.cardSO.CardName} no tiene acciones válidas");
        }

        return actions;
    }

    // ===========================
    // VALIDATION
    // ===========================
    private bool CanUseMove(SimSnapshot snap, SimCardState hero, int moveIndex)
    {
        var move = hero.moves[moveIndex];

        if (!hero.CanAct())
            return false;

        if (move.MoveSO.EnergyCost > snap.MyEnergy)
            return false;

        return true;
    }

    private float EstimateAttackValue(
        SimSnapshot snap,
        SimCardState hero,
        int moveIndex,
        SimCardState target)
    {
        var move = hero.moves[moveIndex];
        int estimatedDamage =
            movementSimulator.EstimateQuickDamage(hero, target, moveIndex);

        int defense = target.GetEffectiveDefense();
        int lifeDamage = Math.Max(0, estimatedDamage - defense);

        if (lifeDamage > 0)
            return lifeDamage * 5f;

        return -estimatedDamage * 2f;
    }

    // ===========================
    // SNAPSHOT REDUCIDO
    // ===========================
    private SimSnapshot BuildLightSnapshot(
        SimSnapshot original,
        List<(SimCardState hero, int moveIndex, int targetPosition)> plan)
    {
        var snap = original.Clone();

        foreach (var action in plan)
        {
            var move = action.hero.moves[action.moveIndex];
            snap.MyEnergy -= move.MoveSO.EnergyCost;
        }

        return snap;
    }

    private void Log(string msg)
    {
        if (showDebugLogs)
            Debug.Log(msg);
    }
}
