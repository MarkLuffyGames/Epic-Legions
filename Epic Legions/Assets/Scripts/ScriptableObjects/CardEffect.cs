using UnityEngine;


public abstract class CardEffect : ScriptableObject
{

    [SerializeField][TextArea] private string effectDescription;

    public string EffectDescription => effectDescription;
    public abstract void ActivateEffect();
}

[CreateAssetMenu(fileName = "Increase Attack", menuName = "Epic Legions/Card Effects/ Increase Attack")]
public class IncreaseAttackEffect : CardEffect
{
    public override void ActivateEffect()
    {
        throw new System.NotImplementedException();
    }
}
