using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Store : MonoBehaviour
{
    [System.Serializable]
    public class ItemTypeToggleImagePair
    {
        public Toggle toggle;
        public Image image;
        public Sprite selectedSprite;
        public Sprite defaultSprite;
    }

    [System.Serializable]
    public class StoreTypeToggleImagePair
    {
        public Toggle toggle;
        public Image image;
        public Sprite selectedSprite;
        public Sprite defaultSprite;
    }

    public List<ItemTypeToggleImagePair> itemTypeToggleImagePairs;
    public List<StoreTypeToggleImagePair> storeTypeToggleImagePairs;
    private Toggle lastSelectedItemType = null;
    private Toggle lastSelectedStoreType = null;

    void Start()
    {
        foreach (var pair in itemTypeToggleImagePairs)
        {
            pair.toggle.onValueChanged.AddListener((isOn) => OnToggleChanged(pair.toggle, isOn));
            pair.toggle.isOn = false;

            SetToggleDefault();
        }

        foreach (var pair in storeTypeToggleImagePairs)
        {
            pair.toggle.onValueChanged.AddListener((isOn) => OnToggleChanged(pair.toggle, isOn));
            pair.toggle.isOn = false;

            SetToggleDefault();
        }

        foreach (var pair in itemTypeToggleImagePairs)
        {
            if (pair.toggle.isOn)
            {
                lastSelectedItemType = pair.toggle;
                break;
            }
        }

        foreach (var pair in storeTypeToggleImagePairs)
        {
            if (pair.toggle.isOn)
            {
                lastSelectedStoreType = pair.toggle;
                break;
            }
        }

        UpdateItemTypeToggleImages();
    }

    public void SetToggleDefault()
    {
        if (itemTypeToggleImagePairs.Count > 0)
            {
                itemTypeToggleImagePairs[0].toggle.isOn = true;
                lastSelectedItemType = itemTypeToggleImagePairs[0].toggle;
            }
    }


    void OnToggleChanged(Toggle changedToggle, bool isOn)
    {
        if (isOn && changedToggle == lastSelectedItemType)
        {
            changedToggle.isOn = true;
            return;
        }

        if (isOn)
        {
            lastSelectedItemType = changedToggle;
            UpdateItemTypeToggleImages();
        }
    }

    void UpdateItemTypeToggleImages()
    {
        foreach (var pair in itemTypeToggleImagePairs)
        {
            if (pair.toggle.isOn)
            {
                pair.image.sprite = pair.selectedSprite;
            }
            else
            {
                pair.image.sprite = pair.defaultSprite;
            }
        }
    }
}
