using System;
using UnityEngine;

public abstract class Condition : ScriptableObject
{
    public abstract bool ActivateEffect(Card caster, Card target);
}
