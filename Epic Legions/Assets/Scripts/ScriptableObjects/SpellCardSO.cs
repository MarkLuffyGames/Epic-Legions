using UnityEngine;

[CreateAssetMenu(fileName = "New Spell Card", menuName = "Hemera Legions/Spell Card")]
public class SpellCardSO : CardSO
{
    [SerializeField] private MoveSO move;

    public MoveSO Move => move;
}
