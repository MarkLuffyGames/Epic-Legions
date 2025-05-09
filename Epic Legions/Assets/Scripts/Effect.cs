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
    private int defense;
    private int currentDefense;
    private int absorbDamage;
    public Card casterHero;
    private bool hasProtector;
    private int speed;
    private int attack;
    private int ignoredDefense;
    private int damageCounterattack;
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
            defense = modifyDefense.IsIncrease ? modifyDefense.Amount : -modifyDefense.Amount;
            currentDefense = defense;
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
            speed = modifySpeed.IsIncrease ? modifySpeed.Amount : -modifySpeed.Amount;
            durability = modifySpeed.NumberTurns;
            isNegative = !modifySpeed.IsIncrease;
        }
        else if(cardEffect is ModifyAttack modifyAttack)
        {
            attack = modifyAttack.IsIncrease ? modifyAttack.Amount : -modifyAttack.Amount;
            durability = modifyAttack.NumberTurns;
            isNegative = !modifyAttack.IsIncrease;
        }
        else if(cardEffect is IgnoredDefense ignoredDefense)
        {
            this.ignoredDefense = ignoredDefense.Amount;
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
            damageCounterattack = counterattack.Amount;
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
        currentDefense = defense;
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
        if (isActive) return speed;
        return 0;
    }

    public int GetAttack()
    {
        if (isActive) return attack;
        return 0;
    }

    public int GetIgnoredDefense()
    {
        return ignoredDefense;
    }

    public void ApplyPoisonDamage(int damage)
    {
        affectedHero.ApplyPoisonDamage(damage);
    }

    public int GetCounterattackDamage()
    {
        return damageCounterattack;
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
