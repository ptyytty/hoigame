using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StoreManager : MonoBehaviour
{
    List<ConsumeItem> allconsumeitem = ItemDatabase.consumeItems;
    List<EquipItem> allequipitem = ItemDatabase.equipItems;
    void Start()
    {
        GetRandomEquipItems();
        GetRandomConsumeItems(2);
    }

    // Update is called once per frame
    void Update()
    {

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

    public List<EquipItem> GetRandomEquipItems()
    {
        List<EquipItem> copy = new List<EquipItem>(ItemDatabase.equipItems);
        List<EquipItem> result = new List<EquipItem>();

        int rand = Random.Range(0, copy.Count);
        result.Add(copy[rand]);
        copy.RemoveAt(rand);


        return result;
    }

    void DisplayRandomItemsInShop()
    {
        List<EquipItem> equipItems = GetRandomEquipItems();
        List<ConsumeItem> consumeItems = GetRandomConsumeItems(2);


        foreach (ConsumeItem item in consumeItems)
        {
            Debug.Log(item.name_item + " - " + item.price + "골드");
            // 여기서 itemUI 오브젝트에 연결 가능
        }
         foreach (EquipItem item in equipItems)
        {
            Debug.Log(item.name_item + " - " + item.price + "골드");
            // 여기서 itemUI 오브젝트에 연결 가능
        }
    }


}
