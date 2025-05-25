using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class QuestList : ScriptableObject
{
    public string questId;
    public string questname;
    public bool isBossQuest;
    public List<string> dungeonId;
}
