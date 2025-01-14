using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Move", menuName = "Epic Legions/Move")]
public class MoveSO : ScriptableObject
{
    [SerializeField] private string moveName;
    [SerializeField] private int damage;
    [SerializeField] private CardEffect moveEffect;
    [SerializeField] private bool needTarget;
    [SerializeField] private MoveType moveType;
    [SerializeField] private TargetsType targetsType;
    [SerializeField] private GameObject visualEffect; 
    [SerializeField][TextArea] private string effectDescription;

    public string MoveName => moveName;
    public int Damage => damage;
    public CardEffect MoveEffect => moveEffect;
    public bool NeedTarget => needTarget;
    public MoveType MoveType => moveType;
    public TargetsType TargetsType => targetsType;
    public GameObject VisualEffect => visualEffect;
    public string EffectDescription => effectDescription;
}

public enum MoveType
{
    MeleeAttack, RangedAttack, PositiveEffect, AdverseEffect
}

public enum TargetsType
{
    SingleTarget, TargetLine
}

