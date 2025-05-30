using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("Panel")]
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
        panelStore.SetActive(true);
        undoBtn.SetActive(true);
    }

    public void OnClickDungeonList()
    {
        panelSelectDungeon.SetActive(true);
        undoBtn.SetActive(true);
    }

    public void OnClickUndo()
    {
        if (panelStore.activeSelf)
        {
            panelStore.SetActive(false);
            undoBtn.SetActive(false);
        }

        if (panelStore.activeSelf)
            panelStore.SetActive(false);
        else if (panelDungeonPreparation.activeSelf)
        {
            partySelector.ResetAssignParty();
            panelDungeonPreparation.SetActive(false);
            panelHeroList.SetActive(true);
            panelItemList.SetActive(false);
        }
        else if (panelSelectDungeon.activeSelf)
        {
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

    public void OnclickStartDungeon()
    {
        SceneManager.LoadScene("Dungeon_Oratio");
    }
}
