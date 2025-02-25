using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stun", menuName = "Hemera Legions/Card Effects/ Stun")]
public class Stun : CardEffect
{
    public override void ActivateEffect(Card caster, Card target, Movement movement)
    {
        target.AddEffect(new Effect(this));
    }

    public override void ActivateEffect(Card caster, List<Card> target, Movement movement)
    {
        Debug.LogWarning("Accediendo a un metodo no implemantado");
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
