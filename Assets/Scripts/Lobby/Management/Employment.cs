using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Employment : MonoBehaviour
{
    [SerializeField] private GameObject heroButtonPrefab;
    [SerializeField] private Transform gridMainTab;

    [Header("Hero Info Panel")]
    [SerializeField] private TMP_Text heroName;
    [SerializeField] private TMP_Text heroHp;
    [SerializeField] private TMP_Text heroDef;
    [SerializeField] private TMP_Text heroRes;
    [SerializeField] private TMP_Text heroSpd;
    [SerializeField] private TMP_Text heroHit;

    private List<Job> randomHeros;

    void Start()
    {
        DisplayRandomHero();
    }

    List<Job> ShowEmployableHero(int level) // 매개 변수 = 슬롯 확장 단계계
    {
        List<Job> copy = HeroManager.instance.GetAllJobs();
        List<Job> result = new List<Job>();

        for (int i = 0; i < copy.Count && i < level; i++)
        {
            int rand = Random.Range(0, copy.Count);
            result.Add(copy[rand]);
        }

        return result;
    }

    void DisplayRandomHero()
    {
        randomHeros = ShowEmployableHero(2);

        foreach (Job job in randomHeros)
        {
            GameObject slot = Instantiate(heroButtonPrefab, gridMainTab);
            Button heroButton = slot.GetComponent<Button>();

            heroButton.onClick.AddListener(() =>
            {

            });
        }
    }
}
