using System.Collections.Generic;
using UnityEngine;


public abstract class CardEffect : ScriptableObject
{

    public GameObject visualEffectCardEffect;
    public Sprite iconSprite;
    public float effectScore = 1.0f;
    public abstract void ActivateEffect(Card caster, Card target);
    public abstract void ActivateEffect(Card caster, List<Card> target);
    public abstract void UpdateEffect(Effect effect);
    public abstract void DeactivateEffect(Effect effect);
}
