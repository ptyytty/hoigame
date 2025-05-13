using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;



public class DBManager : MonoBehaviour
{
    //58.230.61.213 <- 공인 IP
    private string url = "http://58.230.61.213:3000/";

    //싱글톤
    public static DBManager instance = null;

    public Job[] jobs {get;private set;}
    public Skill[] skills {get;private set;}

    //직업 종류 찾기
    public JobList jobData;
    public SkillList skillData;


    void Awake()
    {
        if(instance == null){
            instance = this;
        }else{
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(this.gameObject);
        RequestJobStat();
        RequestSkillData();
    }

    public void RequestJobStat(){
         StartCoroutine(GetJobDataFromDatabase());
     }

    public void RequestSkillData()
    {
        StartCoroutine(GetSkillDataFromDatabase());
    }

    //job 테이블 호출 코루틴
     public IEnumerator GetJobDataFromDatabase(){
        UnityWebRequest request = UnityWebRequest.Get(url + "jobs");
        

        // 요청을 보내고 기다리기
        yield return request.SendWebRequest();
        Debug.Log("Start Request");

        // 요청이 성공적으로 완료되었을 경우
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Connect Succes");
            string json = request.downloadHandler.text;

            //json이 전체 배열일 때 변환
            if(json.StartsWith("[")){
                json = "{\"jobs\":" + json + "}";
            }
            try{
                jobData = JsonUtility.FromJson<JobList>(json);
                Debug.Log("첫 번째 직업: " + jobData.jobs[0].name_job);
            }catch{}
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
     }

    // skill 테이블에서 데이터를 가져오는 코루틴
    public IEnumerator GetSkillDataFromDatabase()
    {
        UnityWebRequest request = UnityWebRequest.Get(url + "skills");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            
                if(json.StartsWith("[")){
                    json = "{\"skills\":" + json + "}";
            }
            try{
                skillData = JsonUtility.FromJson<SkillList>(json);
            } catch{}
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }

    public void test(){
        Debug.Log("두 번째 직업: " + jobData.jobs[1].name_job);
    }

}
