using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DungeonSelector : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject dungeonSelectPanel;
    [SerializeField] private GameObject dungeonPreparationPanel;
    [Header("UI")]
    [SerializeField] private TMP_Text dungeonNameText;
    [SerializeField] private TMP_Text dungeonQuestText;
    [SerializeField] private Image dungeonImage;
    [Header("Buttons")]
    [SerializeField] private Button dungeonSelect;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    [Header("Dungeon Data")]
    [SerializeField] private List<Dungeon> dungeons;

    private int currentIndex = 0;
    private GameObject currentPanel;

    void Start()
    {
        if (dungeons == null || dungeons.Count == 0)
            return;

        dungeonSelect.onClick.AddListener(SelectDungeon);
        previousButton.onClick.AddListener(MoveLeft);
        nextButton.onClick.AddListener(MoveRight);
        currentPanel = dungeonSelectPanel;

        if (currentIndex == 0)
            previousButton.gameObject.SetActive(false);

        UpdateUI();
    }

    void MoveLeft()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateUI();

        }
    }

    void MoveRight()
    {
        if (currentIndex < dungeons.Count - 1)
        {
            currentIndex++;
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        Dungeon currentDungeon = dungeons[currentIndex];
        dungeonNameText.text = currentDungeon.dungeonName;
        dungeonImage.sprite = currentDungeon.thumbnail;
        if (currentIndex == 0)
        {
            previousButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(true);
        }
        else if (currentIndex == dungeons.Count - 1)
        {
            previousButton.gameObject.SetActive(true);
            nextButton.gameObject.SetActive(false);
        }
        else
        {
            previousButton.gameObject.SetActive(true);
            nextButton.gameObject.SetActive(true);
        }
    }

    void SelectDungeon()
    {
        dungeonPreparationPanel.SetActive(true);
        currentPanel = dungeonPreparationPanel;
    }

    public void ResetSelectDungeon()
    {
        if (currentPanel == dungeonPreparationPanel)
        {
            dungeonPreparationPanel.SetActive(false);
            dungeonSelectPanel.SetActive(true);
            currentPanel = dungeonSelectPanel;
        }
        
        currentIndex = 0;
        UpdateUI();
        
    }
}
