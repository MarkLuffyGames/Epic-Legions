using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Drain Health", menuName = "Hemera Legions/Card Effects/ Drain Health")]
public class DrainHealth : CardEffect
{
    public override void ActivateEffect(Card caster, Card target)
    {
        caster.ToHeal(caster.lastDamageInflicted);
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        throw new System.NotImplementedException();
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }
}
