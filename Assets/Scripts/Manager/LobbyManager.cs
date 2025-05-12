using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public GameObject dungeonListPanel;
    public GameObject dungeonPartyPanel;
    public GameObject dungeonItemPanel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void btnDungeonList(){
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
