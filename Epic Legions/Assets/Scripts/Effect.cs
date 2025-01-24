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

    public int durability;

    public Effect(CardEffect cardEffect)
    {
        if(cardEffect is AbsorbDamage absorbDamage)
        {
            this.absorbDamage = absorbDamage.Amount;
            durability = absorbDamage.NumberTurns;
        }
        else if(cardEffect is IncreaseDefense increaseDefense)
        {
            defense = increaseDefense.Amount;
            currentDefense = increaseDefense.Amount;
            durability = increaseDefense.NumberTurns;
        }
        else if(cardEffect is TransferDamage transferDamage)
        {
            damageReceiver = transferDamage.Caster;
            durability = transferDamage.NumberTurns;
            hasProtector = true;
        }
        else if(cardEffect is ReduceSpeed reduceSpeed)
        {
            speed = -reduceSpeed.Amount;
            durability = reduceSpeed.NumberTurns;
        }
    }

}
