using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StoreManager : MonoBehaviour
{
    List<ConsumeItem> showConsumeItem;
    List<EquipItem> showEquipItem;

    public GameObject itemSlotPrefabs;
    public Transform itemGridParent;

    void Start()
    {
        DisplayRandomItemsInShop();
    }

    public List<ConsumeItem> GetRandomConsumeItems(int count)
    {
        List<ConsumeItem> copy = new List<ConsumeItem>(ItemDatabase.consumeItems);
        List<ConsumeItem> result = new List<ConsumeItem>();
        Debug.Log(copy);

        for (int i = 0; i < count && copy.Count > 0; i++)   // copy.Count copy 리스트의 요소 개수
        {
            int rand = Random.Range(0, copy.Count);
            result.Add(copy[rand]);
            copy.RemoveAt(rand);
        }

        return result;
    }

    public List<EquipItem> GetRandomEquipItems(int count = 1)
    {
        List<EquipItem> copy = new List<EquipItem>(ItemDatabase.equipItems);
        List<EquipItem> result = new List<EquipItem>();

        int iterations = Mathf.Min(count, copy.Count);

        for (int i = 0; i < iterations; i++)
        {
            int rand = Random.Range(0, copy.Count);
            result.Add(copy[rand]);
            copy.RemoveAt(rand);
        }


        return result;
    }

    void DisplayRandomItemsInShop()
    {
        showEquipItem = GetRandomEquipItems(1);
        showConsumeItem = GetRandomConsumeItems(2);

        foreach (ConsumeItem item in showConsumeItem)
        {
            GameObject slotGO = Instantiate(itemSlotPrefabs, itemGridParent);
            Product slotUI = slotGO.GetComponent<Product>();
            slotUI.SetConsumeItemData(item);
        }

        foreach (EquipItem item in showEquipItem)
        {
            GameObject slotGO = Instantiate(itemSlotPrefabs, itemGridParent);
            Product slotUI = slotGO.GetComponent<Product>();
            slotUI.SetEquipItemData(item);
        }

    }
}
