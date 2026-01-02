using System;
using UnityEngine;

[Serializable]
public class Effect
{

    private CardEffect moveEffect;
    public CardEffect MoveEffect => moveEffect;

    private Card affectedHero;
    private bool isActive;
    private bool isStunned;
    private bool isBurned;
    private int amount;
    private int currentDefense;
    private int absorbDamage;
    public Card casterHero;
    private bool hasProtector;
    private bool isNegative;
    private bool isRemovable = true;
    public bool isRanged;

    public int durability;
    public int elapsedTurns;

    private string effectDescription;

    public int Durability => durability % DuelManager.NumberOfTurns > 0 ? 
        durability / DuelManager.NumberOfTurns + 1 : durability / DuelManager.NumberOfTurns;

    public Effect(CardEffect cardEffect, Card hero)
    {
        moveEffect = cardEffect;
        affectedHero = hero;

        if (cardEffect is AbsorbDamage absorbDamage)
        {
            this.absorbDamage = absorbDamage.Amount;
            durability = absorbDamage.NumberTurns;
            effectDescription = absorbDamage.DescriptionText(this);
        }
        else if(cardEffect is ModifyDefense modifyDefense)
        {
            amount = modifyDefense.IsIncrease ? modifyDefense.Amount : -modifyDefense.Amount;
            currentDefense = amount;
            durability = modifyDefense.NumberTurns;
            isNegative = !modifyDefense.IsIncrease;
            effectDescription = modifyDefense.DescriptionText(this);
            if (!isNegative) durability++;
        }
        else if(cardEffect is TransferDamage transferDamage)
        {
            casterHero = transferDamage.Caster;
            durability = transferDamage.NumberTurns;
            hasProtector = true;
            isRemovable = false;
            effectDescription = transferDamage.DescriptionText(this);
        }
        else if(cardEffect is ModifySpeed modifySpeed)
        {
            amount = modifySpeed.IsIncrease ? modifySpeed.Amount : -modifySpeed.Amount;
            durability = modifySpeed.NumberTurns;
            isNegative = !modifySpeed.IsIncrease;
            effectDescription = modifySpeed.DescriptionText(this);
            if (!isNegative) durability++;
        }
        else if(cardEffect is ModifyAttack modifyAttack)
        {
            amount = modifyAttack.IsIncrease ? modifyAttack.Amount : -modifyAttack.Amount;
            durability = modifyAttack.NumberTurns;
            isNegative = !modifyAttack.IsIncrease;
            effectDescription = modifyAttack.DescriptionText(this);
            if (!isNegative) durability++;
        }
        else if(cardEffect is Stun stun)
        {
            isStunned = true;
            durability = DuelManager.NumberOfTurns;
            isNegative = true;
            effectDescription = stun.DescriptionText();
        }
        else if(cardEffect is Poison poison)
        {
            durability = poison.NumberTurns;
            amount = poison.Amount;
            isNegative = true;
            effectDescription = poison.DescriptionText();
        }
        else if(cardEffect is Antivenom antivenom)
        {
            casterHero = antivenom.Caster;
            durability = 1;
            effectDescription = antivenom.DescriptionText();
        }
        else if( cardEffect is Counterattack counterattack)
        {
            casterHero = counterattack.Caster;
            amount = counterattack.Amount;
            durability = 1;
            isRemovable = false;
            effectDescription = counterattack.DescriptionText();
        }
        else if(cardEffect is ToxicContact poisonedcounterattack)
        {
            durability = poisonedcounterattack.NumberTurns;
            effectDescription = poisonedcounterattack.DescriptionText(this);
        }
        else if(cardEffect is Lethargy lethargy)
        {
            durability = lethargy.NumberTurns;
            effectDescription = lethargy.DescriptionText(this);
        }
        else if(cardEffect is ParasiteSeed parasiteSeed)
        {
            casterHero = parasiteSeed.Caster;
            durability = parasiteSeed.NumberTurns;
            amount = parasiteSeed.Amount;
            isNegative = true;
            effectDescription = parasiteSeed.DescriptionText(this);
        }
        else if(cardEffect is FullDamageReflection fulldamageReflection)
        {
            durability = fulldamageReflection.NumberTurns;
            effectDescription = fulldamageReflection.DescriptionText(this);
        }
        else if(cardEffect is NoHealing noHealing)
        {
            durability = noHealing.NumberTurns;
            isNegative = true;
            effectDescription = noHealing.DescriptionText(this);
        }
        else if (cardEffect is HeroControl heroControl)
        {
            casterHero = heroControl.Caster;
            durability = 1;
            isNegative = true;
            effectDescription = heroControl.DescriptionText();
        }
        else if (cardEffect is Paralysis paralysis)
        {
            durability = paralysis.NumberTurns;
            isNegative = true;
            effectDescription = paralysis.DescriptionText(this);
        }
        else if (cardEffect is Burn burn)
        {
            isBurned = true;
            durability = 1;
            isNegative = true;
            effectDescription = burn.DescriptionText();
        }
        else if (cardEffect is AttackImmunity rangedImmunity)
        {
            durability = rangedImmunity.NumberTurns;
            isRanged = rangedImmunity.IsRanged;
            effectDescription = rangedImmunity.DescriptionText(this);
        }
        else if(cardEffect is IncreaseEnergy increaseEnergy)
        {
            amount = increaseEnergy.Amount;
            durability = increaseEnergy.NumberTurns;
        }

        if (!isNegative)
        {
            ActivateEffect();
        }

    }

    public void ActivateEffect()
    {
        isActive = true;
    }
    public bool IsStunned()
    {
        if (isActive) return isStunned;
        return false;
    }

    public bool IsBurned()
    {
        if (isActive) return isBurned;
        return false;
    }
    public void SetCurrentDefence(int newCurrentDefence)
    {
        currentDefense = newCurrentDefence;
    }
    public int GetCurrentDefence()
    {
        if (isActive) return currentDefense;
        return 0;
    }
    public void RegenerateDefense()
    {
        currentDefense = amount;
    }

    public int GetDamageAbsorbed()
    {
        if (isActive) return absorbDamage;
        return 0;
    }

    public int GetAmount()
    {
        return amount;
    }

    public bool HasProtector()
    {
        return hasProtector;
    }

    public int GetSpeed()
    {
        if (isActive) return amount;
        return 0;
    }

    public int GetAttack()
    {
        if (isActive) return amount;
        return 0;
    }

    public int GetIgnoredDefense()
    {
        return amount;
    }

    public void ApplyPoisonDamage(SimCardState simCardState = null)
    {
        if (simCardState != null)
        {
            simCardState.ApplyPoisonDamage(amount);
            return;
        }
        
        affectedHero.ApplyPoisonDamage(amount);
        
    }

    public void DrainHelat(SimCardState simCardState)
    {
        if(simCardState != null)
        {
            simCardState.DrainHelat(amount, casterHero);
            return;
        }
        
        casterHero.ToHeal(affectedHero.ReceiveDamage(amount, amount, null, MoveType.PositiveEffect));
    }

    public void CancelStun()
    {
        if(moveEffect is Stun)
        {
            moveEffect.DeactivateEffect(this);
        }
    }

    public int GetCounterattackDamage()
    {
        return amount;
    }

    public bool IsNegative()
    {
        return isNegative;
    }

    public bool IsRemovable()
    {
        return isRemovable;
    }

    public void SetEffectDescription(string description)
    {
        effectDescription = description;
    }

    public string GetEffectDescription()
    {
        return effectDescription;
    }
}
