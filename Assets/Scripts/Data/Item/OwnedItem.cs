using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 보유 아이템 저장
[System.Serializable]
public class OwnedItem<T>
{
    public T itemData;
    public int count;

    public OwnedItem(T itemData, int count)
    {
        this.itemData = itemData;
        this.count = count;
    }
}
