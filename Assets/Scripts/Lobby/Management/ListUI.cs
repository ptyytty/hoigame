using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ListUI : MonoBehaviour
{
    [System.Serializable]
    public class ToggleImagepair
    {
        public Toggle toggle;
        public GameObject background;
        public TextMeshProUGUI label;
        public GameObject panel;
        [HideInInspector] public Material baseMaterial;
    }

    [SerializeField] private List<ToggleImagepair> listTabToggles;
    [SerializeField] private List<ToggleImagepair> mainTabToggles;

    private Color defaultTextColor = new Color(185f / 255f, 185f / 255f, 185f / 255f, 1f);
    private Color selectedTextColor = new Color(1f, 1f, 1f, 1f);
    private Color selectedOutlineColor = new Color(164f / 255f, 109f / 255f, 9f / 255f, 1f);
    private float selectedOutlineWidth = 0.18f;


    private Toggle currentListTab;
    private Toggle currentMainTab;

     void OnEnable()
    {
        for (int i = 0; i < listTabToggles.Count; i++)
        {
            int index = i;
            if (index == 0)
            {
                listTabToggles[index].toggle.isOn = true;
                currentListTab = listTabToggles[index].toggle;
            }
            else
            {
                listTabToggles[index].toggle.isOn = false;
            }
        }

        for (int i = 0; i < mainTabToggles.Count; i++)
        {
            int index = i;
            if (index == 0)
            {
                mainTabToggles[index].toggle.isOn = true;
                currentMainTab = mainTabToggles[index].toggle;
            }
            else
            {
                mainTabToggles[index].toggle.isOn = false;
            }
        }

        SetListTabToggles();
        SetMainTabToggles();

        UpdateToggle(listTabToggles);
    }

    void SetListTabToggles()
    {
        foreach (ToggleImagepair pair in listTabToggles)
        {
            pair.baseMaterial = new Material(pair.label.fontMaterial);
            pair.label.fontMaterial = pair.baseMaterial;
            pair.toggle.onValueChanged.AddListener((isOn) =>
            {
                OnToggleChanged(listTabToggles, pair, ref currentListTab);
            });
        }


        UpdateToggle(listTabToggles);
    }

    void SetMainTabToggles()
    {
        foreach (ToggleImagepair pair in mainTabToggles)
        {
            pair.baseMaterial = new Material(pair.label.fontMaterial);
            pair.label.fontMaterial = pair.baseMaterial;
            pair.toggle.onValueChanged.AddListener((isOn) =>
            {
                OnToggleChanged(mainTabToggles, pair, ref currentMainTab);
            });
        }

        UpdateToggle(mainTabToggles);
    }

    void OnToggleChanged(List<ToggleImagepair> togglepairs, ToggleImagepair pair, ref Toggle currentTab)
    {
        if (currentTab == pair.toggle)
            return;

        currentTab = pair.toggle;

        UpdateToggle(togglepairs);
    }

    void UpdateToggle(List<ToggleImagepair> toggleImagepairs)
    {
        foreach (ToggleImagepair pair in toggleImagepairs)
        {
            bool ison = pair.toggle.isOn;

            if (ison)
            {
                if(pair.panel != null) pair.panel.SetActive(true);
                
                pair.background.SetActive(true);
                pair.label.color = selectedTextColor;
                pair.label.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, selectedOutlineWidth);
                pair.label.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, selectedOutlineColor);
                pair.label.alpha = 1f;

                // ✅ 그림자(Underlay) 설정
                pair.label.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.5f);
                pair.label.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 1.5f);
                pair.label.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -1.5f);
                pair.label.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0.5f)); // 약간 반투명한 검정
            }

            if (!ison)
            {
                if(pair.panel != null) pair.panel.SetActive(false);
                
                pair.background.SetActive(false);
                pair.label.color = defaultTextColor;
                pair.label.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);

                // ✅ 그림자 제거
                pair.label.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
                pair.label.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                pair.label.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
                pair.label.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0f));
            }
        }
    }
}
