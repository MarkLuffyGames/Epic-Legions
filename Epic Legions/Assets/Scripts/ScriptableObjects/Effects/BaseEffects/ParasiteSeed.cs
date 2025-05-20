using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Parasite Seed", menuName = "Hemera Legions/Card Effects/ Parasite Seed")]
public class ParasiteSeed : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;
    private Card caster;

    public Card Caster => caster;
    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
    public override void ActivateEffect(Card caster, Card target)
    {
        target.AddEffect(new Effect(this, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        throw new System.NotImplementedException();
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect)
    {
        effect.elapsedTurns++;

        if (effect.elapsedTurns / (float)DuelManager.NumberOfTurns  == 1)
        {
            effect.elapsedTurns = 0;
            effect.DrainHelat();
        }

        effect.durability--;
    }
}
