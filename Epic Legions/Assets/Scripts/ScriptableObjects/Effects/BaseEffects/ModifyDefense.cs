using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Modify Defense", menuName = "Hemera Legions/Card Effects/ Modify Defense")]
public class ModifyDefense : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;
    [SerializeField] private bool isIncrease;

    public int Amount => amount;
    public int NumberTurns => numberTurns;
    public bool IsIncrease => isIncrease;

    public override void ActivateEffect(Card caster, Card target)
    {
        numberTurns *= 20;
        target.AddEffect(new Effect(this));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        numberTurns *= 20;
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
    }
}
