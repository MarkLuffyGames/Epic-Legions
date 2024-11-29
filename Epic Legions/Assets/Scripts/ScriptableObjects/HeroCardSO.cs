using UnityEngine;

public enum HeroClass { Warrior, Archer, Wizard, Druid, Necromancer }

[CreateAssetMenu(fileName = "New Hero Card", menuName = "Cards/Hero Card")]
public class HeroCardSO : CardSO
{
    [SerializeField] private HeroClass heroClass;
    [SerializeField] private int attack;
    [SerializeField] private int defence;
    [SerializeField] private int speed;
    [SerializeField] private int energy;

    public HeroClass HeroClass => heroClass;
    public int Attack => attack;
    public int Defence => defence;
    public int Speed => speed;
    public int Energy => energy;


}


