using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Poison", menuName = "Hemera Legions/Card Effects/ Poison")]
public class Poison : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private float numberTurns = Mathf.Infinity;
    public int Amount => amount;
    public int NumberTurns => (int)numberTurns;
    public override void ActivateEffect(Card caster, Card target)
    {
        target.AddEffect(new Effect(this, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach (Card card in target)
        {
            card.AddEffect(new Effect(this, card));
        }
    }

    public override void DeactivateEffect(Effect effect)
    {
        effect.durability = 0;
    }

    public override void UpdateEffect(Effect effect)
    {
        effect.elapsedTurns++;

        if (DuelManager.NumberOfTurns % effect.elapsedTurns == 0)
            effect.ApplyPoisonDamage(amount);
    }
}
