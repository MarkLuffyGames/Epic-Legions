using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Poison", menuName = "Hemera Legions/Card Effects/ Poison")]
public class Poison : CardEffect
{
    [SerializeField] private int amount = 10;
    [SerializeField] private int numberTurns = 1;
    public int Amount => amount;
    public int NumberTurns => numberTurns;
    public override void ActivateEffect(Card caster, Card target)
    {
        if (visualEffectCardEffect) Instantiate(visualEffectCardEffect, target.FieldPosition.transform.position + Vector3.up * 0.1f, Quaternion.identity);
        target.AddEffect(new Effect(this, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach (Card card in target)
        {
            if (visualEffectCardEffect) Instantiate(visualEffectCardEffect, card.FieldPosition.transform.position + Vector3.up * 0.1f, Quaternion.identity);
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

        if (effect.elapsedTurns / (float)DuelManager.NumberOfTurns == 1)
        {
            effect.ApplyPoisonDamage();
            effect.elapsedTurns = 0;
        }
    }
    public string DescriptionText()
    {
        return $"Take {amount} poison damage per turn.";
    }
}
