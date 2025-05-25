using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestDB : MonoBehaviour
{
    public static readonly List<QuestList> questLists = new List<QuestList>
    {
        new QuestList{
            questId = "exploration_of_dungeon_80per",
            questname = "던전의 80% 탐험",
            isBossQuest = false,
            dungeonId = new List<string> {"dungeon_Oratio", "dungeon_Gratia" }
        },

        new QuestList{
            questId = "all_combat_completed",
            questname = "모든 전투 완료",
            isBossQuest = false,
            dungeonId = new List<string> {"dungeon_Oratio", "dungeon_Gratia"}
        }
    };
}
