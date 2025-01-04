using System.Collections.Generic;
using UnityEngine;

public enum HeroClass { Warrior, Archer, Wizard, Druid, Necromancer }

[CreateAssetMenu(fileName = "New Hero Card", menuName = "Epic Legions/Hero Card")]
public class HeroCardSO : CardSO
{
    [SerializeField] private HeroClass heroClass;
    [SerializeField] private int healt;
    [SerializeField] private int defence;
    [SerializeField] private int speed;
    [SerializeField] private int energy;
    [SerializeField] private List<MoveSO> moves;

    public HeroClass HeroClass => heroClass;
    public int Healt => healt;
    public int Defence => defence;
    public int Speed => speed;
    public int Energy => energy;
    public List<MoveSO> Moves => moves;

}