using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 삭제 예정

public class DungeonListDB : MonoBehaviour
{
    public static readonly List<Dungeon> dungeonLists = new List<Dungeon>
    {
        new Dungeon{
            dungeonId = "dungeon_Oratio",
            dungeonName = "기도관",
            thumbnail = Resources.Load<Sprite>("Icons/Lobby/Gem blue"),
            questId = new List<string> {"exploration_of_dungeon_80per"}
        },

        new Dungeon{
            dungeonId = "dungeon_Gratia",
            dungeonName = "은혜관",
            thumbnail = Resources.Load<Sprite>("Icons/Lobby/Green blue"),
            questId = new List<string> {"exploration_of_dungeon_80per"}
        },

        new Dungeon{
            dungeonId = "",
            dungeonName = "말씀관",
            questId = new List<string> {""}
        }
    };
}
