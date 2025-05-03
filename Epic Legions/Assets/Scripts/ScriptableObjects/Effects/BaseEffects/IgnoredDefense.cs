using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Ignored Defense", menuName = "Hemera Legions/Card Effects/ Ignored Defense")]
public class IgnoredDefense : CardEffect
{
    [SerializeField] private int amount;

    public int Amount => amount;

    public override void ActivateEffect(Card caster, Card target)
    {
        
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
