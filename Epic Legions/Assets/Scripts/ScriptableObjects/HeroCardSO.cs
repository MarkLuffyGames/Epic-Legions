using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum HeroClass { Warrior, Paladin, Wizard, Hunter, Assassin, Druid, Beast, Colossus, Necromancer, None }

[CreateAssetMenu(fileName = "New Hero Card", menuName = "Hemera Legions/Hero Card")]
public class HeroCardSO : CardSO
{
    [SerializeField] private HeroClass heroClass;
    [SerializeField] private int health;
    [SerializeField] private int defense;
    [SerializeField] private int speed;
    [SerializeField] private int energy;
    [SerializeField] private List<MoveSO> moves;

    public HeroClass HeroClass => heroClass;
    public int Health => health;
    public int Defense => defense;
    public int Speed => speed;
    public int Energy => energy;
    public List<MoveSO> Moves => moves;

}