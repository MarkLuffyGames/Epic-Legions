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
    private int amount;
    private int currentDefense;
    private int absorbDamage;
    public Card casterHero;
    private bool hasProtector;
    private bool isNegative;
    private bool isRemovable = true;

    public int durability;
    public int elapsedTurns;

    public Effect(CardEffect cardEffect, Card hero)
    {
        moveEffect = cardEffect;
        affectedHero = hero;

        if (cardEffect is AbsorbDamage absorbDamage)
        {
            this.absorbDamage = absorbDamage.Amount;
            durability = absorbDamage.NumberTurns;
        }
        else if(cardEffect is ModifyDefense modifyDefense)
        {
            amount = modifyDefense.IsIncrease ? modifyDefense.Amount : -modifyDefense.Amount;
            currentDefense = amount;
            durability = modifyDefense.NumberTurns;
            isNegative = !modifyDefense.IsIncrease;
        }
        else if(cardEffect is TransferDamage transferDamage)
        {
            casterHero = transferDamage.Caster;
            durability = transferDamage.NumberTurns;
            hasProtector = true;
            isRemovable = false;
        }
        else if(cardEffect is ModifySpeed modifySpeed)
        {
            amount = modifySpeed.IsIncrease ? modifySpeed.Amount : -modifySpeed.Amount;
            durability = modifySpeed.NumberTurns;
            isNegative = !modifySpeed.IsIncrease;
        }
        else if(cardEffect is ModifyAttack modifyAttack)
        {
            amount = modifyAttack.IsIncrease ? modifyAttack.Amount : -modifyAttack.Amount;
            durability = modifyAttack.NumberTurns;
            isNegative = !modifyAttack.IsIncrease;
        }
        else if(cardEffect is IgnoredDefense ignoredDefense)
        {
            amount = ignoredDefense.Amount;
        }
        else if(cardEffect is Stun stun)
        {
            isStunned = true;
            durability = 21;
            isNegative = true;
        }
        else if(cardEffect is Poison poison)
        {
            durability = poison.NumberTurns;
            amount = poison.Amount;
            isNegative = true;
        }
        else if(cardEffect is Antivenom antivenom)
        {
            casterHero = antivenom.Caster;
            durability = 1;
        }
        else if( cardEffect is Counterattack counterattack)
        {
            casterHero = counterattack.Caster;
            amount = counterattack.Amount;
            durability = 1;
            isRemovable = false;
        }
        else if(cardEffect is ToxicContact poisonedcounterattack)
        {
            durability = poisonedcounterattack.NumberTurns;
        }
        else if(cardEffect is Lethargy lethargy)
        {
            durability = lethargy.NumberTurns;
        }
        else if(cardEffect is ParasiteSeed parasiteSeed)
        {
            casterHero = parasiteSeed.Caster;
            durability = parasiteSeed.NumberTurns;
        }
        else if(cardEffect is FullDamageReflection fulldamageReflection)
        {
            durability = fulldamageReflection.NumberTurns;
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

    public bool HasProtector()
    {
        if (isActive) return hasProtector;
        return false;
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

    public void ApplyPoisonDamage()
    {
        affectedHero.ApplyPoisonDamage(amount);
    }

    public void DrainHelat()
    {
        affectedHero.ReceiveDamage(amount, amount, null);
        casterHero.ToHeal(amount);
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
}
