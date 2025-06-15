using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionScanner : MonoBehaviour
{
    public float detectionDistance = 40f;   // 탐지 거리
    [SerializeField] private Transform partyTransform;
    private Interactable currentTarget;
    private float coneAngle = 70f;
    private float coneOffsetAngle = 0f;

    void Update()
    {
        ScanForInteractable();
    }

    // void ScanForInteractable()
    // {
    //     Vector3 origin = transform.position + Vector3.up * 1.2f;    // Party 위치에서 Y 값 올리기

    //     Vector3[] directions = new Vector3[]
    //     {
    //         GetDiagonalDirection(DungeonManager.instance.currentDir, 0f),
    //         GetDiagonalDirection(DungeonManager.instance.currentDir, -15f),
    //         GetDiagonalDirection(DungeonManager.instance.currentDir, 60f)
    //     };

    //     Color[] debugColors = new Color[] { Color.cyan, Color.yellow, Color.green };

    //     Interactable closest = null;
    //     float closestDistance = Mathf.Infinity;

    //     for (int i = 0; i < directions.Length; i++)
    //     {
    //         Vector3 dir = directions[i];
    //         Color rayColor = debugColors[i];

    //         Debug.DrawRay(origin, dir * detectionDistance, rayColor);

    //         if (Physics.Raycast(origin, dir, out RaycastHit hit, detectionDistance))
    //         {
    //             Interactable interactable = hit.collider.GetComponent<Interactable>();
    //             if (interactable != null)
    //             {
    //                 float distance = Vector3.Distance(origin, hit.point);
    //                 if (distance < closestDistance)
    //                 {
    //                     closestDistance = distance;
    //                     closest = interactable;
    //                 }
    //             }
    //         }
    //     }

    //     if (closest != null && closest != currentTarget)
    //     {
    //         if (currentTarget != null)
    //             currentTarget.ShowUI(false);

    //         currentTarget = closest;
    //         currentTarget.ShowUI(true);
    //     }
    //     else if (closest == null && currentTarget != null)
    //     {
    //         currentTarget.ShowUI(false);
    //         currentTarget = null;
    //     }
    // }

    // Vector3 GetDiagonalDirection(MoveDirection dir, float offsetAngle)
    // {
    //     float baseAngle = (dir == MoveDirection.Left) ? -120f : 120f;
    //     return (Quaternion.AngleAxis(baseAngle + offsetAngle, Vector3.up) * partyTransform.forward).normalized;
    // }


    void ScanForInteractable()
    {
        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 forward = GetDiagonalDirection(DungeonManager.instance.currentDir, 0f);

        Collider[] hits = Physics.OverlapSphere(origin, detectionDistance);
        Interactable closest = null;
        float closestAngle = Mathf.Infinity;

        foreach (Collider col in hits)
        {
            Interactable interactable = col.GetComponent<Interactable>();
            if (interactable == null) continue;

            Vector3 dirToTarget = (col.transform.position - origin).normalized;
            float angle = Vector3.Angle(forward, dirToTarget);

            if (angle <= coneAngle / 2f)
            {
                // 중심에 가까운 오브젝트 선택
                if (angle < closestAngle)
                {
                    closestAngle = angle;
                    closest = interactable;
                }
            }
        }

        // UI 처리
        if (closest != null && closest != currentTarget)
        {
            if (currentTarget != null)
                currentTarget.ShowUI(false);

            currentTarget = closest;
            currentTarget.ShowUI(true);
        }
        else if (closest == null && currentTarget != null)
        {
            currentTarget.ShowUI(false);
            currentTarget = null;
        }
    }

    Vector3 GetDiagonalDirection(MoveDirection dir, float offsetAngle)
    {
        float baseAngle = (dir == MoveDirection.Left) ? -120f : 120f;
        return (Quaternion.AngleAxis(baseAngle + offsetAngle, Vector3.up) * partyTransform.forward).normalized;
    }

    void OnDrawGizmos()
    {

        if (partyTransform == null) return;

        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 forward = GetDiagonalDirection(DungeonManager.instance != null ? DungeonManager.instance.currentDir : MoveDirection.Right, coneOffsetAngle);

        // 회전 각도 범위 계산
        float halfAngle = coneAngle / 2f;
        Quaternion leftRotation = Quaternion.AngleAxis(-halfAngle, Vector3.up);
        Quaternion rightRotation = Quaternion.AngleAxis(halfAngle, Vector3.up);

        Vector3 leftDir = leftRotation * forward;
        Vector3 rightDir = rightRotation * forward;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(origin, leftDir * detectionDistance);
        Gizmos.DrawRay(origin, rightDir * detectionDistance);
        Gizmos.DrawRay(origin, forward * detectionDistance);
        Gizmos.DrawWireSphere(origin, detectionDistance);
    }

}
