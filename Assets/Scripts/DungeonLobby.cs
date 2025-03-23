using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonLobby : MonoBehaviour
{
    private JobList jobList;

    public int[] myjob = new int[4];


    void Start()
    {
        startDungeon();
    }

    //실행해야 BattleManager 정상 실행
    public void startDungeon(){
        if(DBManager.instance != null)
            jobList = DBManager.instance.jobData;

        for(int i=0; i< myjob.Length; i++){
            myjob[i] = jobList.jobs[i].id_job;
            Debug.Log(myjob[i]);
        }
    }

}
