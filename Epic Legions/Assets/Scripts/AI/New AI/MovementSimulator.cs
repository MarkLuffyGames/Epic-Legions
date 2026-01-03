using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.GraphView;
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

        if (!ValidateMovement(snap, hero, move, targetPosition)) return;

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

        // Procesar el ataque según el objetivo
        if (moveSO.TargetsType != TargetsType.SINGLE && moveSO.TargetsType != TargetsType.DIRECT)
        {
            // ATAQUE DE ÁREA - afecta a varios enemigos
            SimAreaAttack(snap, hero, moveIndex, targetPosition, result);
        }
        else if (targetPosition == -1 && moveSO.MoveType != MoveType.PositiveEffect)
        {
            Debug.LogWarning("⚠️ Simulación de ataque directo a vida con ataque de 0 daño");
            // ATAQUE DIRECTO A VIDA
            SimDirectLifeAttack(snap, hero, moveIndex, result);
        }
        else
        {
            // ATAQUE A OBJETIVO ESPECÍFICO
            var target = FindCardByPosition(snap, targetPosition, move.MoveSO.MoveType);
            if (target != null && snap.CardStates.TryGetValue(target.OriginalCard, out var targetState) && targetState.Alive)
            {
                SimReceiveDamage(snap, hero, target, moveIndex, result);
                SimApplyMovementEffects(snap, hero, target, moveIndex, result);
            }
        }
    }

    private bool ValidateMovement(SimSnapshot snap, SimCardState hero, Movement move, int targetPosition)
    {
        if (!snap.CardStates.TryGetValue(hero.OriginalCard, out var attackerState) || !attackerState.CanAct())
        {
            if (showDebugLogs)
                Debug.Log($"✗ Simulación descartada: {hero.OriginalCard.cardSO.CardName} no puede actuar");
            return false;
        }

        var moveSO = move.MoveSO;

        // Validar energía
        if (moveSO.EnergyCost > snap.MyEnergy)
        {
            if (showDebugLogs)
                Debug.Log($"✗ Simulación descartada: {hero.OriginalCard.cardSO.CardName} energía insuficiente");
            return false;
        }

        // DIFERENCIAR entre movimientos sin objetivo vs ataques a vida directa
        if (targetPosition == -1)
        {
            if (moveSO.NeedTarget)
            {
                // ES ATAQUE DIRECTO A VIDA - validar que no hay héroes enemigos
                bool hayHeroesEnemigosEnCampo = snap.EnemyHeroes.Any(e => e.Alive);
                if (hayHeroesEnemigosEnCampo && moveSO.MoveType != MoveType.PositiveEffect && moveSO.Damage != 0)
                {
                    if (showDebugLogs)
                        Debug.Log($"✗ Simulación descartada: No se puede atacar directo a vida con héroes enemigos en el campo");
                    return false;
                }
                else if (!hayHeroesEnemigosEnCampo && moveSO.MoveType != MoveType.PositiveEffect && moveSO.Damage == 0)
                {
                    if (showDebugLogs)
                        Debug.Log($"✗ Simulación descartada: No se puede aplicar efecto negativo directo a vida");
                    return false;
                }
                // Si es efecto positivo con target -1, es auto-aplicado (válido)
            }
            // Si NO necesita objetivo, es movimiento auto-aplicado (siempre válido)
        }
        else
        {
            // Validar objetivo específico
            var target = FindCardByPosition(snap, targetPosition, move.MoveSO.MoveType);
            if (target == null || !snap.CardStates.TryGetValue(target.OriginalCard, out var targetState) || !targetState.Alive)
            {
                if (showDebugLogs)
                    Debug.Log($"✗ Simulación descartada: Objetivo en posición {targetPosition} no válido");
                return false;
            }

            // Validar línea de visión
            if (!HasLineOfSightToTarget(snap, attackerState, targetState,
                IsHunterRangedAttack(hero, moveSO), IsAssassin(hero)))
            {
                if (showDebugLogs)
                    Debug.Log($"✗ Simulación descartada: Sin línea de visión a posición {targetPosition}");
                return false;
            }
        }

        return true;
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

    private void SimAreaAttack(SimSnapshot snap, SimCardState attacker, int moveIndex,int targetPosition, FullPlanSim result)
    {
        var move = attacker.moves[moveIndex];
        var moveSO = move.MoveSO;

        if (showDebugLogs)
            Debug.Log($"🌍 ATAQUE DE ÁREA: {attacker.OriginalCard.cardSO.CardName} -> {moveSO.MoveName}");

        // Obtener los objetivos del ataque de área
        var targets = snap.EnemyHeroes.Where(e => e.Alive).ToList();

        if (targets.Count > 0)
        {
            // Aplicar daño a todos los enemigos
            foreach (var targetState in targets)
            {
                var targetCard = targetState;
                int damageBefore = targetState.CurrentHP;
                int defenseBefore = targetState.CurrentDEF;

                if(moveSO.Damage > 0)
                    SimReceiveDamage(snap, attacker, targetCard, moveIndex, result);

                // Aplicar efectos del movimiento
                SimApplyMovementEffects(snap, attacker, targetCard, moveIndex, result);
            }
        }
        else
        {
            // No hay enemigos, ataque directo a vida
            if (showDebugLogs)
                Debug.Log($"   🎯 No hay enemigos, atacando vida directa");
            SimDirectLifeAttack(snap, attacker, moveIndex, result);
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
    public bool IsValidSimTarget(SimSnapshot snap, SimCardState attacker, SimCardState target, int moveIndex)
    {
        var move = attacker.moves[moveIndex];
        var moveSO = move.MoveSO;

        if(moveSO.TargetsCondition == null
            || !moveSO.TargetsCondition.CheckCondition(attacker, target)) return false;

        // Verificar que el objetivo esté vivo
        if (!target.Alive) return false;

        // 1. Verificar si el atacante es de clase Hunter y el movimiento es de rango
        bool isHunterRanged = IsHunterRangedAttack(attacker, moveSO);

        // 2. Verificar si el atacante es de clase Assassin (puede atacar por detrás)
        bool isAssassin = IsAssassin(attacker);

        // 3. Verificar línea de visión (si hay héroes delante protegiendo)
        bool hasLineOfSight = HasLineOfSightToTarget(snap, attacker, target, isHunterRanged, isAssassin);

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
        if (!target.EffectIsApplied(move.MoveSO.MoveType)) 
        {
            if (showDebugLogs)
                Debug.Log($"Efecto {moveEffect.GetType().Name} no aplicable a {target.OriginalCard.cardSO.CardName}");

            return;
        }

        if(showDebugLogs)
            Debug.Log($"Aplicando efecto: {moveEffect.GetType().Name}");

        move.ActivateEffect(attacker, target);
    }

    public int EstimateQuickDamage(SimCardState attacker, SimCardState target, int moveIndex)
    {
        var move = attacker.moves[moveIndex];

        if (move.MoveSO.Damage <= 0) return 0;

        // Estimación rápida
        int damage = move.MoveSO.Damage;

        // Bonus de ataque
        damage += attacker.GetEffectiveAttack();

        // Efectividad elemental
        damage += CardSO.GetEffectiveness(move.MoveSO.Element, target.OriginalCard.GetElement());

        // Reducción por defensa (simplificada)
        int effectiveDefense = target.GetEffectiveDefense();

        // Si tiene IgnoredDefense
        if (move.MoveSO.MoveEffect is IgnoredDefense ignored)
        {
            effectiveDefense = Mathf.Max(0, effectiveDefense - ignored.Amount);
        }

        damage = Mathf.Max(0, damage - effectiveDefense / 2);

        return damage;
    }
}