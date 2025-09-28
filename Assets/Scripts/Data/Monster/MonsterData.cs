using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemies/Monster Data", fileName = "MonsterData")]
public class MonsterData : ScriptableObject
{
    [Header("Identity")]
    public int id;
    public string displayname;
    public Loc loc;

    [Header("Stats")]
    public int hp;
    public int def;
    public int res;
    public int spd;
    public int hit;

    [Header("Skills")]
    public List<Skill> skills = new();
}
