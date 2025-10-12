using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;                // Json.NET 직렬화/역직렬화
using Save;
using System.Linq;
using System.Collections.Generic;

public static class SaveSystem         // 인스턴스 없이 사용
{
    // 파일명은 버전과 분리(파일 내부에 version 유지)
    private const string FileName = "save_v2.json";   // 저장 파일 이름
    private const string BackupName = "save_v2.bak";  // 백업 파일 이름

    // persistentDataPath 아래에 실제 저장 파일 절대 경로 생성
    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    // 동일 폴더에 백업 파일 절대 경로
    private static string BackupPath => Path.Combine(Application.persistentDataPath, BackupName);

    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.None,              // 압축 저장(공백 X => 파일 크기 감소)
        TypeNameHandling = TypeNameHandling.None,  // 타입 정보 미포함(보안/호환성↑)
        NullValueHandling = NullValueHandling.Ignore // null 필드 생략
    };
    // Json.NET 직렬화 옵션을 한 곳에 고정해 재사용



    // ==== 새 데이터 만들기 ====
    public static Save.SaveGame NewSave()
    {
        // 프로젝트 기존 팩토리 & 정규화 루틴을 그대로 활용한다고 가정
        var fresh = CreateNewSave();     // 이미 존재하는 내부 팩토리
        NormalizeAfterLoad(fresh);       // 로드 후 정규화 루틴
        return fresh;
    }

    public static async System.Threading.Tasks.Task<Save.SaveGame> ResetToNewAsync()
    {
        var fresh = NewSave();
        await SaveAsync(fresh);
        return fresh;
    }

    public static bool HasAnySaveFile()
    {
        try
        {
            return System.IO.File.Exists(SavePath) || System.IO.File.Exists(BackupPath);
        }
        catch
        {
            return false;
        }
    }

#if UNITY_EDITOR
    public static bool DeleteAllSaveFiles()
    {
        try
        {
            bool any = false;
            if (System.IO.File.Exists(SavePath))
            {
                System.IO.File.Delete(SavePath);
                any = true;
            }
            if (System.IO.File.Exists(BackupPath))
            {
                System.IO.File.Delete(BackupPath);
                any = true;
            }
            return any;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[SaveSystem] DeleteAllSaveFiles failed: {e}");
            return false;
        }
    }
#endif

    // 저장(원자적). 임시파일에 먼저 쓰고 교체. 실패 시 백업 유지.
    public static async Task<bool> SaveAsync(SaveGame data) // 세이브 비동기 함수(true/false로 성공 여부 반환)
    {
        try
        {
            // 직렬화 (json)
            string json = JsonConvert.SerializeObject(data, JsonSettings);

            // 임시 파일 경로
            string tempPath = SavePath + ".new";
            // 교체 전 임시 파일(.new) 작성

            // 저장 폴더 보장
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);

            // 임시 파일에 우선 기록
            await File.WriteAllTextAsync(tempPath, json, new UTF8Encoding(false));

            // 기존 파일이 있으면 백업
            if (File.Exists(SavePath))
            {
                // 기존 백업 삭제 후 교체
                if (File.Exists(BackupPath))
                    File.Delete(BackupPath);

                // 현재 저장 파일을 백업 파일로 복사
                File.Copy(SavePath, BackupPath);
            }

            // 기존 저장 삭제 -> 본 저장 교체
            if (File.Exists(SavePath))
                File.Delete(SavePath);

            File.Move(tempPath, SavePath);
            // 플랫폼에 따라 File.Replace가 안될 수 있어 Move로 대체

            return true; // 성공
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e}");
            return false; // 실패
        }
    }

    // 세이브 파일 로드 실패 시 백업 로드
    public static async Task<SaveGame> LoadAsync() // 로드 비동기 함수(항상 SaveGame 반환)
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                // 없으면 신규 데이터 반환
                var fresh = CreateNewSave();
                NormalizeAfterLoad(fresh);
                return CreateNewSave();
                // 첫 실행 등 저장 파일이 없으면 새 데이터로 시작
            }

            string json = await File.ReadAllTextAsync(SavePath, Encoding.UTF8);
            // 본 저장 파일을 UTF-8로 읽기

            var data = JsonConvert.DeserializeObject<SaveGame>(json, JsonSettings);
            // JSON → SaveGame 객체로 복원

            if (data == null)
                throw new Exception("Deserialized SaveGame is null");
            // 역직렬화 실패 방지 확인

            return data; // 성공적으로 읽은 모델 반환
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Load failed: {e}. Try backup...");
            // 본 저장 읽기 실패(깨짐/부분쓰기) 시 백업 복구 시도

            // 백업 복구 시도
            try
            {
                if (File.Exists(BackupPath))
                {
                    // 백업 파일 읽기
                    string json = await File.ReadAllTextAsync(BackupPath, Encoding.UTF8);

                    // 역직렬화
                    var data = JsonConvert.DeserializeObject<SaveGame>(json, JsonSettings);

                    if (data != null)
                    {
                        // 백업으로 복구 저장
                        await SaveAsync(data);

                        return data; // 복구된 데이터 반환
                    }
                }
            }
            catch (Exception e2)
            {
                Debug.LogError($"[SaveSystem] Backup load failed: {e2}");
                // 백업까지 실패하면 로그만 남김
            }

            // 최종 실패 -> 신규 세이브
            return CreateNewSave();
            // 어떤 방식으로도 못 읽으면 기본값 새로 생성
        }
    }

    private static SaveGame CreateNewSave() // 신규 세이브 기본값 팩토리
    {
        return new SaveGame
        {
            version = 1,                                   // 스키마 최신 버전으로 생성
            playerId = SystemInfo.deviceUniqueIdentifier,  // 장치 고유 ID(필요 시 식별용)
            inventory = new Save.InventorySave
            {
                slots = new()          // 실제 슬롯 내용 초기화(전부 빈 칸)
            },
            gold = 5000,   // 재화 초기값
            redSoul = 0,
            blueSoul = 0,
            purpleSoul = 0,
            greenSoul = 0
        };
    }

    // 세이브 파일 생성 시 호출
    private static void NormalizeAfterLoad(SaveGame data)
    {
        // 리스트/딕셔너리 null 방지
        data.heroes ??= new List<HeroSave>();

        foreach (HeroSave hero in data.heroes)
        {
            hero.skillLevels ??= new Dictionary<int, int>();

            // 보유 영웅 id 호출
            int ownHeroId = hero.heroId;

            NormalizeSkillLevelsForHero(hero, ownHeroId);
        }
    }

    /// <summary>
    /// 현재 보유 영웅 스킬 레벨 정규화
    /// hero.skillLevels에서 누락된 스킬ID는 0으로 채우고,
    /// 카탈로그에 없는 키는 제거
    /// </summary>
    private static void NormalizeSkillLevelsForHero(HeroSave hero, int ownHeroId)
    {
        // 영웅(직업)별 스킬 ID 목록 가져오기
        var ids = SkillCatalog.GetHeroSkillIds(ownHeroId);

        // 누락 키 → 0으로 채우기
        for (int i = 0; i < ids.Count; i++)
        {
            int id = ids[i];
            if (!hero.skillLevels.ContainsKey(id))
                hero.skillLevels[id] = 0;
        }

        // 카탈로그에 없는 키 제거
        // 열거 중 변경 방지를 위해 임시 리스트 사용
        var toRemove = new List<int>();
        foreach (var key in hero.skillLevels.Keys)    // key = skillId
            if (!ids.Contains(key))
                toRemove.Add(key);
        for (int i = 0; i < toRemove.Count; i++)
            hero.skillLevels.Remove(toRemove[i]);
    }

}
