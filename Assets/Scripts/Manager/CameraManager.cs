using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    [Header("Cinemachine Camera")]
    [SerializeField] private CinemachineVirtualCamera dungeonCam;
    private float zoomSize = 12f;

    public float speed = 3f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 기본 우선순위: dungeonCam=10 > battleCam=8 > partyFocusCam=5 / enemyFocusCam=5 (전투 중엔 battleCam이 기본 활성)
        if (dungeonCam) dungeonCam.Priority = 10;
    }

    void LateUpdate()
    {
        if (DungeonManager.instance.partyTransform == null) return;

        float zoffset = 10f;    // 기본 카메라 위치 값


        if (DungeonManager.instance.currentDir == MoveDirection.Left)
        {
            zoffset = 0f;
        }
        else if (DungeonManager.instance.currentDir == MoveDirection.Right)
        {
            zoffset = 45f;
        }

        float targetZ = Mathf.Lerp(transform.position.z, DungeonManager.instance.partyTransform.position.z
                                         + zoffset, Time.deltaTime * speed);
        transform.position = new Vector3(transform.position.x, transform.position.y, targetZ);
    }

    public void ZoomTo(Transform target, float duration = 0.25f)
    {
        StopAllCoroutines();
        StartCoroutine(CoZoomTo(target, zoomSize, duration));
    }

    IEnumerator CoZoomTo(Transform target, float targetSize, float duration)
    {
        float t = 0f;
        float startSize = dungeonCam.m_Lens.OrthographicSize;
        Transform camTrans = dungeonCam.transform;

        // 프레이밍 보정: 카메라를 타깃의 x/y로 부드럽게 이동 (Orthographic이라 z는 의미 적음)
        Vector3 startPos = camTrans.position;
        Vector3 goalPos = new Vector3(target.position.x, target.position.y, startPos.z);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            dungeonCam.m_Lens.OrthographicSize = Mathf.Lerp(startSize, targetSize, Mathf.SmoothStep(0,1,t));
            camTrans.position = Vector3.Lerp(startPos, goalPos, Mathf.SmoothStep(0,1,t));
            yield return null;
        }
    }

}
