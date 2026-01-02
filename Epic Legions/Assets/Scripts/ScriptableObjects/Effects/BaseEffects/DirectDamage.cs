using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Direct Damage", menuName = "Hemera Legions/Card Effects/ Direct Damage")]
public class DirectDamage : CardEffect
{
    [SerializeField] private int amount;
    public override void ActivateEffect(Card caster, Card target)
    {
        Debug.Log("Activado");
        caster.DuelManager.GetOpposingPlayerManager(caster.DuelManager.GetPlayerManagerForCard(caster)).ReceiveDamage(amount);
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        throw new System.NotImplementedException();
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        Debug.LogWarning("Not Implemented");
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect, SimCardState simCardState)
    {
        throw new System.NotImplementedException();
    }
}
