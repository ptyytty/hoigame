using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SortedDropdown : MonoBehaviour
{
    [SerializeField] private Toggle item;
    [SerializeField] private Image backgroundItem;

    private Color selectedColor = new Color(139f / 255f, 149f / 255, 179f / 255);
    private Color defaultColor = new Color(1f, 1f, 1f, 0f);

    void Start()
    {
        item.onValueChanged.AddListener(OnToggleValueChanged);

        OnToggleValueChanged(item.isOn);
    }

    void OnToggleValueChanged(bool isOn)
    {
        backgroundItem.color = isOn ? selectedColor : defaultColor;
    }
}
