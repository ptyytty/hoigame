using UnityEngine;

public class Move : MonoBehaviour
{
    public float moveSpeed = 5f;

    // 이동을 계속하도록 할 변수
    private bool isMovingLeft = false;
    private bool isMovingRight = false;
    private bool isInFrontRow = true; // 앞열인지 뒷열인지 구분하는 변수

    // 왼쪽 버튼을 눌렀을 때 호출
    public void StartMoveLeft()
    {
        isMovingLeft = true;
        isMovingRight = false; // 오른쪽 이동 멈추기
    }

    // 왼쪽 버튼을 떼면 호출
    public void StopMoveLeft()
    {
        isMovingLeft = false;
    }

    // 오른쪽 버튼을 눌렀을 때 호출
    public void StartMoveRight()
    {
        isMovingRight = true;
        isMovingLeft = false; // 왼쪽 이동 멈추기

        // 오른쪽 버튼 클릭 시 캐릭터의 모습 바꾸기 (앞모습으로 회전)
        if (isInFrontRow)
        {
            // 회전: Y축으로 180도 회전 (전체 파티가 회전해야 함)
            transform.Rotate(0, 180, 0);
            isInFrontRow = false; // 뒷열로 변경
        }
    }

    // 오른쪽 버튼을 떼면 호출
    public void StopMoveRight()
    {
        isMovingRight = false;
    }

    void Update()
    {
        // 왼쪽 방향키로 계속 이동
        if (isMovingLeft)
        {
            Vector3 move = new Vector3(0, 0, -1) * moveSpeed * Time.deltaTime;
            transform.position += move;
        }

        // 오른쪽 방향키로 계속 이동
        if (isMovingRight)
        {
            Vector3 move = new Vector3(0, 0, 1) * moveSpeed * Time.deltaTime;
            transform.position += move;
        }
    }
}
