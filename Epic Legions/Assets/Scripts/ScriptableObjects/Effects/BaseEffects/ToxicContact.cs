using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Toxic Contact", menuName = "Hemera Legions/Card Effects/ Toxic Contact")]
public class ToxicContact : CardEffect
{
    [SerializeField] private int numberTurns;
    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
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
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect)
    {
        effect.durability--;
    }
}
