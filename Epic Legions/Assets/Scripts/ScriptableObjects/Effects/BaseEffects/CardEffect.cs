using System.Collections.Generic;
using UnityEngine;


public abstract class CardEffect : ScriptableObject
{
    public abstract void ActivateEffect(Card caster, Card target, Movement movement);
    public abstract void ActivateEffect(Card caster, List<Card> target, Movement movement);
    public abstract void UpdateEffect(Movement movement);
}
