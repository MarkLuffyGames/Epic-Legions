using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Broken Armor", menuName = "Hemera Legions/Effect Condition/ Broken Armor")]
public class BrokenArmor : Condition
{
    public override bool CheckCondition(Card caster, Card target)
    {
        return target.CurrentDefensePoints <= 0;
    }
}
