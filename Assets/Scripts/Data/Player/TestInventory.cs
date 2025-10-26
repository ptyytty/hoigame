using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TestInventory", menuName ="Game/Test Inventory")]
public class TestInventory : ScriptableObject
{
    public List<OwnedItem<ConsumeItem>> startingConsumeItems;
    public List<OwnedItem<EquipItem>> startingEquipItems;

    [Header("Starting Currencies")]
    public int startGold = 5000;
    public int startRedSoul = 10;
    public int startBlueSoul = 10;
    public int startGreenSoul = 10;
}
