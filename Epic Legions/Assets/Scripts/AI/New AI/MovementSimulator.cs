using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovementSimulator
{
    private bool showDebugLogs;

    public MovementSimulator(bool showDebugLogs = false)
    {
        this.showDebugLogs = showDebugLogs;
    }

    public void SimUseMovement(SimSnapshot snap, SimCardState hero, int moveIndex, int targetPosition, FullPlanSim result)
    {
        var move = hero.moves[moveIndex];
        var moveSO = move.MoveSO;

        // Consumir energía
        snap.MyEnergy -= moveSO.EnergyCost;
        result.TotalEnergyCost += moveSO.EnergyCost;

        // Registrar esta acción
        result.AddAction(hero, moveIndex, targetPosition);

        if (showDebugLogs)
        {
            var moveName = moveSO.MoveName;
            string actionInfo = moveSO.NeedTarget ?
                (targetPosition == -1 ? $"[ATAQUE A VIDA]" : $"[OBJETIVO POS{targetPosition}]") : "[AUTO]";
            Debug.Log($"✓ Simulando: {hero.OriginalCard.cardSO.CardName} -> {moveName} {actionInfo}");
        }

        if (targetPosition == -1 && moveSO.MoveType != MoveType.PositiveEffect && !snap.EnemyHeroes.Any(e => e.Alive))
        {
            if(moveSO.Damage == 0)
                Debug.LogWarning("⚠️ Simulación de ataque directo a vida con ataque de 0 daño");
            // ATAQUE DIRECTO A VIDA
            SimDirectLifeAttack(snap, hero, moveIndex, result);
        }
        else
        {
            // ATAQUE A OBJETIVO ESPECÍFICO
            SimCardState target = null;
            if (targetPosition == -1)
                target = hero;
            else
                target = FindCardByPosition(snap, targetPosition, move.MoveSO.MoveType);

            if (target != null && snap.CardStates.TryGetValue(target.OriginalCard, out var targetState) && targetState.Alive)
            {
                SimMovement(snap, hero, moveIndex, target, result);
            }
        }
    }

    private void SimMovement(SimSnapshot snap, SimCardState attackerCard, int movementToUseIndex, SimCardState cardToAttack,FullPlanSim fullPlan)
    {
        if (attackerCard.moves[movementToUseIndex].MoveSO.Damage != 0)
        {
            var moveType = attackerCard.moves[movementToUseIndex].MoveSO.MoveType;
            var targetsType = attackerCard.moves[movementToUseIndex].MoveSO.TargetsType;

            if (targetsType == TargetsType.SINGLE)
            {
                // Aplica el daño a la carta objetivo, considerando efectos especiales como la ignorancia de defensa.
                SimReceiveDamage(snap, attackerCard, cardToAttack, movementToUseIndex, fullPlan);
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log($"🌍 ATAQUE DE ÁREA: {attackerCard.OriginalCard.cardSO.CardName} -> {attackerCard.moves[movementToUseIndex].MoveSO.MoveName}");
                // Si el ataque tiene múltiples objetivos, obtiene todos los objetivos y aplica el daño.
                var targets = GetTargetsForMovement(cardToAttack, attackerCard, movementToUseIndex);
                if (attackerCard.moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
                    targets.Remove(attackerCard); // Asegura que el atacante no se incluya como objetivo.

                foreach (var target in targets)
                {
                    // Aplica el daño a todos los objetivos.
                    SimReceiveDamage(snap, attackerCard, target, movementToUseIndex, fullPlan);
                }
            }

            if (attackerCard.moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
            {
                Counterattack(cardToAttack, attackerCard, snap, fullPlan);
            }
        }

        SimApplyMovementEffects(snap, attackerCard, cardToAttack, movementToUseIndex, fullPlan);
    }

    private List<SimCardState> GetTargetsForMovement(SimCardState cardToAttack, SimCardState attackerCard, int movementToUseIndex)
    {
        var targets = new List<SimCardState>();
        targets.Add(cardToAttack);
        return targets;
    }

    private void Counterattack(SimCardState counterCardState, SimCardState targetCardState, SimSnapshot snap, FullPlanSim fullPlan)
    {
        foreach (var effect in counterCardState.activeEffects)
        {
            if (effect.MoveEffect is Counterattack counterattack)
            {
                if(showDebugLogs)
                    Debug.Log($"   ⚔️ Contraataque de {counterCardState.OriginalCard.cardSO.CardName} a {targetCardState.OriginalCard.cardSO.CardName} por {effect.GetCounterattackDamage()} de daño");
                ApplyDamageToSimCard(snap, targetCardState, effect.GetCounterattackDamage(), 0, fullPlan);
                counterCardState.activeEffects.Remove(effect);
                break;
            }
            else if (effect.MoveEffect is ToxicContact poisonedcounterattack)
            {
                if(showDebugLogs)
                    Debug.Log($"   ☠️ Contacto Tóxico de {counterCardState.OriginalCard.cardSO.CardName} a {targetCardState.OriginalCard.cardSO.CardName}");
                var poison = poisonedcounterattack.PoisonEffect;
                poison.ActivateEffect(counterCardState, targetCardState);
            }
        }
    }

    private SimCardState FindCardByPosition(SimSnapshot snap, int positionIndex, MoveType moveType)
    {
        if(moveType != MoveType.PositiveEffect)
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

    private void SimDirectLifeAttack(SimSnapshot snap, SimCardState attacker, int moveIndex, FullPlanSim result)
    {
        var move = attacker.moves[moveIndex];
        int damage = move.MoveSO.Damage;

        // Aplicar daño directo a la vida del enemigo
        snap.EnemyLife = Mathf.Max(0, snap.EnemyLife - damage);
        result.DirectLifeDamage += damage;

        if (showDebugLogs) Debug.Log($"Ataque directo a vida: {damage} daño");
    }

    private void SimReceiveDamage(SimSnapshot snap, SimCardState attacker, SimCardState target, int moveIndex, FullPlanSim result)
    {
        var move = attacker.moves[moveIndex];

        // Guardar estado antes para logs
        int hpBefore = target.CurrentHP;
        int defBefore = target.CurrentDEF;

        // Calcular daño
        int damage = CalculateSimulatedDamage(attacker, target, move);
        int ignoredDefense = CalculateSimulatedIgnoredDefense(attacker, target, move);

        if (target.HasProtector() != null)
        {
            Debug.Log($"   🛡️ Daño protegido por {target.HasProtector().casterHero.cardSO.CardName}");
            ApplyDamageToSimCard(snap, snap.CardStates.GetValueOrDefault(target.HasProtector().casterHero), damage, 0, result);
            return;
        }

        // Aplicar reflejo de daño completo
        if (target.HasFullDamageReflection())
        {
            Debug.Log($"   🔄 Daño reflejado por {target.OriginalCard.cardSO.CardName}");
            ApplyDamageToSimCard(snap, attacker, damage, 0, result);
        }

        if (target.IsInLethargy() || target.HasPhantomShield() ||
            (move.MoveSO.MoveType == MoveType.RangedAttack && target.HasRangedImmunity()) || (move.MoveSO.MoveType == MoveType.MeleeAttack && target.HasMeleeImmunity()))
        {
            damage = 0;
        }

        // Aplicar daño al objetivo
        int actualDamage = ApplyDamageToSimCard(snap, target, damage, ignoredDefense, result);
        int hpDamage = 0;
        // Logs detallados del daño
        if (showDebugLogs && actualDamage > 0)
        {
            int hpAfter = target.CurrentHP;
            int defAfter = target.CurrentDEF;

            hpDamage = hpBefore - hpAfter;

            Debug.Log($"   💥 {target.OriginalCard.cardSO.CardName}: " +
                     $"-{hpDamage} HP, -{defBefore - defAfter} DEF " +
                     $"(Vida: {hpAfter}/{target.HP}, DEF: {defAfter})");
        }

        // Registrar daño para scoring
        if (!target.ControllerIsMine)
        {
            result.DamageToEnemyHeroes[target] =
                result.DamageToEnemyHeroes.GetValueOrDefault(target) + hpDamage;

            if (!target.Alive)
            {
                result.EnemyHeroesKilled++;
                if(showDebugLogs)
                    Debug.Log($"   💀 ¡{target.OriginalCard.cardSO.CardName} ELIMINADO!");
            }
        }
        else
        {
            result.MyHPLost += actualDamage;
        }

        attacker.lastDamageInflicted = actualDamage;
    }

    private int CalculateSimulatedDamage(SimCardState attacker, SimCardState target, Movement move)
    {
        var moveSO = move.MoveSO;

        // REPLICAR EXACTAMENTE la lógica de DuelManager.CalculateAttackDamage
        if (moveSO.MoveEffect is DestroyDefense)
        {
            return target.CurrentDEF + target.GetDamageAbsorbed();
        }

        int damage = moveSO.Damage;
        damage += attacker.GetEffectiveAttack(); // Bonus de ataque del héroe

        // Efectividad elemental
        damage += CardSO.GetEffectiveness(moveSO.Element, target.OriginalCard.GetElement());

        // Modificadores del movimiento
        if (moveSO.MoveEffect is IncreaseAttackDamage attackModifier)
        {
            // Verificar condición si existe
            if (moveSO.EffectCondition == null || moveSO.EffectCondition.CheckCondition(attacker.OriginalCard, target.OriginalCard))
            {
                damage += attackModifier.Amount;
            }
        }
        else if (moveSO.MoveEffect is MissingHPToAttack missingHPToAttack)
        {
            damage += (attacker.HP - attacker.CurrentHP); // HP faltante
        }

        return Mathf.Max(0, damage);
    }

    private int CalculateSimulatedIgnoredDefense(SimCardState attacker, SimCardState target, Movement move)
    {
        var moveSO = move.MoveSO;

        if (moveSO.MoveEffect is IgnoredDefense ignored)
        {
            if (ignored.Amount == moveSO.Damage)
            {
                // Caso especial: ignorar defensa igual al daño base + modificadores
                int baseDamage = moveSO.Damage + attacker.GetEffectiveAttack();
                baseDamage += CardSO.GetEffectiveness(moveSO.Element, target.OriginalCard.GetElement());
                return baseDamage;
            }
            return ignored.Amount;
        }

        return 0;
    }

    private int ApplyDamageToSimCard(SimSnapshot snap, SimCardState target, int damage, int ignoredDefense, FullPlanSim result)
    {
        if (!snap.CardStates.TryGetValue(target.OriginalCard, out var targetState) || !targetState.Alive)
            return 0;

        if (showDebugLogs)
            Debug.Log($"   🎯 {target.OriginalCard.cardSO.CardName}: " +
                     $"(Vida: {target.CurrentHP}/{target.HP}, DEF: {target.DEF}/{target.CurrentDEF})");

        if (showDebugLogs)
            Debug.Log($"   Calculando daño: {damage} base, {ignoredDefense} defensa ignorada");

        // Aplicar absorción de daño
        damage = Mathf.Max(0, damage - target.GetDamageAbsorbed());

        if (target.GetDamageAbsorbed() > 0 && showDebugLogs)
            Debug.Log($"   Absorción: {target.GetDamageAbsorbed()} daño absorbido");

        // Calcular reducción de defensa
        int effectiveDefense = Mathf.Max(0, target.GetEffectiveDefense());
        int defenseReduction = Mathf.Min(effectiveDefense, damage - ignoredDefense);

        // Reducir defensa
        target.CurrentDEF = Mathf.Max(0, target.CurrentDEF - defenseReduction);

        // Calcular daño a HP
        int remainingDamage = Mathf.Max(0, damage - defenseReduction);
        int hpDamage = Mathf.Min(target.CurrentHP, remainingDamage);
        target.CurrentHP -= hpDamage;

        if (showDebugLogs)
        {
            Debug.Log($"   Defensa efectiva: {effectiveDefense} (ignorada: {ignoredDefense})");
            Debug.Log($"   Reducción: {defenseReduction} DEF, {hpDamage} HP");
        }

        // Verificar muerte
        if (target.CurrentHP <= 0)
        {
            target.Alive = false;
            target.CurrentHP = 0;

            // Actualizar listas
            snap.MyControlledHeroes.Remove(target);
            snap.EnemyHeroes.Remove(target);

            if (showDebugLogs)
                Debug.Log($"   💀 {target.OriginalCard.cardSO.CardName} HA MUERTO");
        }

        return hpDamage + defenseReduction;
    }

    private void SimApplyMovementEffects(SimSnapshot snap, SimCardState attacker, SimCardState target, int moveIndex, FullPlanSim result)
    {
        var move = attacker.moves[moveIndex];
        var moveEffect = move.MoveSO.MoveEffect;

        if (moveEffect == null) return;
        if (!target.EffectIsApplied(move.MoveSO, attacker, target)) 
        {
            if (showDebugLogs)
                Debug.Log($"Efecto {moveEffect.GetType().Name} no aplicable a {target.OriginalCard.cardSO.CardName}");

            return;
        }

        if(showDebugLogs)
            Debug.Log($"Aplicando efecto: {moveEffect.GetType().Name}");

        move.ActivateEffect(attacker, target);
    }
}