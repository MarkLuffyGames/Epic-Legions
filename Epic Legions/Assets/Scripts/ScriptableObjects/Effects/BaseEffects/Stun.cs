using UnityEngine;

[CreateAssetMenu(fileName = "Stun", menuName = "Epic Legions/Card Effects/ Stun")]
public class Stun : CardEffect
{
    public override void ActivateEffect(Card caster, Card target)
    {
        if (target.CurrentDefensePoints == 0)
        {
            if (target.stunned == 0) target.ToggleStunned();
        }
    }
}
