using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Increase Attack Damage", menuName = "Hemera Legions/Card Effects/ Increase Attack Damage")]
public class IncreaseAttackDamage : CardEffect
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
