using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Modify Attack", menuName = "Hemera Legions/Card Effects/ Modify Attack")]
public class ModifyAttack : CardEffect
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

    public override void UpdateEffect(Effect effect)
    {
        effect.durability--;
    }
}
