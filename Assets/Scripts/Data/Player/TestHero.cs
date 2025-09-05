using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="TestHero", menuName ="Game/Create Test Hero")]
public class TestHero : ScriptableObject        // 프로토타입
{
    public List<Job> jobs;
}
