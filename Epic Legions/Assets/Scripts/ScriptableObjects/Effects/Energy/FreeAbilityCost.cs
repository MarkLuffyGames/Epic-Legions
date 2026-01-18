using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Free Ability Cost", menuName = "Hemera Legions/Card Effects/ Free Ability Cost")]
public class FreeAbilityCost : CardEffect
{
    [SerializeField] private int numberTurns;
    public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
    public override void ActivateEffect(Card caster, Card target)
    {
        caster.DuelManager.GetPlayerManagerForCard(caster).AddEffect(new Effect(this, null));
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        caster.snapshot.MyGlobalEffects.Add(new Effect(this, null));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        throw new System.NotImplementedException();
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect, SimCardState simCardState = null)
    {
        effect.durability--;
    }
}
