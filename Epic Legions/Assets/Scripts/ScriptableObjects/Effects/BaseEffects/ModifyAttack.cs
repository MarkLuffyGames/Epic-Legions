using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Modify Attack", menuName = "Hemera Legions/Card Effects/ Modify Attack")]
public class ModifyAttack : CardEffect
{
    [SerializeField] private int amount;
    [SerializeField] private int numberTurns;
    [SerializeField] private bool isIncrease;

    public int Amount => amount;
    public int NumberTurns => numberTurns;
    public bool IsIncrease => isIncrease;
    public override void ActivateEffect(Card caster, Card target, Movement movement)
    {
        throw new System.NotImplementedException();
    }

    public override void ActivateEffect(Card caster, List<Card> target, Movement movement)
    {
        throw new System.NotImplementedException();
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
