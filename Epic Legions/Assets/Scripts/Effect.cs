using System;
using UnityEngine;

[Serializable]
public class Effect
{
    public int defense;
    public int currentDefense;
    public int absorbDamage;
    public Card damageReceiver;
    public bool hasProtector;
    public int speed;
    public int attack;

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
    }

}
