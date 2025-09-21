using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("Cinemachine Camera")]
    [SerializeField] private CinemachineVirtualCamera dungeonCam;
    public float speed = 3f;

    void Start()
    {

    }

    void Update()
    {
        
    }

    void LateUpdate()
    {
        if (DungeonManager.instance.partyTransform == null) return;

        float zoffset = 10f;    // 기본 카메라 위치 값


        if(DungeonManager.instance.currentDir == MoveDirection.Left)
        {
            zoffset = 0f;
        }else if (DungeonManager.instance.currentDir == MoveDirection.Right)
        {
            zoffset = 45f;
        }

        float targetZ = Mathf.Lerp(transform.position.z, DungeonManager.instance.partyTransform.position.z
                                         + zoffset, Time.deltaTime * speed);
        transform.position = new Vector3(transform.position.x, transform.position.y, targetZ);
        

    }

    
}
