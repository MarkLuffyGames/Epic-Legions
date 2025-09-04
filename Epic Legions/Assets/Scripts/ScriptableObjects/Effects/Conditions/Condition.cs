using System;
using UnityEngine;

public abstract class Condition : ScriptableObject
{
    public abstract bool CheckCondition(Card caster, Card target);
}
