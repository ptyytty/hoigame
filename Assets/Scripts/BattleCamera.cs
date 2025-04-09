using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using UnityEngine;

// MainCamera 스크립트
public class BattleCamera : MonoBehaviour
{
    public static BattleCamera instance = null;
    void Awake()
    {
        if(instance == null){
            instance = this;
        }else{
            Destroy(this.gameObject);
        }
    }

    public CinemachineVirtualCamera battleCam;
    public CinemachineVirtualCamera enemyCam;
    public CinemachineVirtualCamera partyCam;

    public void SwitchToEnemy(){
        battleCam.Priority = 0;
        enemyCam.Priority = 10;
        partyCam.Priority = 0;
    }

    public void SwitchToDefault(){
        battleCam.Priority = 10;
        enemyCam.Priority = 0;
        partyCam.Priority = 0;
    }

    public void SwitchToParty(){
        battleCam.Priority = 0;
        enemyCam.Priority = 0;
        partyCam.Priority = 10;
    }
}
