using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Broken Armor", menuName = "Hemera Legions/Effect Condition/ Broken Armor")]
public class BrokenArmor : Condition
{
    public override bool CheckCondition(Card caster, Card target)
    {
        return target.CurrentDefensePoints <= 0;
    }

    public override bool CheckCondition(SimCardState caster, SimCardState target)
    {
        return target.CurrentDEF <= 0;
    }
}
