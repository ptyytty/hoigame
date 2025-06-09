using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ManageHeroList : MonoBehaviour
{
    [Header("List Up")]
    [SerializeField] private Button heroButtonPrefab;
    [SerializeField] private Transform listPanel;

    void Start()
    {
        GetMyHero();
    }

    void GetMyHero()
    {
        foreach (Job job in HeroManager.instance.GetAllJobs())
        {
            Button heroButton = Instantiate(heroButtonPrefab, listPanel);
            Image buttonImage = heroButton.GetComponent<Image>();
            Image heroImage = heroButton.GetComponentInChildren<Image>();
            TMP_Text heroName = heroButton.transform.Find("Text_Name").GetComponent<TMP_Text>();
            TMP_Text heroJob = heroButton.transform.Find("Text_Job").GetComponent<TMP_Text>();
            TMP_Text heroLevel = heroButton.transform.Find("Text_Level").GetComponent<TMP_Text>();

            heroJob.text = job.name_job;

            Button capturedButton = heroButton;
            Image capturedImage = buttonImage;

            heroButton.onClick.AddListener(() =>
            {

            });
            
        }
    }
}
