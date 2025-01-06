using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Increase Defense", menuName = "Epic Legions/Card Effects/ Increase Defense")]
public class IncreaseDefense : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;

    public override void ActivateEffect(Card caster, Card target)
    {
        target.ApplyDefenseBonus(amount, numberTurns);
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        foreach (Card card in target)
        {
            card.ApplyDefenseBonus(amount, numberTurns);
        }
    }
}
