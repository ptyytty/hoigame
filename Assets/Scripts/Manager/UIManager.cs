using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class UIinfo
{
    public Button onButton;
    public RectTransform panelPos;
    public float moveOffset;
    public float duration;
    public Vector2 hiddenPos;
    public Vector2 visiblePos;
    public bool isOpen;
}

public class UIManager : MonoBehaviour
{
    [SerializeField] private List<UIinfo> uiList = new List<UIinfo>();

    void Start()
    {
        for (int i = 0; i < uiList.Count; i++)
        {
            int index = i;
            uiList[index].onButton.onClick.AddListener(() =>
            {
                if (uiList[index].isOpen)
                    CloseDrawer(uiList[index]);
                else if (!uiList[index].isOpen)
                    OpenDrawer(uiList[index]);
            });
        }

    }

    void OpenDrawer(UIinfo ui)
    {
        if (ui.isOpen)
            return;
        else if (!ui.isOpen)
        {
            ui.panelPos.DOAnchorPos(ui.visiblePos, ui.duration).SetEase(Ease.OutBack).SetUpdate(true);
            ui.isOpen = true;
        }
    }

    void CloseDrawer(UIinfo ui)
    {
        if (ui.isOpen)
        {
            ui.panelPos.DOAnchorPos(ui.hiddenPos, ui.duration).SetEase(Ease.OutCubic).SetUpdate(true);
            ui.isOpen = false;
        }
        else if (!ui.isOpen)
            return;
        
    }
}
