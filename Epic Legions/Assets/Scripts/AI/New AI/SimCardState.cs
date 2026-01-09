using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental;
using UnityEngine;

[Serializable]
public class SimCardState
{
    public SimSnapshot snapshot;
    public Card OriginalCard;
    public bool OwnerIsMine;
    public bool ControllerIsMine;
    public int HP;
    public int CurrentHP;
    public int DEF;
    public int CurrentDEF;
    public int SPD;
    public int CurrentSPD;
    public int FieldIndex;
    public bool Alive = true;

    public List<Movement> moves = new List<Movement>();
    public Card[] equipmentCard = new Card[3];

    // Efectos activos
    public List<Effect> activeEffects = new List<Effect>();

    public int lastDamageInflicted;

    public SimCardState Clone(SimSnapshot snap)
    {
        return new SimCardState
        {
            OriginalCard = this.OriginalCard,
            OwnerIsMine = this.OwnerIsMine,
            ControllerIsMine = this.ControllerIsMine,
            HP = this.HP,
            CurrentHP = this.CurrentHP,
            DEF = this.DEF,
            CurrentDEF = this.CurrentDEF,
            SPD = this.SPD,
            CurrentSPD = this.CurrentSPD,
            FieldIndex = this.FieldIndex,
            Alive = this.Alive,
            moves = new List<Movement>(this.moves),
            equipmentCard = (Card[])this.equipmentCard,
            activeEffects = new List<Effect>(this.activeEffects),
            snapshot = snap
        };
    }

    public void ManageEffects(int SubTurnsPassedInSimulation)
    {
        for (int i = 0; i < SubTurnsPassedInSimulation; i++)
        {
            foreach (var effect in activeEffects)
            {
                if (effect.durability > 0)
                {
                    effect.MoveEffect.UpdateEffect(effect, this);
                }
            }

            List<Effect> effects = new List<Effect>();
            foreach (var effect in activeEffects)
            {
                if (effect.durability <= 0)
                {
                    effects.Add(effect);
                }
            }

            activeEffects.RemoveAll(stat => stat.durability <= 0);
        }
    }

    public int GetEffectiveAttack()
    {
        int attackModifier = 0;

        if (activeEffects == null) return attackModifier;

        foreach (Effect effect in activeEffects)
        {
            if (effect.MoveEffect is ModifyAttack) attackModifier += effect.GetAttack();
        }

        return attackModifier;
    }

    public int GetEffectiveDefense()
    {
        int defense = CurrentDEF;

        foreach (Effect effect in activeEffects)
        {
            if (effect.MoveEffect is ModifyDefense) defense += effect.GetCurrentDefence();
        }

        return defense;
    }

    public int GetEffectiveSpeed()
    {
        int speed = CurrentSPD;

        foreach (Effect effect in activeEffects)
        {
            if (effect.MoveEffect is ModifySpeed) speed += effect.GetSpeed();
        }

        return speed;
    }

    public bool CanAct()
    {
        return Alive && !IsStunned() && !IsParalyzed() && !IsInLethargy();
    }

    private bool IsStunned()
    {
        return activeEffects.Any(x => x.IsStunned());
    }

    public bool IsInLethargy()
    {
        return activeEffects.Any(x => x.MoveEffect is Lethargy);
    }

    private bool IsParalyzed()
    {
        return activeEffects.Any(x => x.MoveEffect is Paralysis);
    }

    public bool IsBurned()
    {
        return activeEffects.Any(x => x.MoveEffect is Burn);
    }

    public bool HasFullDamageReflection()
    {
        return activeEffects.Any(x => x.MoveEffect is FullDamageReflection);
    }

    public bool HasMeleeImmunity()
    {
        foreach (Effect effect in activeEffects)
        {
            if (effect.MoveEffect is AttackImmunity rangedImmunity)
            {
                if (!effect.isRanged)
                    return true;
            }
        }
        return false;
    }

    public bool HasRangedImmunity()
    {
        foreach (Effect effect in activeEffects)
        {
            if (effect.MoveEffect is AttackImmunity rangedImmunity)
            {
                if (effect.isRanged)
                    return true;
            }
        }
        return false;
    }

    public bool HasPhantomShield()
    {
        foreach (Effect effect in activeEffects)
        {
            if (effect.MoveEffect is PhantomShield phantomShield)
            {
                effect.MoveEffect.DeactivateEffect(effect);
                return true;
            }
        }
        return false;
    }

    public Effect HasProtector()
    {
        foreach (Effect effect in activeEffects)
        {
            if (effect.HasProtector()) return effect;
        }

        return null;
    }

    public void ClearAllEffects()
    {
        activeEffects.RemoveAll(e => e.IsRemovable());
    }

    public void CleanAllNegativeEffects()
    {
        activeEffects.RemoveAll(e => e.IsNegative());
    }

    public bool CanReceiveHealing()
    {
        return !activeEffects.Any(x => x.MoveEffect is NoHealing);
    }

    public int GetDamageAbsorbed()
    {
        int damageAbsorbed = 0;

        foreach (Effect effect in activeEffects)
        {
            damageAbsorbed += effect.GetDamageAbsorbed();
        }

        return damageAbsorbed;
    }

    public Card GetController()
    {
        if (activeEffects == null) return null;

        foreach (Effect effect in activeEffects)
        {
            if (effect.MoveEffect is HeroControl heroControl) return heroControl.Caster;
        }

        return null;
    }

    public bool EffectIsApplied(MoveSO moveSO, SimCardState caster, SimCardState target)
    {
        if (IsInLethargy() || HasPhantomShield()
            || (moveSO.MoveType == MoveType.RangedAttack && HasRangedImmunity())
            || (moveSO.MoveType == MoveType.MeleeAttack && HasMeleeImmunity())
            || HasProtector() != null
            || !(moveSO.EffectCondition != null && moveSO.EffectCondition.CheckCondition(caster, target)))
        {
            return false;
        }

        return true;
    }

    public void ToHeal(int amount)
    {
        if (!CanReceiveHealing()) amount = 0;
        CurrentHP += amount;
        if (CurrentHP > HP) CurrentHP = HP;
    }

    public void AddEffect(Effect effect)
    {
        if (effect.MoveEffect is not Poison || activeEffects.All(x => x.MoveEffect is not Antivenom))
        {
            activeEffects.Add(effect);
        }
    }

    public int GetEnergyBonus()
    {
        int energy = 0;

        if (activeEffects == null) return energy;

        foreach (Effect effect in activeEffects)
        {
            if (effect.MoveEffect is IncreaseEnergy) energy += effect.GetAmount();
        }

        return energy;
    }

    public void RechargeEnergy(int amount)
    {
        var snap = ControllerIsMine ? snapshot : snapshot;
        snap.MyEnergy += amount;
        if (snap.MyEnergy > 100) snap.MyEnergy = 100;
    }

    public void ApplyPoisonDamage(int amount)
    {
        CurrentHP -= amount;
        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            Alive = false;
        }
    }

    public void DrainHelat(int amount, Card casterHero)
    {
        var casterState = snapshot.CardStates.FirstOrDefault(x => x.Key == casterHero).Value;
        if (casterState == null || !casterState.Alive)
        {
            Debug.Log("El héroe que drena vida no está presente o no está vivo en la simulación.");
            return;
        }

        int healthDrained = Math.Min(amount, CurrentHP);
        CurrentHP -= amount;
        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            Alive = false;
        }
         
        
        snapshot.CardStates.FirstOrDefault(x => x.Key == casterHero).Value.ToHeal(healthDrained);
    }
}