using System;
using UnityEngine;

[Serializable]
public class Effect
{
    public int defense;
    public int currentDefense;
    public int absorbDamage;

    public int durability;
    public Effect (int defense, int absorbDamage, int durability)
    {
        this.defense = defense;
        currentDefense = defense;
        this.absorbDamage = absorbDamage;

        this.durability = durability;
    }


}
