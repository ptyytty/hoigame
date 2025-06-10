using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroManager : MonoBehaviour
{
    public static HeroManager instance { get; private set; }

    public JobList jobList;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    public List<Job> GetAllJobs()
    {
        if (DBManager.instance != null)
            jobList = DBManager.instance.jobData;

        return new List<Job>(jobList.jobs);
    }

    
}
