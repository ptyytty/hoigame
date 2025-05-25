using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private GameObject menuContainer;
    [SerializeField] private GameObject panelSelectDungeon;
    [SerializeField] private GameObject dungeonListPanel;
    [SerializeField] private GameObject undoBtnInStore;
    [SerializeField] private GameObject undoBtnInSelectDungeon;

    public void btnShowStore()
    {
        menuContainer.SetActive(true);
    }
    public void OnClickUndo()
    {
        menuContainer.SetActive(false);
        panelSelectDungeon.SetActive(false);
    }


    public void btnDungeonList()
    {
        dungeonListPanel.SetActive(true);
    }

    public void OnclickStartDungeon(){
        SceneManager.LoadScene("Dungeon_Oratio");
    }
}
