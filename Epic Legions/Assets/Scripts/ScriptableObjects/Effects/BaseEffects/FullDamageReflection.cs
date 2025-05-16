using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Full Damage Reflection", menuName = "Hemera Legions/Card Effects/ Full Damage Reflection")]
public class FullDamageReflection : CardEffect
{
    [SerializeField] private int numberTurns = 1;
    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
    public override void ActivateEffect(Card caster, Card target)
    {
        target.AddEffect(new Effect(this, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        throw new System.NotImplementedException();
    }

    public override void DeactivateEffect(Effect effect)
    {
        
    }

    public override void UpdateEffect(Effect effect)
    {
        effect.durability--;
    }
}
