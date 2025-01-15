using System.Collections.Generic;
using UnityEngine;


public abstract class CardEffect : ScriptableObject
{
    public abstract void ActivateEffect(Card caster, Card target, Movement movement);
    public abstract void ActivateEffect(Card caster, List<Card> target, Movement movement);
    public abstract StatModifier UpdateEffect(Movement movement);
}

[CreateAssetMenu(fileName = "Increase Attack", menuName = "Epic Legions/Card Effects/ Increase Attack")]
public class IncreaseAttackEffect : CardEffect
{
    public override void ActivateEffect(Card caster, Card target, Movement movement)
    {
        throw new System.NotImplementedException();
    }

    public override void ActivateEffect(Card caster, List<Card> target, Movement movement)
    {
        throw new System.NotImplementedException();
    }

    public override StatModifier UpdateEffect(Movement movement)
    {
        throw new System.NotImplementedException();
    }
}
