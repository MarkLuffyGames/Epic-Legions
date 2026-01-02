using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum PassiveSkillActivationPhase
{
    None,
    StartOfTurn
}
public abstract class CardEffect : ScriptableObject
{
    public int Id;
    public GameObject visualEffectCardEffect;
    public Sprite iconSprite;
    public float effectDuration = 1.0f;
    public float effectScore = 1.0f;
    public bool isPermanent;
    public bool isPassive;
    public PassiveSkillActivationPhase passiveSkillActivationPhase;

    public abstract void ActivateEffect(Card caster, Card target);
    public abstract void ActivateEffect(SimCardState caster, SimCardState target);
    public abstract void ActivateEffect(Card caster, List<Card> target);
    public abstract void UpdateEffect(Effect effect, SimCardState simCardState = null);
    public abstract void DeactivateEffect(Effect effect);

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Id == 0)
        {
            var generator = AssetDatabase.LoadAssetAtPath<IDGenerator>("Assets/Scripts/ScriptableObjects/IDGenerator/IDGenerator.asset");
            if (generator != null)
            {
                Id = generator.EffectID();
                EditorUtility.SetDirty(this);
                Debug.Log($"Effect ID set to {Id} for {GetType().Name}");
            }
            else
            {
                Debug.LogWarning("IDGenerator not found. Please create an IDGenerator asset at Assets/Scripts/ScriptableObjects/IdGenerator.asset");
            }
        }
    }
#endif
}
