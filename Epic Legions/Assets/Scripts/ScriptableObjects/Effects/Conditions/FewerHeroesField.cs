using UnityEngine;

[CreateAssetMenu(fileName = "Fewer Heroes Field", menuName = "Hemera Legions/Effect Condition/ Fewer Heroes Field")]
public class FewerHeroesField : Condition
{
    public override bool CheckCondition(Card caster, Card target)
    {
        if(caster.DuelManager.GetOpposingPlayerManager(caster.DuelManager.GetPlayerManagerForCard(caster)).GetAllCardVisibleInField().Count > 0)
        {
            return caster.DuelManager.GetPlayerManagerForCard(caster).GetAllCardVisibleInField().Count < target.DuelManager.GetPlayerManagerForCard(target).GetAllCardVisibleInField().Count;
        }
        return false;
    }
}
