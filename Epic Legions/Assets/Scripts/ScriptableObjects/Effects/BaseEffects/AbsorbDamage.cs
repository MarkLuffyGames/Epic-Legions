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
        if(visualEffectCardEffect) Instantiate(visualEffectCardEffect, target.FieldPosition.transform.position + Vector3.up * 0.1f, Quaternion.identity);
        target.AddEffect(new Effect(this, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach (Card card in target)
        {
            if (visualEffectCardEffect) Instantiate(visualEffectCardEffect, card.FieldPosition.transform.position, Quaternion.identity);
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
        effect.SetEffectDescription(DescriptionText(effect));
    }

    public string DescriptionText(Effect effect)
    {
        var s = $" for {effect.Durability} turn{(effect.Durability > 1 ? "s" : "")}";
        return "Absorb " + amount + " damage" + (!isPermanent ? s : "");
    }
}
