using System;
using UnityEngine;

[Serializable]
public class Effect
{
    private bool isActive;
    private bool isStunned;
    private int defense;
    private int currentDefense;
    private int absorbDamage;
    public Card damageReceiver;
    private bool hasProtector;
    private int speed;
    private int attack;
    private int ignoredDefense;

    public int durability;

    public Effect(CardEffect cardEffect)
    {
        if(cardEffect is AbsorbDamage absorbDamage)
        {
            this.absorbDamage = absorbDamage.Amount;
            durability = absorbDamage.NumberTurns;
        }
        else if(cardEffect is ModifyDefense modifyDefense)
        {
            defense = modifyDefense.IsIncrease ? modifyDefense.Amount : -modifyDefense.Amount;
            currentDefense = defense;
            durability = modifyDefense.NumberTurns;
        }
        else if(cardEffect is TransferDamage transferDamage)
        {
            damageReceiver = transferDamage.Caster;
            durability = transferDamage.NumberTurns;
            hasProtector = true;
        }
        else if(cardEffect is ModifySpeed modifySpeed)
        {
            speed = modifySpeed.IsIncrease ? modifySpeed.Amount : -modifySpeed.Amount;
            durability = modifySpeed.NumberTurns;
        }
        else if(cardEffect is ModifyAttack modifyAttack)
        {
            attack = modifyAttack.IsIncrease ? modifyAttack.Amount : -modifyAttack.Amount;
            durability = modifyAttack.NumberTurns;
        }
        else if(cardEffect is IgnoredDefense ignoredDefense)
        {
            this.ignoredDefense = ignoredDefense.Amount;
        }
        else if(cardEffect is Stun stun)
        {
            isStunned = true;
            durability = 1;
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
}
