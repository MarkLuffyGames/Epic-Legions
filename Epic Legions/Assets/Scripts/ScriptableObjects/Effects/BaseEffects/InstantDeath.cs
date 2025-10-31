using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Instant Death", menuName = "Hemera Legions/Card Effects/ Instant Death")]
public class InstantDeath : CardEffect
{
    public override void ActivateEffect(Card caster, Card target)
    {
        CorrutinaHelper.Instancia.EjecutarCorrutina(Activate(caster, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        throw new System.NotImplementedException();
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    IEnumerator Activate(Card caster, Card target)
    {
        Instantiate(visualEffectCardEffect, target.transform.position + Vector3.up, Quaternion.identity);
        yield return new WaitForSeconds(1.5f);
        target.ReceiveDamage(100, 100, caster, MoveType.PositiveEffect);
    }
}
