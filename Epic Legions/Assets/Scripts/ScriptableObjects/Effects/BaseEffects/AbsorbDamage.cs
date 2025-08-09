using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Absorb Damage", menuName = "Hemera Legions/Card Effects/ Absorb Damage")]
public class AbsorbDamage : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;

    public int Amount => amount;
    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
    public override void ActivateEffect(Card caster, Card target)
    {
        Instantiate(visualEffectCardEffect, target.FieldPosition.transform.position + Vector3.up * 0.1f, Quaternion.identity);
        target.AddEffect(new Effect(this, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach (Card card in target)
        {
            Instantiate(visualEffectCardEffect, card.FieldPosition.transform.position, Quaternion.identity);
            card.AddEffect(new Effect(this, card));
        }
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect)
    {
        if (!isPermanent) effect.durability--;
    }
}
