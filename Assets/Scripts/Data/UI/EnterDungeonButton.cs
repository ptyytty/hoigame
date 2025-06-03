using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

[CreateAssetMenu(fileName = "EnterDungeonImage", menuName = "UI/Enter Dungeon Image")]
public class EnterDungeonButton : ScriptableObject
{
    public Sprite noEntryImage;
    public Sprite entryImage;
}
