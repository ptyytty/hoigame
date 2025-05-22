using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StoreManager : MonoBehaviour
{

    [System.Serializable]
    public class ToggleImagepair    // 토글 버튼 정보
    {
        public Toggle toggle;
        public Image image;
        public Sprite selectedSprite;
        public Sprite defaultSprite;
        public Text labelText;
        public Color selectedTextColor = new Color(238f / 255f, 190f / 255f, 20f / 255f);
        public Color defaultTextcolor = new Color(1, 1, 1);
    }

    public List<ToggleImagepair> itemTypeToggleImagePairs;  // 아이템 종류 토글
    public List<ToggleImagepair> storeTypeToggleImagePairs; // 상점 토글
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

    // 토글 전환에 따른 패널 변경
    public void ShowPannelByIndex(int index)
    {
        bool islocal = index == 0;
        bool isonline = index == 1;

        // 로컬 상점
        localStore.SetActive(islocal);
        if (islocal) // 로컬 상점 전환 시 전체 토글로 초기화 / 로비 갔다와도 초기화
        {
            itemTypeToggleImagePairs[0].toggle.isOn = true;
            lastSelectedItemType = itemTypeToggleImagePairs[0].toggle;

            storeTypeToggleImagePairs[0].toggle.isOn = true;
            lastSelectedStoreType = storeTypeToggleImagePairs[0].toggle;
        }

        // 온라인 상점
        onlineToggleGroup.SetActive(isonline);
        onlineStore.SetActive(isonline);
        onlineBackground.SetActive(isonline);
        itemToggleGroup.SetActive(isonline);

    }


    // 토글 전환
    void OnToggleChanged(Toggle changedToggle, List<ToggleImagepair> toggleGroup, ref Toggle lastSelectedToggle)    //ref: lastSelectedToggle 참조 호출
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

    // 토글 버튼 이미지 변경
    void UpdateToggle(List<ToggleImagepair> toggleGroup)
    {
        foreach (var pair in toggleGroup)
        {
            bool isOn = pair.toggle.isOn;

            pair.image.sprite = pair.toggle.isOn ? pair.selectedSprite : pair.defaultSprite;

            if (pair.labelText != null) // labelText 여부에 따른 텍스트 색상 변경
            {
                pair.labelText.color = isOn ? pair.selectedTextColor : pair.defaultTextcolor;
            }
        }
    }
}
