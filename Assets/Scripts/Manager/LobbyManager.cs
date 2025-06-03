using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelGoods;
    [SerializeField] private GameObject panelStore;
    [SerializeField] private GameObject panelSelectDungeon;
    [SerializeField] private GameObject panelDungeonPreparation;

    [Header("Undo Button")]
    [SerializeField] private GameObject undoBtn;

    [Header("Dungeon Preparation")]
    [SerializeField] private GameObject panelHeroList;
    [SerializeField] private GameObject panelItemList;
    [SerializeField] private PartySelector partySelector;

    public void OnclickShowStore()
    {
        panelMenu.SetActive(false);
        panelStore.SetActive(true);
        undoBtn.SetActive(true);
    }

    public void OnClickDungeonList()
    {
        panelMenu.SetActive(false);
        panelGoods.SetActive(false);
        panelSelectDungeon.SetActive(true);
        undoBtn.SetActive(true);
    }

    public void OnClickUndo()
    {

        if (panelStore.activeSelf)
        {
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            panelStore.SetActive(false);
            undoBtn.SetActive(false);
        } 
        else if (panelDungeonPreparation.activeSelf)
        {
            partySelector.ResetAssignParty();
            panelDungeonPreparation.SetActive(false);
            panelSelectDungeon.SetActive(true);
            panelHeroList.SetActive(true);
            panelItemList.SetActive(false);
        }
        else if (panelSelectDungeon.activeSelf)
        {
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            panelSelectDungeon.SetActive(false);
            undoBtn.SetActive(false);
        }
    }

    public void OnClickListToggle()
    {
        if (panelHeroList.activeSelf)
        {
            panelHeroList.SetActive(false);
            panelItemList.SetActive(true);
        }
        else
        {
            panelHeroList.SetActive(true);
            panelItemList.SetActive(false);
        }
    }
}
