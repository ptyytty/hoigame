using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BattleInput : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera rayCamera;              // 비우면 Camera.main 사용
    [SerializeField] private LayerMask combatantLayer = ~0; // Combatant 레이어로 제한 권장

    [Header("Behavior")]
    [Tooltip("빈 공간 탭 시 타게팅 취소")]
    [SerializeField] private bool cancelOnEmptyTap = true;

    void Awake()
    {
        if (!rayCamera) rayCamera = Camera.main;
    }

    void Update()
    {
        if (Input.touchCount <= 0) return;

        // 첫 번째 터치만 사용(멀티터치는 필요해지면 확장)
        var t = Input.GetTouch(0);
        if (t.phase != TouchPhase.Began) return;

        // UI 위 터치 무시
        if (IsPointerOverUI(t.fingerId)) return;

        if (!BattleManager.Instance || !BattleManager.Instance.IsTargeting) return;
        if (!rayCamera) return;

        var ray = rayCamera.ScreenPointToRay(t.position);
        if (Physics.Raycast(ray, out var hit, 1000f, combatantLayer))
        {
            var c = hit.collider.GetComponentInParent<Combatant>();
            if (c != null)
            {
                BattleManager.Instance.NotifyCombatantClicked(c);
                return;
            }
        }

        if (cancelOnEmptyTap)
            BattleManager.Instance.CancelTargeting();
    }

    // === UI 위 터치면 true ===
    bool IsPointerOverUI(int fingerId)
    {
        if (EventSystem.current == null) return false;

        if (!EventSystem.current.IsPointerOverGameObject(fingerId)) return false;

        // 필요 시 정밀 필터
        var ped = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        return results.Count > 0;
    }
}