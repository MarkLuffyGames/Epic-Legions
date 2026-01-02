using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Missing HP To Attack", menuName = "Hemera Legions/Card Effects/ Missing HP To Attack")]
public class MissingHPToAttack : CardEffect
{
    public override void ActivateEffect(Card caster, Card target)
    {
        
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        throw new System.NotImplementedException();
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect, SimCardState simCardState)
    {
        throw new System.NotImplementedException();
    }
}
