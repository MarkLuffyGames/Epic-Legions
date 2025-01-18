using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Increase Attack", menuName = "Epic Legions/Card Effects/ Increase Attack")]
public class IncreaseAttack : CardEffect
{
    public override void ActivateEffect(Card caster, Card target, Movement movement)
    {
        throw new System.NotImplementedException();
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
