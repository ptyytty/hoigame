using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TestInventory", menuName ="Game/Test Inventory")]
public class TestInventory : ScriptableObject
{
    public List<OwnedItem<ConsumeItem>> startingConsumeItems;
    public List<OwnedItem<EquipItem>> startingEquipItems;
}
