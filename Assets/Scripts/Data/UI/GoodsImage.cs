using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

[CreateAssetMenu(fileName = "GoodsImage", menuName = "Game/Goods Image")]
public class GoodsImage : ScriptableObject
{
    public Sprite warriorImage;
    public Sprite rangeImage;
    public Sprite specialImage;
    public Sprite healerImage;
}
