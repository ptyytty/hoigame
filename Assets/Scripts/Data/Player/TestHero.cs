using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="TestHero", menuName ="Game/Create Test Hero")]
public class TestHero : ScriptableObject
{
    public List<Job> jobs;
}
