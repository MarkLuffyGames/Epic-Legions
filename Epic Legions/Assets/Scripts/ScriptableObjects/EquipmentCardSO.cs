using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum EquipmentType { Weapon,  }
[CreateAssetMenu(fileName = "New Equipment Card", menuName = "Hemera Legions/Equipment Card")]
public class EquipmentCardSO : CardSO
{
    [SerializeField] private EquipmentType equipmentType;
    [SerializeField] private List<HeroClass> supportedClasses = new List<HeroClass>();
    [SerializeField] private List<MoveSO> moves;
    [SerializeField] private CardEffect effect;

    public EquipmentType EquipmentType => equipmentType;
    public List<HeroClass> SupportedClasses => supportedClasses;
    public List<MoveSO> Moves => moves;
    public CardEffect Effect => effect;
}
