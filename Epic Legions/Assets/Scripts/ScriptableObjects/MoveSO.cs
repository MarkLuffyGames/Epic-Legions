using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Move", menuName = "Hemera Legions/Move")]
public class MoveSO : ScriptableObject
{
    [SerializeField] private int moveId;
    [SerializeField] private string moveName;
    [SerializeField] private int damage;
    [SerializeField] private int energyCost;
    [SerializeField] private CardElement element;
    [SerializeField] private CardEffect moveEffect;
    [SerializeField] private Condition effectCondition;
    [SerializeField] private Condition targetsCondition;
    [SerializeField] private bool needTarget;
    [SerializeField] private bool onMyself;
    [SerializeField] private MoveType moveType;
    [SerializeField] private TargetsType targetsType;
    [SerializeField] private GameObject visualEffect; 
    [SerializeField] private GameObject visualEffectHit; 
    [SerializeField][TextArea] private string effectDescription;

    public int Id => moveId;
    public string MoveName => moveName;
    public int Damage => damage;
    public int EnergyCost => energyCost;
    public CardElement Element => element;
    public CardEffect MoveEffect => moveEffect;
    public Condition EffectCondition => effectCondition;
    public Condition TargetsCondition => targetsCondition;
    public bool NeedTarget => needTarget;
    public bool OnMyself => onMyself;
    public MoveType MoveType => moveType;
    public TargetsType TargetsType => targetsType;
    public GameObject VisualEffect => visualEffect;
    public GameObject VisualEffectHit => visualEffectHit;
    public string EffectDescription => effectDescription;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (moveId == 0)
        {
            var generator = AssetDatabase.LoadAssetAtPath<IDGenerator>("Assets/Scripts/ScriptableObjects/IDGenerator/IDGenerator.asset");
            if (generator != null)
            {
                moveId = generator.MoveID();
                EditorUtility.SetDirty(this);
                Debug.Log($"Move ID set to {moveId} for {moveName}");
            }
            else
            {
                Debug.LogWarning("IDGenerator not found. Please create an IDGenerator asset at Assets/Scripts/ScriptableObjects/IdGenerator.asset");
            }
        }
    }
#endif
}

public enum MoveType
{
    MeleeAttack, RangedAttack, PositiveEffect
}

public enum TargetsType
{
    SINGLE, ADJACENT, FIELD, DIRECT, COLUMN, PIERCE, CONE, SURROUND
}

