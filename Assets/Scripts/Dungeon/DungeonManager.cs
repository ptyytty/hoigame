using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
public float moveSpeed = 10f;  // 이동 속도
    private bool isLeftMoving = false; // 이동 여부
    private bool isRightMoving = false;
    private bool isInFrontRow = true; // 앞열인지 뒷열인지 구분하는 변수

    void Update()
    {
        if(isInFrontRow){
            if (isLeftMoving){
                transform.Translate(new Vector3(0, 0, -1) * moveSpeed * Time.deltaTime);
            }else if(isRightMoving){
                transform.Translate(new Vector3(0, 0, 1) * moveSpeed * Time.deltaTime);
            }
        }else if(!isInFrontRow){
            if (isLeftMoving){
                transform.Translate(new Vector3(0, 0, 1) * moveSpeed * Time.deltaTime);
            }else if(isRightMoving){
                transform.Translate(new Vector3(0, 0, -1) * moveSpeed * Time.deltaTime);
            }
        }
        
    }

    // 버튼을 누를 때 호출
    public void OnStartMoveLeft()
    {
        isLeftMoving = true;
        if (!isInFrontRow)
        {
            // 회전: Y축으로 180도 회전 (전체 파티가 회전해야 함)
            transform.Rotate(0, -180, 0);
            isInFrontRow = true; // 뒷열로 변경
        }
    }

    // 버튼에서 손을 뗄 때 호출
    public void OnStopMoveLeft()
    {
        isLeftMoving = false;
    }

    public void OnStartMoveRight(){
        isRightMoving = true;

        // 오른쪽 버튼 클릭 시 캐릭터의 모습 바꾸기 (앞모습으로 회전)
        if (isInFrontRow)
        {
            // 회전: Y축으로 180도 회전 (전체 파티가 회전해야 함)
            transform.Rotate(0, 180, 0);
            isInFrontRow = false; // 뒷열로 변경
        }

    }

    public void OnStopMoveRight(){
        isRightMoving = false;
    }
}
