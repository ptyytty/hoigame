using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Store : MonoBehaviour
{
    [System.Serializable]
    public class ToggleImagepair
    {
        public Toggle toggle;
        public Image image;
        public Sprite selectedSprite;
        public Sprite defaultSprite;
        public Text labelText;
        public Color selectedTextColor = new Color(238f / 255f, 190f / 255f, 20f / 255f);
        public Color defaultTextcolor = new Color(1, 1, 1);
    }

    public List<ToggleImagepair> itemTypeToggleImagePairs;
    public List<ToggleImagepair> storeTypeToggleImagePairs;
    public GameObject localStore, onlineStore;
    public GameObject itemToggleGroup;
    public GameObject onlineBackground;
    public GameObject onlineToggleGroup, onlineSell, onlineBuy;

    private Toggle lastSelectedItemType = null;
    private Toggle lastSelectedStoreType = null;

    void Start()
    {
        for (int i = 0; i < itemTypeToggleImagePairs.Count; i++)
        {
            int index = i;

            itemTypeToggleImagePairs[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnToggleChanged(itemTypeToggleImagePairs[index].toggle, itemTypeToggleImagePairs, ref lastSelectedItemType);
                }
            });
        }

        for (int i = 0; i < storeTypeToggleImagePairs.Count; i++)
        {
            int index = i;

            storeTypeToggleImagePairs[i].toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnToggleChanged(storeTypeToggleImagePairs[index].toggle, storeTypeToggleImagePairs, ref lastSelectedStoreType);
                    ShowPannelByIndex(index);
                }
            });
        }

        if (itemTypeToggleImagePairs.Count > 0)
        {
            itemTypeToggleImagePairs[0].toggle.isOn = true;
            lastSelectedItemType = itemTypeToggleImagePairs[0].toggle;
        }

        if (storeTypeToggleImagePairs.Count > 0)
        {
            storeTypeToggleImagePairs[0].toggle.isOn = true;
            lastSelectedStoreType = storeTypeToggleImagePairs[0].toggle;
        }

        UpdateToggle(itemTypeToggleImagePairs);
        UpdateToggle(storeTypeToggleImagePairs);
    }

    public void ShowPannelByIndex(int index)
    {
        localStore.SetActive(index == 0);
        if (index == 0)
        {
            itemTypeToggleImagePairs[0].toggle.isOn = true;
            lastSelectedItemType = itemTypeToggleImagePairs[0].toggle;
        }

        onlineToggleGroup.SetActive(index == 1);
        onlineStore.SetActive(index == 1);
        onlineBackground.SetActive(index == 1);
        itemToggleGroup.SetActive(index == 1);
        
    }


    void OnToggleChanged(Toggle changedToggle, List<ToggleImagepair> toggleGroup, ref Toggle lastSelectedToggle)
    {
        if (changedToggle.isOn)
        {
            if (changedToggle != lastSelectedToggle)
            {
                lastSelectedToggle = changedToggle;
                UpdateToggle(toggleGroup);
            }
            else
            {
                changedToggle.isOn = true;
            }
        }
    }

    void UpdateToggle(List<ToggleImagepair> toggleGroup)
    {
        foreach (var pair in toggleGroup)
        {
            bool isOn = pair.toggle.isOn;

            pair.image.sprite = pair.toggle.isOn ? pair.selectedSprite : pair.defaultSprite;

            if (pair.labelText != null)
            {
                pair.labelText.color = isOn ? pair.selectedTextColor : pair.defaultTextcolor;
            }
        }
    }
}
