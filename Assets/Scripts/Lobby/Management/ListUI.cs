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
        [HideInInspector] public Material baseMaterial;
    }

    [SerializeField] private List<ToggleImagepair> listTabToggles;
    //[SerializeField] private List<ToggleImagepair> mainTabToggles;

    private Color defaultTextColor = new Color(185f / 255f, 185f / 255f, 185f / 255f, 1f);
    private Color selectedTextColor = new Color(1f, 1f, 1f, 1f);
    private Color selectedOutlineColor = new Color(164f / 255f, 109f / 255f, 164f / 255f, 1f);
    private float selectedOutlineWidth = 0.4f;


    private Toggle currentSelect;

    void Start()
    {
        SetListTabToggles();
        SetMainTabToggles();
    }

    void SetListTabToggles()
    {
        foreach (ToggleImagepair pair in listTabToggles)
        {
            pair.baseMaterial = new Material(pair.label.fontMaterial);
            pair.label.fontMaterial = pair.baseMaterial;
            pair.toggle.onValueChanged.AddListener((isOn) => OnToggleChanged(listTabToggles, pair, ref currentSelect));
        }


    }

    void SetMainTabToggles()
    {

    }

    void OnToggleChanged(List<ToggleImagepair> togglepairs, ToggleImagepair pair, ref Toggle currentSelect)
    {
        if (currentSelect == pair.toggle)
            return;
        
        pair.background.SetActive(true);
        pair.label.color = selectedTextColor;
        pair.label.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, selectedOutlineWidth);
        pair.label.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, selectedOutlineColor);
        pair.label.alpha = 1f;

        currentSelect = pair.toggle;

        var shadow = pair.label.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 5f);
        shadow.effectDistance = new Vector2(-30f, -20f);

        UpdateToggle(togglepairs);
    }

    void UpdateToggle(List<ToggleImagepair> toggleImagepairs)
    {
        foreach (ToggleImagepair pair in toggleImagepairs)
        {
            bool ison = pair.toggle.isOn;

            if (!ison)
            {
                pair.background.SetActive(false);
                pair.label.color = defaultTextColor;
                pair.label.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
                Destroy(pair.label.GetComponent<Shadow>());
            }
        }
    }
}
