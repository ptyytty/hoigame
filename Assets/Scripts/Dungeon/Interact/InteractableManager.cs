#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableManager : MonoBehaviour
{
    public static InteractableManager instance;

    void Awake()
    {
        if(instance == null){
            instance = this;
        }else{
            Destroy(this.gameObject);
        }
    }
    
    [SerializeField] private List<GameObject> candidates; // 상호작용 가능 오브젝트 목록
    [SerializeField] private List<GameObject> stairs;   // 계단 목록
    [SerializeField] private float interactionChance = 0.1f;    // 상호작용 적용 확률

    
    [SerializeField] private string[] nameFilters = { "Cabinet", "vending machine V2", "water purifier" };
    [SerializeField] private string[] stairFilters = {"UpStairs", "DownStairs"};

    
    public GameObject interactionUI;
    void Start()
    {
        AutoFindFloorAndScan();
        AssingInteractables();
    }

    /// <summary>
    /// candidates 리스트에 있는 오브젝트 중 확률에 따라 Interactable 컴포넌트 부여
    /// </summary>
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
    /// <summary>
    /// "Floor" 태그를 가진 오브젝트를 찾아 그 하위 오브젝트들을 대상으로 candidates와 stairs 자동 등록
    /// </summary>
    void AutoFindFloorAndScan()
    {
        GameObject[] floorObject = GameObject.FindGameObjectsWithTag("Floor");

        if (floorObject == null || floorObject.Length == 0)
        {
            Debug.LogError("❌ 'Floor' 태그를 가진 오브젝트를 찾을 수 없습니다.");
            return;
        }

        int objCount = 0;
        int stairCount = 0;

        foreach (GameObject floor in floorObject)
        {
             foreach (Transform child in floor.transform)
            {
                string objName = child.name.ToLower();

                foreach (string keyword in nameFilters)
                {
                    if (objName.Contains(keyword.ToLower()) && !candidates.Contains(child.gameObject))
                    {
                        candidates.Add(child.gameObject);
                        objCount++;
                        break;
                    }
                }

                foreach (string keyword in stairFilters)
                {
                    if (objName.Contains(keyword.ToLower()) && !stairs.Contains(child.gameObject))
                    {
                        stairs.Add(child.gameObject);
                        stairCount++;
                        break;
                    }
                }
            }
        }
        Debug.Log($"✅ 자동 등록 완료: 상호작용 오브젝트 {objCount}개, 계단 {stairCount}개");
    }
}

