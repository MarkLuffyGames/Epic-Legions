using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Destroy Defense", menuName = "Hemera Legions/Card Effects/ Destroy Defense")]
public class DestroyDefense : CardEffect
{
    public override void ActivateEffect(Card caster, Card target)
    {
        
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {

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
