using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Move", menuName = "Epic Legions/Move")]
public class MoveSO : ScriptableObject
{
    [SerializeField] private string moveName;
    [SerializeField] private int damage;
    [SerializeField] private CardEffect moveEffect;

    public string MoveName => moveName;
    public int Damage => damage;
    public CardEffect MoveEffect => moveEffect;


}

