using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Heal", menuName = "Hemera Legions/Card Effects/ Heal")]
public class Heal : CardEffect
{
    [SerializeField] private int amount;
    public override void ActivateEffect(Card caster, Card target)
    {
        Instantiate(visualEffectCardEffect, target.FieldPosition.transform.position, Quaternion.identity);
        target.ToHeal(amount);
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach(Card card in target)
        {
            Instantiate(visualEffectCardEffect, card.FieldPosition.transform.position, Quaternion.identity);
            card.ToHeal(amount);
        }
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
