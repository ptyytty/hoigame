
    //직업의 속성 정보
[System.Serializable]
public class Job{
    public int id_job;
    public string name_job;
    public int hp;
    public int def;
    public int res;
    public int spd;
    public int hit;
}

[System.Serializable]
public class JobList{
    public Job[] jobs;
}