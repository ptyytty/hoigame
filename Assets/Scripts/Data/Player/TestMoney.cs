using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TestMoney", menuName = "Game/Create Test Money")]
public class TestMoney : ScriptableObject
{
    public int money;
    public int redSoul;
    public int blueSoul;
    public int purpleSoul;
    public int greenSoul;

    public JobCategory warrior = JobCategory.Warrior;
    public JobCategory range = JobCategory.Ranged;
    public JobCategory special = JobCategory.Special;
    public JobCategory healer = JobCategory.Healer;

}
