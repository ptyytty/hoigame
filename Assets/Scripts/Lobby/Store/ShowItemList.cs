using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 로컬 상점 상품 생성
public class ShowItemList : MonoBehaviour
{
    List<ConsumeItem> showConsumeItem;
    List<EquipItem> showEquipItem;

    public GameObject itemSlotPrefabs;
    public Transform itemGridParent;

    void Start()
    {
        DisplayRandomItemsInShop();
    }

    // 상점 목록
    void DisplayRandomItemsInShop()
    {
        showEquipItem = GetRandomEquipItems(1);
        showConsumeItem = GetRandomConsumeItems(2);

        foreach (EquipItem item in showEquipItem)
        {
            GameObject slotGO = Instantiate(itemSlotPrefabs, itemGridParent);
            Product slotUI = slotGO.GetComponent<Product>();
            slotUI.SetSlotImageByJob(item.jobCategory);
            slotUI.SetEquipItemData(item);
        }

        foreach (ConsumeItem item in showConsumeItem)
        {
            GameObject slotGO = Instantiate(itemSlotPrefabs, itemGridParent);
            Product slotUI = slotGO.GetComponent<Product>();
            slotUI.SetConsumeItemData(item);
        }
    }
    
        // 소모 아이템 2개 랜덤 지정
    public List<ConsumeItem> GetRandomConsumeItems(int count)
    {
        List<ConsumeItem> copy = new List<ConsumeItem>(ItemDatabase.consumeItems);
        List<ConsumeItem> result = new List<ConsumeItem>();

        for (int i = 0; i < count && copy.Count > 0; i++)   // copy.Count copy 리스트의 요소 개수
        {
            int rand = Random.Range(0, copy.Count);
            result.Add(copy[rand]);
            copy.RemoveAt(rand);
        }

        return result;
    }

    // 장비 아이템 1개 랜덤 지정
    public List<EquipItem> GetRandomEquipItems(int count = 1)
    {
        List<EquipItem> copy = new List<EquipItem>(ItemDatabase.equipItems);
        List<EquipItem> result = new List<EquipItem>();

        int iterations = Mathf.Min(count, copy.Count);  // 1과 copy 인덱스 중 작은 값

        for (int i = 0; i < iterations; i++)
        {
            int rand = Random.Range(0, copy.Count);
            result.Add(copy[rand]);
            copy.RemoveAt(rand);
        }

        return result;
    }
}
