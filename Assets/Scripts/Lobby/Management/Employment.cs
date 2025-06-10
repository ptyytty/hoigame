using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Employment : MonoBehaviour
{
    [SerializeField] private GameObject heroPrefab;
    [SerializeField] private Transform mainTabGrid;

    List<Job> showHeroList;

    void Start()
    {
    }

    void ShowEmployableHero()
    {
        List<Job> jobs = new List<Job>(HeroManager.instance.GetAllJobs());
        List<Job> result = new List<Job>();

        for (int i = 0; i < jobs.Count; i++)
        {
            int rand = Random.Range(0, jobs.Count);
            result.Add(jobs[rand]);
        }

        showHeroList = result;

        foreach (Job job in showHeroList)
        {
        }
    }
}
