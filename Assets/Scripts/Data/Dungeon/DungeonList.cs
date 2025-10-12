using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 삭제 예정

[CreateAssetMenu(fileName = "NewDungeon", menuName = "MyGame/DungeonData")]
public class Dungeon : ScriptableObject
{
    public string dungeonId;
    public string dungeonName;
    public Sprite thumbnail;
    public List<string> questId;
}

