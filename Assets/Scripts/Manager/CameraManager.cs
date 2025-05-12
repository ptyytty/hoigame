using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
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

        float zoffset = 10f;


        if(DungeonManager.instance.currentDir == MoveDirection.Left)
        {
            zoffset = 0f;
        }else if (DungeonManager.instance.currentDir == MoveDirection.Right)
        {
            zoffset = 40f;
        }

        float targetZ = Mathf.Lerp(transform.position.z, DungeonManager.instance.partyTransform.position.z
                                         + zoffset, Time.deltaTime * speed);
        transform.position = new Vector3(transform.position.x, transform.position.y, targetZ);
        

    }

    
}
