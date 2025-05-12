#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> candidates; // 상호작용 가능 오브젝트 목록
    [SerializeField] private List<GameObject> stairs;   // 계단 목록
    [SerializeField] private float interactionChance = 0.1f;    // 상호작용 적용 확률
    void Start()
    {
        AssingInteractables();
    }

    // 상호작용 랜덤 배정
    void AssingInteractables(){
        foreach(GameObject obj in candidates){
            if(Random.value <= interactionChance){
                if(!obj.TryGetComponent(out Interactable interactable)){
                    obj.AddComponent<Interactable>();
                    
                }
            }
        }
    }

    [SerializeField] private Transform parentObject; // 상호작용 오브젝트 그룹의 부모
    [SerializeField] private string[] nameFilters = { "Cabinet", "vending machine V2", "water purifier" };
    [SerializeField] private string[] stairFilters = {"UpStairs", "DownStairs"};

    [ContextMenu("Scan Children by Name and Add to Candidates")]
    public void ScanChildrenByNameAndAddToCandidates()
    {
#if UNITY_EDITOR
        if (parentObject == null)
        {
            Debug.LogError("❌ parentObject가 설정되지 않았습니다.");
            return;
        }

        int objCount = 0;
        int stairCount = 0;

        foreach (Transform child in parentObject)
        {
            string objName = child.name.ToLower();

            // 오브젝트 추가
            foreach (string keyword in nameFilters)
            {
                if (objName.Contains(keyword.ToLower()))
                {
                    GameObject obj = child.gameObject;

                    // 중복 방지
                    if (!candidates.Contains(obj))
                    {
                        candidates.Add(obj);
                        Debug.Log($"✔️ 추가됨: {obj.name}");
                        objCount++;
                    }

                    break;
                }
            }

            // 계단 추가
            foreach(string keyword in stairFilters)
            {
                if(objName.Contains(keyword.ToLower()))
                {
                    GameObject obj = child.gameObject;

                    if (!stairs.Contains(obj))
                    {
                        stairs.Add(obj);
                        Debug.Log($"✔️ 추가됨: {obj.name}");
                        stairCount++;
                    }

                    break;
                }
            }
        }
        EditorUtility.SetDirty(this);   // 추가 후 저장
        Debug.Log($"✅ 누적 등록 완료 (새로 추가된 오브젝트 항목: {objCount}개)");
        Debug.Log($"✅ 누적 등록 완료 (새로 추가된 계단 항목: {stairCount}개)");
#endif
    }

    [ContextMenu("🧹 Clear Candidates List")]
    public void ClearCandidatesList()
    {
#if UNITY_EDITOR
        int previousCount = candidates.Count;
        candidates.Clear();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"🧹 candidates 리스트 초기화 완료 (기존 항목 {previousCount}개 삭제)");
#endif
    }

    [ContextMenu("🧹 Clear Stairs List")]
    public void ClearStairsList()
    {
#if UNITY_EDITOR
        int previousCount = candidates.Count;
        stairs.Clear();
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"🧹 stairs 리스트 초기화 완료 (기존 항목 {previousCount}개 삭제)");
#endif
    }
}

