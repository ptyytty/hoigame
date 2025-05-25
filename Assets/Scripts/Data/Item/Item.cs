using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum JobCategory
{
    Warrior,
    Ranged,
    Special,
    Healer
}

[System.Serializable]
public class JobSpritePair
{
    public JobCategory category;
    public Sprite sprite;
}

public enum ItemType
{
    Equipment,
    Consume
}

public enum ItemTarget
{
    Ally,
    Enemy
}

[System.Serializable]
public class ConsumeItem
{
    public int id_item;
    public string name_item;
    public int price;
    public string iconName;
    public Sprite icon => Resources.Load<Sprite>($"Icons/Item/Consume/{iconName}");
    public ItemType itemType;
    public int value;
    public List<ConsumeBuffType> buffTypes;
    public ItemTarget itemTarget;
    public Area area;
    public string description;

    public string ToBuffDescription()
    {
        if (buffTypes == null || buffTypes.Count == 0)
            return "효과 없음";

        return string.Join(", ", buffTypes.Select(bt => bt.ToKorean()));
    }

}

[System.Serializable]
public class EquipItem
{
    public int id_item;
    public string name_item;
    public int price;
    public string iconName;
    public Sprite icon => Resources.Load<Sprite>($"Icons/Item/Equipment/{iconName}");
    public JobCategory jobCategory;
    public ItemType itemType;
    public string effectText;
    public int value;
    public string description;
}