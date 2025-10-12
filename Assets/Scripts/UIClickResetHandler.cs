using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIClickResetHandler : MonoBehaviour
{
    [SerializeField] private List<GameObject> exemptUIRoots;
    [SerializeField] private PartySelector partySelector;
    [SerializeField] private HeroListUp heroListUp;
    [SerializeField] private ItemList itemList;

    // === ListUIBase에서 클릭 직후 1프레임 리셋 억제용 ===
    bool _suppressNextReset;

    public System.Action OnReset; // 콜백

    public void RegisterResetCallback(System.Action callback) => OnReset += callback;
    public void UnregisterResetCallback(System.Action callback) => OnReset -= callback;

    /// 버튼 onClick 직전에 호출 (동일 프레임 리셋 방지)
    public void SuppressOnce() => _suppressNextReset = true;

    void Start()
    {
        if (partySelector != null)
            RegisterResetCallback(partySelector.ResetSelectorState);

        if (heroListUp != null)
            RegisterResetCallback(heroListUp.ResetHeroListState);

        if (itemList != null)
            RegisterResetCallback(itemList.ResetItemListState);
    }

    void Update()
    {
        // --- 마우스(에디터/PC) ---
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonUp(0))
        {
            HandlePointerUp(-1, Input.mousePosition);
        }
#else
        // --- 터치(모바일) ---
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Ended)
            {
                HandlePointerUp(t.fingerId, t.position);
            }
        }
#endif
    }

    void HandlePointerUp(int pointerId, Vector2 screenPos)
    {
        if (_suppressNextReset) { _suppressNextReset = false; return; }

        // ✅ 1) Raycast와 무관하게, 우선 exempt 루트 사각형 안이면 리셋 금지
        if (IsInsideAnyExemptRect(screenPos)) return;

        bool overUI = false;
        if (EventSystem.current != null)
        {
            overUI = (pointerId >= 0)
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        if (overUI)
        {
            var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                foreach (var root in exemptUIRoots)
                {
                    if (root != null && r.gameObject.transform.IsChildOf(root.transform))
                    {
                        return; // 제외된 UI 영역 안 → 리셋 금지
                    }
                }
            }

            // UI 위이지만 제외 영역은 아님 → 보통 리셋 안 함(기존 유지)
            return;
        }

        // UI 바깥 클릭으로 판정 → 리셋
        OnReset?.Invoke();
    }
    bool IsInsideAnyExemptRect(Vector2 screenPos)
    {
        if (exemptUIRoots == null) return false;
        foreach (var root in exemptUIRoots)
        {
            if (root == null) continue;
            var rt = root.transform as RectTransform;
            if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                return true;
        }
        return false;
    }
}
