using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DungeonManager : MonoBehaviour
{
    public static DungeonManager instance {get; private set;}
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public Transform partyTransform;
    public float moveSpeed = 50f;  // 이동 속도
    private bool isMoving = false;
    private bool isInFrontRow = true; // 앞열인지 뒷열인지 구분하는 변수

    public MoveDirection currentDir{get; private set;}


    void Update()
    {
        //  Debug.Log("현재 이동 방향(Dungeon Manager): " + currentDir);    이동 방향 확인
        if (isMoving){
            Vector3 dir = GetMoveVector(currentDir);
            partyTransform.Translate(dir * moveSpeed * Time.deltaTime);
        }
        
    }

    public void StartMove(int dir)
    {

        currentDir = (MoveDirection)dir;  //정수 -> 열거형 캐스팅

        isMoving = true;

        if (currentDir == MoveDirection.Left && !isInFrontRow)
        {
            transform.Rotate(0, -180, 0);
            isInFrontRow = true;
        }
        else if (currentDir == MoveDirection.Right && isInFrontRow)
        {
            transform.Rotate(0, 180, 0);
            isInFrontRow = false;
        }
    }

    public void StopMove()
    {
        isMoving = false;
    }

    Vector3 GetMoveVector(MoveDirection dir)
    {
        if (isInFrontRow)
            return dir == MoveDirection.Left ? Vector3.back : Vector3.forward;
        else
            return dir == MoveDirection.Left ? Vector3.forward : Vector3.back;
    }

}

// 이동 방향 열
public enum MoveDirection
{
    Left = 0,
    Right = 1
}