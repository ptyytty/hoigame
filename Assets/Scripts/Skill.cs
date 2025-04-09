    [System.Serializable]
    public class Skill{
        public int id_skill;
        public string name_skill;
        public int dmg;
        public int target;     //스킬 대상 0: 적     1: 아군     2: 자신
        public int loc;        //사용 위치 0: 상관없음   1: 전열     2: 후열
        public int area;       //사용 범위 0: 단일     1: 같은 열     3: 전체
        public int id_job;
    }

    [System.Serializable]
    public class SkillList{
        public Skill[] skills;
    }