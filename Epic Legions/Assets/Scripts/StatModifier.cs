using System;

[Serializable]
public class StatModifier
{
    public int defense;
    public int currentDefense;

    public int durability;
    public StatModifier (int defense, int durability)
    {
        this.defense = defense;
        currentDefense = defense;

        this.durability = durability;
    }
}
