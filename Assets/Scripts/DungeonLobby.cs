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
            Debug.Log("실행 확인");

            // if (jobList == null || jobList.jobs == null || jobList.jobs.Length == 0) {
            //     Debug.LogError("jobList 또는 jobList.jobs가 null이거나 비어 있습니다!");
            //     Debug.Log(jobList.jobs.Length);
            //     return; // 배열이 없으면 함수 종료
            // }

        //던전 파티 직업 선언
        // Mathf.Min(myjob.Length, jobList.jobs.Length) / myjob.Length
        for(int i=0; i< Mathf.Min(myjob.Length, jobList.jobs.Length); i++){
            myjob[i] = jobList.jobs[i].id_job;
            Debug.Log(myjob[i]);
            // Debug.Log(myjob.Length);
            // Debug.Log(jobList.jobs.Length);
        }
    }

}
