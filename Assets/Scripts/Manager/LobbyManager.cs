using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public GameObject menuContainer;
    public GameObject dungeonListPanel;
    public GameObject dungeonPartyPanel;
    public GameObject dungeonItemPanel;
    public GameObject undoBtn;

    public void btnShowStore()
    {
        menuContainer.SetActive(true);
    }
    public void OnClickUndo()
    {
        menuContainer.SetActive(false);
    }


    public void btnDungeonList()
    {
        dungeonListPanel.SetActive(true);
        dungeonPartyPanel.SetActive(false);
        dungeonItemPanel.SetActive(false);
    }

    public void btnDungeonParty(){
        dungeonListPanel.SetActive(false);
        dungeonPartyPanel.SetActive(true);
        dungeonItemPanel.SetActive(false);
    }
    public void btnDungeonItem(){
        dungeonListPanel.SetActive(false);
        dungeonPartyPanel.SetActive(false);
        dungeonItemPanel.SetActive(true);
    }

    public void OnclickStartDungeon(){
        SceneManager.LoadScene("Dungeon_Oratio");
    }
}
