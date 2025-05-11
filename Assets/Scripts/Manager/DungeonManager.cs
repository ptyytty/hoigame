using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DungeonManager : MonoBehaviour
{
    public Transform partyTransform;
    public float moveSpeed = 50f;  // 이동 속도
    private bool isMoving = false;
    private MoveDirection currentDirection;
    private bool isInFrontRow = true; // 앞열인지 뒷열인지 구분하는 변수



    void Update()
    {
        if (isMoving){
            Vector3 dir = GetMoveVector(currentDirection);
            partyTransform.Translate(dir * moveSpeed * Time.deltaTime);
        }
        
    }

    public void StartMove(int dir)
    {
        currentDirection = (MoveDirection)dir;  //정수 -> 열거형 캐스팅
        isMoving = true;

        if (currentDirection == MoveDirection.Left && !isInFrontRow)
        {
            transform.Rotate(0, -180, 0);
            isInFrontRow = true;
        }
        else if (currentDirection == MoveDirection.Right && isInFrontRow)
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

    public enum MoveDirection
    {
        Left = 0,
        Right = 1
    }

}
