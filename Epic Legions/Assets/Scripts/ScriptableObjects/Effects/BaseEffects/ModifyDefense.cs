using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Modify Defense", menuName = "Hemera Legions/Card Effects/ Modify Defense")]
public class ModifyDefense : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;
    [SerializeField] private bool isIncrease;

    public int Amount => amount;
    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
    public bool IsIncrease => isIncrease;

    public override void ActivateEffect(Card caster, Card target)
    {
        Instantiate(visualEffectCardEffect, target.FieldPosition.transform.position, Quaternion.identity);
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

    public override void UpdateEffect(Effect effect, SimCardState simCardState)
    {
        if (!isPermanent) effect.durability--;
        effect.SetEffectDescription(DescriptionText(effect));
    }

    public string DescriptionText(Effect effect)
    {
        var s = $" for {effect.Durability} turn{(effect.Durability > 1 ? "s" : "")}";
        return $"{(isIncrease ? "+" : "-")}{amount} defense{(!isPermanent ? s : "")}";
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.AddEffect(new Effect(this, target.OriginalCard));
    }
}
