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

    public System.Action OnReset; // 콜백

    public void RegisterResetCallback(System.Action callback)
    {
        OnReset += callback;
    }

    void Start()
    {
        if (partySelector != null)
            RegisterResetCallback(partySelector.ResetSelectorState);

        if (heroListUp != null)
            RegisterResetCallback(heroListUp.ResetHeroListState);

        //if (itemList != null)
            //RegisterResetCallback(() => itemList.SetAllEquipButtonsInteractable(true));
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            if (IsClickInsideAnyUI())
                return;

            partySelector.ResetPartySlotInteractable();
            OnReset?.Invoke(); // 🔥 한꺼번에 초기화
        }
    }

    bool IsClickInsideAnyUI()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
            return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            foreach (var root in exemptUIRoots)
            {
                if (root != null && result.gameObject.transform.IsChildOf(root.transform))
                    return true;
            }
        }
        return false;
    }
}
