using System.Collections.Generic;
using UnityEngine;


public abstract class CardEffect : ScriptableObject
{

    [SerializeField][TextArea] private string effectDescription;

    public string EffectDescription => effectDescription;
    public abstract void ActivateEffect(Card caster, Card target);
    public abstract void ActivateEffect(Card caster, List<Card> target);
}

[CreateAssetMenu(fileName = "Increase Attack", menuName = "Epic Legions/Card Effects/ Increase Attack")]
public class IncreaseAttackEffect : CardEffect
{
    public override void ActivateEffect(Card caster, Card target)
    {
        throw new System.NotImplementedException();
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        throw new System.NotImplementedException();
    }
}
