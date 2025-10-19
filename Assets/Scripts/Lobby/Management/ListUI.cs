using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// ToggleGroup에 따른 MainTab 변경
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
        [HideInInspector] public bool materialReady;    // 중복 복제 방지
    }

    [SerializeField] private List<ToggleImagepair> listTabToggles;      // 0 = 이름 1 = 직업 2 = 레벨
    [SerializeField] private List<ToggleImagepair> mainTabToggles;

    [SerializeField] private GameObject infoPanel;

    [Header("Script")]
    [SerializeField] private ListUpManager listUpManager;
    [SerializeField] private Employment employment;

    private readonly Color defaultTextColor = new Color(185f / 255f, 185f / 255f, 185f / 255f, 1f);
    private readonly Color selectedTextColor = new Color(1f, 1f, 1f, 1f);
    private readonly Color selectedOutlineColor = new Color(164f / 255f, 109f / 255f, 9f / 255f, 1f);
    private const float selectedOutlineWidth = 0.18f;


    private Toggle currentListTab;
    private Toggle currentMainTab;

    void OnEnable()
    {
        InitExclusiveToggles(listTabToggles, ref currentListTab);
        InitExclusiveToggles(mainTabToggles, ref currentMainTab);

        SetListTabToggles();
        SetMainTabToggles();

        UpdateToggle(listTabToggles);

        NotifySortChangedByCurrentListTab();
    }

    // 토글 초기화
    private void InitExclusiveToggles(List<ToggleImagepair> pairs, ref Toggle currentTab)
    {
        for (int i = 0; i < pairs.Count; i++)
        {
            var t = pairs[i].toggle;
            bool on = (i == 0);
            t.isOn = on;
            if (on) currentTab = t;
        }
    }

    // 리스트 탭 바인딩
    void SetListTabToggles()
    {
        foreach (var pair in listTabToggles)
        {
            PrepareMaterialIfNeeded(pair);

            pair.toggle.onValueChanged.RemoveAllListeners();
            pair.toggle.onValueChanged.AddListener((isOn) =>
            {
                OnToggleChanged(listTabToggles, pair, ref currentListTab);
                if (isOn) NotifySortChanged(pair); // 정렬 기준 변경
            });
        }
        UpdateToggle(listTabToggles);
    }

    // 리스트 탭 바인딩
    void SetMainTabToggles()
    {
        foreach (ToggleImagepair pair in mainTabToggles)
        {
            pair.baseMaterial = new Material(pair.label.fontMaterial);
            pair.label.fontMaterial = pair.baseMaterial;
            pair.toggle.onValueChanged.AddListener((isOn) =>
            {
                OnToggleChanged(mainTabToggles, pair, ref currentMainTab);
                ControlDispayPanel(pair);
            });
        }

        UpdateToggle(mainTabToggles);
    }

    void OnToggleChanged(List<ToggleImagepair> togglepairs, ToggleImagepair pair, ref Toggle currentTab)
    {
        if (currentTab == pair.toggle && pair.toggle.isOn) return;
        currentTab = pair.toggle;
        UpdateToggle(togglepairs);
    }

    // 토글 비주얼 적용
    void UpdateToggle(List<ToggleImagepair> pairs)
    {
        for (int i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            bool isOn = pair.toggle.isOn;

            if (pair.panel) pair.panel.SetActive(isOn);

            if (pair.background) pair.background.SetActive(isOn);
            pair.label.color = isOn ? selectedTextColor : defaultTextColor;

            // TMP 머티리얼 파라미터 적용
            var mat = pair.label.fontMaterial;
            if (isOn)
            {
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, selectedOutlineWidth);
                mat.SetColor(ShaderUtilities.ID_OutlineColor, selectedOutlineColor);
                // Underlay(그림자)
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.5f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 1.5f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -1.5f);
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0.5f));
                pair.label.alpha = 1f;
            }
            else
            {
                mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
                mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
                mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0, 0, 0, 0f));
            }
        }
    }

    // 정렬 토글 전달
    private void NotifySortChanged(ToggleImagepair pair)
    {
        if (listUpManager == null) return;

        int idx = listTabToggles.IndexOf(pair);
        switch (idx)
        {
            case 0: listUpManager.SetSortType(ListUpManager.SortType.Name); break;
            case 1: listUpManager.SetSortType(ListUpManager.SortType.Job); break;
            case 2: listUpManager.SetSortType(ListUpManager.SortType.Level); break;
            default: listUpManager.SetSortType(ListUpManager.SortType.Name); break;
        }
    }

    // 초기화
    private void NotifySortChangedByCurrentListTab()
    {
        for (int i = 0; i < listTabToggles.Count; i++)
        {
            if (listTabToggles[i].toggle.isOn)
            {
                NotifySortChanged(listTabToggles[i]);
                break;
            }
        }
    }

    void ControlDispayPanel(ToggleImagepair toggle)
    {

        if (infoPanel.activeSelf == true) listUpManager.EmployPanelState(false);

        if (toggle == mainTabToggles[0])
        {
            listUpManager.ResetButtonImage();

            listUpManager.PricePanelState(true);
            employment.ResetButtonImage();

            listUpManager.RecoveryPanelState(false);
            listUpManager.ApplyPanelState(false);
        }
        else if (toggle == mainTabToggles[1])
        {
            listUpManager.ResetButtonImage();

            listUpManager.EmployPanelState(false);
            listUpManager.PricePanelState(false);

            listUpManager.RecoveryPanelState(true);
            listUpManager.ApplyPanelState(true);
        }
    }
    
    // 라벨 디자인 머테리얼 복제 적용
    private void PrepareMaterialIfNeeded(ToggleImagepair pair)
    {
        if (pair.materialReady || pair.label == null) return;
        pair.baseMaterial = new Material(pair.label.fontMaterial);
        pair.label.fontMaterial = pair.baseMaterial;
        pair.materialReady = true;
    }
}
