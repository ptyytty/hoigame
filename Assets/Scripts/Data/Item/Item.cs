using System.Collections.Generic;

public enum JobCategory
{
    Warrior,
    Ranged,
    Special,
    Healer
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
    public ItemType itemType;
    public int value;
    public List<ConsumeBuffType> buffTypes;
    public ItemTarget itemTarget;
    public Area area;
    public string description;
}

[System.Serializable]
public class EquipItem
{
    public int id_item;
    public string name_item;
    public int price;
    public JobCategory jobCategory;
    public ItemType itemType;
    public int value;
    public string description;
}