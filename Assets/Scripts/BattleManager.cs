using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using Unity.Jobs;
using UnityEngine;
using DG.Tweening;



public class BattleManager : MonoBehaviour
{
    [SerializeField]
    public TMP_Text[] skill_name = new TMP_Text[3];
    [SerializeField]
    public TMP_Text[] skill_dmg = new TMP_Text[3];
    [SerializeField]
    public TMP_Text[] skill_target = new TMP_Text[3];
    [SerializeField]
    public TMP_Text[] skill_area = new TMP_Text[3];

    private JobList jobList;
    private SkillList skillList;

    private string[] skillname = new string[3];
    private int[] dmg = new int[3];
    public string[] target = new string[3];
    public string[] area = new string[3];
    // private Sprite[] image = new Sprite[3];

    void Start()
    {
        //데이터 호출
        if(DBManager.instance != null)
            skillList = DBManager.instance.skillData;


        for(int i=0; i < Mathf.Min(skillList.skills.Length, 3);i++){
                skillname[i] = skillList.skills[i].name_skill;
                dmg[i] = skillList.skills[i].dmg;
                switch(skillList.skills[i].target){
                    case 0:
                        target[i] = "적";
                        break;
                    case 1:
                        target[i] = "아군";
                        break;
                    case 2:
                        target[i] = "자신";
                        break;
                }
                switch(skillList.skills[i].area){
                    case 0:
                        area[i] = "단일";
                        break;
                    case 1:
                        area[i] = "같은 열";
                        break;
                    case 2:
                        area[i] = "전체";
                        break;
                }

                skill_name[i].text = skillname[i];
                skill_dmg[i].text = "피해: " + dmg[i].ToString();
                skill_target[i].text = "대상: " + target[i];
                skill_area[i].text = "범위: " + area[i];
        }
    }



}
