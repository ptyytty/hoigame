using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//보유 아이템 확인 및 추가
public class PlayerItemManager : MonoBehaviour
{
    public static PlayerItemManager instance;

    // 게임 내에서 실제 변경되는 아이템 데이터
    public List<OwnedItem<ConsumeItem>> ownedConsumeItem = new();
    public List<OwnedItem<ConsumeItem>> GetOwnedConsumeItems()
    {
        return ownedConsumeItem;
    }
    public List<OwnedItem<EquipItem>> ownedEquipItem = new();

    [Header("테스트 데이터")]
    public TestInventory testInventory;

    // 보유한 아이템 데이터 복사본
    [System.Serializable]
    public class InventoryData
    {
        public List<OwnedItem<ConsumeItem>> consumeItems;
        public List<OwnedItem<EquipItem>> equipItems;
    }

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(this.gameObject);

        DontDestroyOnLoad(this);
        LoadTestInventory();
        //LoadInventory();    // 인벤토리 정보 호출
    }

    // 보유 중인 아이템은 개수 늘리기
    public void AddConsumeItem(ConsumeItem item, int amount = 1)
    {
        var owned = ownedConsumeItem.Find(x => x.itemData.id_item == item.id_item);

        if (owned != null)
        {
            owned.count += amount;
        }
        else
        {
            ownedConsumeItem.Add(new OwnedItem<ConsumeItem>(item, amount));
        }
    }

    public void AddEquipItem(EquipItem item)
    {
        ownedEquipItem.Add(new OwnedItem<EquipItem>(item, 1)); // 장비는 일반적으로 개별 관리
    }

    public int GetConsumeItemCount(ConsumeItem item)
    {
        var owned = ownedConsumeItem.Find(x => x.itemData.id_item == item.id_item);
        return owned != null ? owned.count : 0;
    }

    public void PrintOwnedConsumeItem()
    {
        if (ownedConsumeItem == null || ownedConsumeItem.Count == 0)
        {
            Debug.Log("📦 보유 중인 소비 아이템이 없습니다.");
            return;
        }

        Debug.Log("📋 보유 중인 소비 아이템 목록:");

        foreach (var owned in ownedConsumeItem)
        {
            string name = owned.itemData.name_item;
            int count = owned.count;
            Debug.Log($"🧪 {name} - {count}개");
        }
    }

    public void SaveInventory()
    {
        InventoryData data = new InventoryData
        {
            consumeItems = ownedConsumeItem,
            equipItems = ownedEquipItem
        };

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("inventory", json);
        PlayerPrefs.Save();

        Debug.Log("✅ 인벤토리 저장됨: " + json);
    }

    public void LoadInventory()
    {
        if (PlayerPrefs.HasKey("inventory"))
        {
            string json = PlayerPrefs.GetString("inventory");
            InventoryData data = JsonUtility.FromJson<InventoryData>(json);

            ownedConsumeItem = data.consumeItems;
            ownedEquipItem = data.equipItems;

            Debug.Log("✅ 인벤토리 로드됨");
        }
    }

    // 테스트/초기 아이템 데이터 호출
    void LoadTestInventory()
    {
        ownedConsumeItem = new List<OwnedItem<ConsumeItem>>(testInventory.startingConsumeItems);
        ownedEquipItem = new List<OwnedItem<EquipItem>>(testInventory.startingEquipItems);
    }
}
