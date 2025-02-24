using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Ignored Defense", menuName = "Hemera Legions/Card Effects/ Ignored Defense")]
public class IgnoredDefense : CardEffect
{
    [SerializeField] private int amount;

    public int Amount => amount;

    public override void ActivateEffect(Card caster, Card target, Movement movement)
    {
        movement.effect = new Effect(this);
    }

    public override void ActivateEffect(Card caster, List<Card> target, Movement movement)
    {
        throw new System.NotImplementedException();
    }

    public override void DeactivateEffect(Movement movement)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Movement movement)
    {
        throw new System.NotImplementedException();
    }
}
