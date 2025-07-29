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
    [SerializeField] private GameObject panelManagement;
    [SerializeField] private GameObject panelSelectDungeon;
    [SerializeField] private GameObject panelDungeonPreparation;
    [Header("Main Lobby")]
    [SerializeField] private GameObject btnFriend;
    [SerializeField] private GameObject btnMailbox;

    [Header("Undo Button")]
    [SerializeField] private GameObject undoBtn;

    [Header("Dungeon Preparation")]
    [SerializeField] private GameObject panelHeroList;
    [SerializeField] private GameObject panelItemList;

    [Header("Scripts")]
    [SerializeField] private HeroListUp heroListUp;
    [SerializeField] private PartySelector partySelector;

    public void OnClickDungeonList()
    {
        panelSelectDungeon.SetActive(true);
        undoBtn.SetActive(true);
        
        panelMenu.SetActive(false);
        panelGoods.SetActive(false);
        btnFriend.SetActive(false);
        btnMailbox.SetActive(false);
    }

    public void OnClickManagement()
    {
        panelManagement.SetActive(true);
        undoBtn.SetActive(true);

        panelMenu.SetActive(false);
        btnFriend.SetActive(false);
        btnMailbox.SetActive(false);
    }

    public void OnclickShowStore()
    {
        panelStore.SetActive(true);
        undoBtn.SetActive(true);

        panelMenu.SetActive(false);
        btnFriend.SetActive(false);
        btnMailbox.SetActive(false);
    }

    public void OnClickUndo()
    {

        if (panelStore.activeSelf)
        {
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            btnFriend.SetActive(true);
            btnMailbox.SetActive(true);

            panelStore.SetActive(false);
            undoBtn.SetActive(false);
        }
        else if (panelManagement.activeSelf)
        {
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            btnFriend.SetActive(true);
            btnMailbox.SetActive(true);

            panelManagement.SetActive(false);
            undoBtn.SetActive(false);
        }
        else if (panelDungeonPreparation.activeSelf)
        {
            heroListUp.ResetButton();

            partySelector.ResetAssignParty();
            panelSelectDungeon.SetActive(true);
            panelHeroList.SetActive(true);

            panelDungeonPreparation.SetActive(false);
            panelItemList.SetActive(false);
        }
        else if (panelSelectDungeon.activeSelf)
        {
            panelMenu.SetActive(true);
            panelGoods.SetActive(true);
            btnFriend.SetActive(true);
            btnMailbox.SetActive(true);

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
