using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class ItemCatalog
{
    // item id로 조회
    private static Dictionary<int, ConsumeItem> consumeMap;
    private static Dictionary<int, EquipItem>   equipMap;

    // 맵 만들어져 있는지 확인
    private static bool built;

    // 한 번 호출하여 맵 구성
    private static void BuildOnce()
    {
        if (built) return;  // 존재한다면 return
        consumeMap = ItemDatabase.consumeItems?.ToDictionary(i => i.id_item)    // ?(null-조건연산자) ItemDatabase.consumeItems가 널이 아니면 아이템을 id_item을 키로 하는 사전으로 변환
                     ?? new Dictionary<int, ConsumeItem>();                     // ?? (null-병합) 앞 결과가 널이면 빈 딕셔너리 사용
        equipMap = ItemDatabase.equipItems?.ToDictionary(i => i.id_item)
                     ?? new Dictionary<int, EquipItem>();
        built = true;
    }

    public static ConsumeItem GetConsume(int id)
    {
        BuildOnce();
        consumeMap.TryGetValue(id, out var item);
        return item;
    }

    public static ConsumeItem GetConsumeByName(string name)
    {
        BuildOnce();
        return consumeMap.Values.FirstOrDefault(i => i.name_item == name);
    }

    public static EquipItem GetEquip(int id)
    {
        BuildOnce();
        equipMap.TryGetValue(id, out var item);
        return item;
    }

    public static bool HasItem(int id)
    {
        BuildOnce();
        return consumeMap.ContainsKey(id) || equipMap.ContainsKey(id);
    }
}
