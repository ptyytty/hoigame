using System.Collections.Generic;

public enum ItemClass
{
    Equipment,
    Consume
}

[System.Serializable]
public class Item
{
    public int id_item;
    public string name_item;
    public int value;
    public List<DebuffType> debuffTypes;
    public Target target;
    public Area area;
    public string description;

}