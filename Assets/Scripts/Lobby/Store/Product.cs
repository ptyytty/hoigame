using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Product : MonoBehaviour
{
    [SerializeField] private TMP_Text productName;
    [SerializeField] private TMP_Text productPrice;
    [SerializeField] private Image productImage;

    public void SetConsumeItemData(ConsumeItem item)
    {
        productName.text = item.name_item;
        productPrice.text = $"{item.price}";
        productImage.sprite = item.icon;
    }

    public void SetEquipItemData(EquipItem item)
    {
        productName.text = item.name_item;
        productPrice.text = $"{item.price}";
        productImage.sprite = item.icon;
    }
}
