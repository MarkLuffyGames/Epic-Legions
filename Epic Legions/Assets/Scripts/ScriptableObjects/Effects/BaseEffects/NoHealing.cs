using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "No Healing", menuName = "Hemera Legions/Card Effects/ No Healing")]
public class NoHealing : CardEffect
{
    [SerializeField] private int numberTurns = 3;
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
        
    }
}
