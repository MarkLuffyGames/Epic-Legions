using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sacrifice For Attack", menuName = "Hemera Legions/Card Effects/ Sacrifice For Attack")]
public class SacrificeForAttack : CardEffect
{
    [SerializeField] CardEffect modifyAttack;
    public override void ActivateEffect(Card caster, Card target)
    {
        CorrutinaHelper.Instancia.EjecutarCorrutina(Activate(caster, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        throw new System.NotImplementedException();
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.CurrentHP = 0;
        foreach (var hero in target.snapshot.MyControlledHeroes)
        {
            modifyAttack.ActivateEffect(caster, hero);
        }
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect, SimCardState simCardState)
    {
        throw new System.NotImplementedException();
    }

    IEnumerator Activate(Card caster, Card target)
    {
        target.ReceiveDamage(100, 100, caster, MoveType.PositiveEffect);
        var targetManager = target.DuelManager.GetPlayerManagerForCard(target);
        yield return new WaitForSeconds(2f);
        modifyAttack.ActivateEffect(caster, targetManager.GetAllCardInField());
    }
}
