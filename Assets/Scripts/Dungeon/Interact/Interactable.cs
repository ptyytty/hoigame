using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 상호작용 오브젝트 자동 부착
public class Interactable : MonoBehaviour
{
    [Header("One-time reward for Untagged")]
    public bool oneTimeReward = true;
    [SerializeField] private bool claimed = false;  // 한번 보상 받았는지

    OutlineDuplicator _outline;

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        TryGetComponent(out _outline);
    }

    public bool IsEligibleForReward
        => CompareTag("Untagged")
           && (!oneTimeReward || !claimed)
           && gameObject.activeInHierarchy;

    public void MarkClaimed()
    {
        claimed = true;
    }

    public void ShowUI(bool show)
    {
        if (CompareTag("Upstair"))
            InteractableManager.instance.interactionUp.gameObject.SetActive(show);
        else if (CompareTag("Downstair"))
            InteractableManager.instance.interactionDown.gameObject.SetActive(show);
        else if (CompareTag("Untagged"))
        {
            // ✅ 보상 가능할 때만 오브젝트 버튼 노출
            if (!IsEligibleForReward) show = false;
            InteractableManager.instance.interactionObj.SetActive(show);
        }

        if (_outline) _outline.EnableOutline(show);
    }
}
