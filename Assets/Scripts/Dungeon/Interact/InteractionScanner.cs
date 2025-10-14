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
            Interactable it = col.GetComponent<Interactable>();
            if (!it) continue;

            Vector3 dirTo = (col.transform.position - origin).normalized;
            float angle = Vector3.Angle(forward, dirTo);
            if (angle > coneAngle / 2f) continue;

            if (it.CompareTag("Upstair") || it.CompareTag("Downstair"))
            {
                detectedStairs.Add(it);
            }
            else
            {
                // ✅ Untagged 중에서도 "보상 가능"한 것만 타깃 후보
                if (it.IsEligibleForReward && angle < closestAngle)
                {
                    closestAngle = angle;
                    closest = it;
                }
            }
        }

        // 일반 오브젝트 UI 토글
        if (closest != null && closest != currentTarget)
        {
            if (currentTarget != null) currentTarget.ShowUI(false);
            currentTarget = closest;
            currentTarget.ShowUI(true);
        }
        else if (closest == null && currentTarget != null)
        {
            currentTarget.ShowUI(false);
            currentTarget = null;
        }

        // 계단 UI 토글(복수)
        foreach (var s in currentStairs) if (!detectedStairs.Contains(s)) s.ShowUI(false);
        foreach (var s in detectedStairs) if (!currentStairs.Contains(s)) s.ShowUI(true);
        currentStairs = detectedStairs;

        // ✅ Manager에 현재 타깃/버튼 표시 상태 알림
        InteractableManager.instance?.SetCurrentObjectTarget(currentTarget);
        InteractableManager.instance?.SetInteractButtonsVisible(
            up: currentStairs.Exists(s => s && s.CompareTag("Upstair")),
            down: currentStairs.Exists(s => s && s.CompareTag("Downstair")),
            obj: currentTarget != null
        );
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
