using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Move", menuName = "Hemera Legions/Move")]
public class MoveSO : ScriptableObject
{
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
}

public enum MoveType
{
    MeleeAttack, RangedAttack, PositiveEffect, Ambush
}

public enum TargetsType
{
    SingleTarget, TargetLine, Midfield, Direct, TargetColumn, FrontBack
}

