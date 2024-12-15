using UnityEngine;

[CreateAssetMenu(fileName = "New Move", menuName = "Epic Legions/Move")]
public class MoveSO : ScriptableObject
{
    [SerializeField] private string moveName;
    [SerializeField][TextArea] private string moveDescription;
    [SerializeField] private int damage;

    public string MoveName => moveName;
    public string MoveDescription => moveDescription;
    public int Damage => damage;
}
