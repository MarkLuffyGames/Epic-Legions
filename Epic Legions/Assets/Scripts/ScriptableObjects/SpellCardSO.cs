using UnityEngine;

[CreateAssetMenu(fileName = "New Spell Card", menuName = "Epic Legions/Spell Card")]
public class SpellCardSO : CardSO
{
    [SerializeField][TextArea] private string effectDescription;
    [SerializeField] private MoveSO move;

    public string EffectDescription => effectDescription;
    public MoveSO Move => move;
}
