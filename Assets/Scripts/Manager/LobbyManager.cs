using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelStore;
    [SerializeField] private GameObject panelSelectDungeon;
    [SerializeField] private GameObject panelDungeonPreparation;

    [Header("Undo Button")]
    [SerializeField] private GameObject undoBtn;
    

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
            panelDungeonPreparation.SetActive(false);
        }
        else if (panelSelectDungeon.activeSelf)
        {
            panelSelectDungeon.SetActive(false);
            undoBtn.SetActive(false);
        }
    }

    public void OnclickStartDungeon(){
        SceneManager.LoadScene("Dungeon_Oratio");
    }
}
