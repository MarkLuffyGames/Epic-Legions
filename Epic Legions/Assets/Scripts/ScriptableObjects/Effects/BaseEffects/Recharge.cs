using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Recharge", menuName = "Hemera Legions/Card Effects/ Recharge")]
public class Recharge : CardEffect
{
    [SerializeField] private int amount;
    public override void ActivateEffect(Card caster, Card target)
    {
        target.RechargeEnergy(amount);
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
