using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stun", menuName = "Epic Legions/Card Effects/ Stun")]
public class Stun : CardEffect
{
    public override void ActivateEffect(Card caster, Card target, Movement movement)
    {
        if (target.CurrentDefensePoints == 0)
        {
            if (target.stunned == 0) target.ToggleStunned();
        }
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
