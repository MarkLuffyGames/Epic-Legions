using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Transfer Damage", menuName = "Hemera Legions/Card Effects/ Transfer Damage")]
public class TransferDamage : CardEffect
{

    [SerializeField] private int numberTurns;
    private Card caster;

    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
    public Card Caster => caster;
    public override void ActivateEffect(Card caster, Card target)
    {
        this.caster = caster;
        target.AddEffect(new Effect(this, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach (Card card in target)
        {
            card.AddEffect(new Effect(this, card));
        }
    }

    public override void UpdateEffect(Effect effect)
    {
        effect.durability--;
    }

    public override void DeactivateEffect(Effect effect)
    {
        effect.durability = 0;
    }
}
