using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionScanner : MonoBehaviour
{
    public float detectionDistance = 40f;   // 탐지 거리
    [SerializeField] private Transform partyTransform;
    private Interactable currentTarget; // 일반 오브젝트 우선순위용
    private List<Interactable> currentStairs = new List<Interactable>(); // 계단 다중 표시용
    private float coneAngle = 70f;
    private float coneOffsetAngle = 0f;

    void Update()
    {
        ScanForInteractable();
    }
    
    void ScanForInteractable()
    {
        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 forward = GetDiagonalDirection(DungeonManager.instance.currentDir, 0f);

        Collider[] hits = Physics.OverlapSphere(origin, detectionDistance);
        
        Interactable closest = null;
        float closestAngle = Mathf.Infinity;
        List<Interactable> detectedStairs = new List<Interactable>();

        foreach (Collider col in hits)
        {
            Interactable interactable = col.GetComponent<Interactable>();
            if (interactable == null) continue;

            Vector3 dirToTarget = (col.transform.position - origin).normalized;
            float angle = Vector3.Angle(forward, dirToTarget);

            if (angle <= coneAngle / 2f)
            {
                // 계단 오브젝트는 모두 따로 처리
                if (interactable.CompareTag("Upstair") || interactable.CompareTag("Downstair"))
                {
                    detectedStairs.Add(interactable);
                }
                else
                {
                    if (angle < closestAngle)
                    {
                        closestAngle = angle;
                        closest = interactable;
                    }
                }
            }
        }

        // 일반 오브젝트 UI 처리
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

        // 계단 오브젝트 UI 처리
        foreach (var stair in currentStairs)
        {
            if (!detectedStairs.Contains(stair))
                stair.ShowUI(false);
        }

        foreach (var stair in detectedStairs)
        {
            if (!currentStairs.Contains(stair))
                stair.ShowUI(true);
        }

        currentStairs = detectedStairs;
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
