using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FullPlanSim
{
    public Dictionary<SimCardState, int> DamageToEnemyHeroes = new();
    public int DirectLifeDamage;
    public int MyHPLost;
    public int EnemyHeroesKilled;
    public int MyHeroesLost;
    public int TotalEnergyCost;
    public int FinalEnergy;
    public double Score;
    public int totalActionsExecuted = 0;
    public int invalidActions = 0;

    // Para tracking de acciones específicas
    public List<(SimCardState hero, int moveIndex, int targetPosition)> Actions = new();
    public void CalculateScore(bool showDebugLogs)
    {
        Score = 0;
        double previousScore = 0;

        if (showDebugLogs)
            Debug.Log("=== CÁLCULO DETALLADO DE SCORE ===");

        bool hasInvalidActions = invalidActions > 0;

        if (hasInvalidActions)
        {
            Score = 0;
            if (showDebugLogs)
                Debug.Log($"❌ Score: 0 (Combinación inválida)");
            return;
        }

        // 1. DAÑO DIRECTO A VIDA
        previousScore = Score;
        Score += DirectLifeDamage * 8.0;
        if (showDebugLogs && DirectLifeDamage > 0)
            Debug.Log($"   💖 Daño a vida: {DirectLifeDamage} × 8.0 = +{DirectLifeDamage * 8.0} | Total: {Score}");

        // 2. HÉROES ELIMINADOS
        previousScore = Score;
        Score += EnemyHeroesKilled * 100.0;
        if (showDebugLogs && EnemyHeroesKilled > 0)
            Debug.Log($"   💀 Eliminaciones: {EnemyHeroesKilled} × 100.0 = +{EnemyHeroesKilled * 100.0} | Total: {Score}");

        // 3. DAÑO A HP DE HÉROES
        int totalHPDamage = DamageToEnemyHeroes.Values.Sum();
        previousScore = Score;
        Score += totalHPDamage * 1.5;
        if (showDebugLogs && totalHPDamage > 0)
            Debug.Log($"   ⚔️ Daño a HP: {totalHPDamage} × 1.5 = +{totalHPDamage * 1.5} | Total: {Score}");

        // 4. EFECTOS POSITIVOS APLICADOS
        //previousScore = Score;
        //Score += PositiveEffectsApplied * 15.0;
        //if (showDebugLogs && PositiveEffectsApplied > 0)
        //    Debug.Log($"   ✨ Efectos positivos: {PositiveEffectsApplied} × 15.0 = +{PositiveEffectsApplied * 15.0} | Total: {Score}");

        // 5. HÉROES CONTROLADOS
        //previousScore = Score;
        //Score += HeroesControlled * 50.0;
        //if (showDebugLogs && HeroesControlled > 0)
        //    Debug.Log($"   🎮 Control de héroes: {HeroesControlled} × 50.0 = +{HeroesControlled * 50.0} | Total: {Score}");

        // 6. BALANCE ENERGÉTICO - ENERGÍA RESTANTE
        previousScore = Score;
        double energyBonus = FinalEnergy * 0.1;
        Score += energyBonus;
        if (showDebugLogs && energyBonus > 0)
            Debug.Log($"   🔋 Energía restante: {FinalEnergy} × 0.1 = +{energyBonus:F1} | Total: {Score}");

        // 7. BALANCE ENERGÉTICO - ENERGÍA GASTADA
        previousScore = Score;
        double energyPenalty = TotalEnergyCost * 0.05;
        Score -= energyPenalty;
        if (showDebugLogs && energyPenalty > 0)
            Debug.Log($"   ⚡ Energía gastada: {TotalEnergyCost} × 0.05 = -{energyPenalty:F1} | Total: {Score}");

        // 8. BONUS POR MULTI-KILL
        previousScore = Score;
        if (EnemyHeroesKilled > 1)
        {
            double multiKillBonus = EnemyHeroesKilled * 30.0;
            Score += multiKillBonus;
            if (showDebugLogs)
                Debug.Log($"   🎯 Multi-kill ({EnemyHeroesKilled}): +{multiKillBonus} | Total: {Score}");
        }

        // 9. BONUS POR VICTORIA
        previousScore = Score;
        if (DirectLifeDamage >= 1000)
        {
            Score += 500.0;
            if (showDebugLogs)
                Debug.Log($"   🏆 Bonus victoria: +500.0 | Total: {Score}");
        }

        // SCORE MÍNIMO para combinaciones válidas
        previousScore = Score;
        if (Score < 1.0)
        {
            Score = 1.0;
            if (showDebugLogs)
                Debug.Log($"   📈 Score mínimo aplicado: 1.0 | Total: {Score}");
        }

        if (showDebugLogs)
        {
            Debug.Log($"=================================");
            Debug.Log($"🎯 SCORE FINAL: {Score:F1}");
            Debug.Log($"=================================");
        }
    }

    public void AddAction(SimCardState hero, int moveIndex, int targetPosition)
    {
        Actions.Add((hero, moveIndex, targetPosition));
    }

    public int GetMoveForHero(SimCardState hero)
    {
        var action = Actions.FirstOrDefault(a => a.hero == hero);
        return action.moveIndex;
    }

    public void Merge(FullPlanSim other)
    {
        // Combinar daño a héroes enemigos
        foreach (var kvp in other.DamageToEnemyHeroes)
        {
            DamageToEnemyHeroes[kvp.Key] = DamageToEnemyHeroes.GetValueOrDefault(kvp.Key) + kvp.Value;
        }

        DirectLifeDamage += other.DirectLifeDamage;
        MyHPLost += other.MyHPLost;
        EnemyHeroesKilled += other.EnemyHeroesKilled;
        MyHeroesLost += other.MyHeroesLost;
        TotalEnergyCost += other.TotalEnergyCost;
        FinalEnergy = other.FinalEnergy;

        // Combinar acciones
        Actions.AddRange(other.Actions);
    }

    public FullPlanSim Clone(SimSnapshot snap)
    {
        var clone = new FullPlanSim
        {
            DirectLifeDamage = this.DirectLifeDamage,
            MyHPLost = this.MyHPLost,
            EnemyHeroesKilled = this.EnemyHeroesKilled,
            MyHeroesLost = this.MyHeroesLost,
            TotalEnergyCost = this.TotalEnergyCost,
            FinalEnergy = this.FinalEnergy,
            Score = this.Score
        };

        foreach (var kvp in this.DamageToEnemyHeroes)
        {
            clone.DamageToEnemyHeroes[kvp.Key] = kvp.Value;
        }

        clone.Actions = new List<(SimCardState hero, int moveIndex, int targetPosition)>(this.Actions);

        return clone;
    }
}