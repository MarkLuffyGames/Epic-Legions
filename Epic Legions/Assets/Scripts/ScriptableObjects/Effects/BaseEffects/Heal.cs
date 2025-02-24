using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Heal", menuName = "Hemera Legions/Card Effects/ Heal")]
public class Heal : CardEffect
{
    [SerializeField] private int amount;
    public override void ActivateEffect(Card caster, Card target, Movement movement)
    {
        target.ToHeal(amount);
    }

    public override void ActivateEffect(Card caster, List<Card> target, Movement movement)
    {
        foreach(Card card in target)
        {
            card.ToHeal(amount);
        }
    }

    public override void DeactivateEffect(Movement movement)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Movement movement)
    {
        throw new System.NotImplementedException();
    }
}
