using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventorySlot
{
    public ConsumeItem item;
    public int count;

    public bool IsEmpty => item == null;
    public bool CanAdd(ConsumeItem newItem) =>
        !IsEmpty && item.id_item == newItem.id_item && count < 4;

    public void AddItem(ConsumeItem newItem)
    {
        if (IsEmpty)
        {
            item = newItem;
            count = 1;
        }
        else if (CanAdd(newItem))
        {
            count++;
        }
    }

    public void RemoveOne()
    {
        if (!IsEmpty)
        {
            count--;
            if (count <= 0)
            {
                item = null;
                count = 0;
            }
        }
    }
}