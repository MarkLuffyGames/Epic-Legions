using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Modify Speed", menuName = "Hemera Legions/Card Effects/ Modify Speed")]
public class ModifySpeed : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;
    [SerializeField] private bool isIncrease;

    public int Amount => amount;
    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
    public bool IsIncrease => isIncrease;
    public override void ActivateEffect(Card caster, Card target)
    {
        target.AddEffect(new Effect(this));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach (Card card in target)
        {
            card.AddEffect(new Effect(this));
        }
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect)
    {
        effect.durability--;

        if (effect.durability <= 0)
        {
            effect = null;
        }
    }
}
