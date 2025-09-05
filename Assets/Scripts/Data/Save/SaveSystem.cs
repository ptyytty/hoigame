using System;                
using System.IO;                  
using System.Text;                    
using System.Threading.Tasks;         
using UnityEngine;                    
using Newtonsoft.Json;                // Json.NET 직렬화/역직렬화
using Save;                           // 만든 Save 네임스페이스(SaveGame 등 모델들)

public static class SaveSystem         // 인스턴스 없이 어디서든 쓰는 정적 유틸 클래스
{
    // 파일명은 버전과 분리(파일 내부에 version 유지)
    private const string FileName = "save_v2.json";   // 실제 저장 파일 이름(스키마 버전은 파일 내부에 둠)
    private const string BackupName = "save_v2.bak";  // 백업 파일 이름
    private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
    // persistentDataPath 아래에 실제 저장 파일의 절대 경로를 만든다

    private static string BackupPath => Path.Combine(Application.persistentDataPath, BackupName);
    // 동일 폴더에 백업 파일 절대 경로

    private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        Formatting = Formatting.None,              // 공백 없이 압축 저장(파일 크기↓)
        TypeNameHandling = TypeNameHandling.None,  // 타입 정보 미포함(보안/호환성↑)
        NullValueHandling = NullValueHandling.Ignore // null 필드는 JSON에서 생략
    };
    // Json.NET 직렬화 옵션을 한 곳에 고정해 재사용

    /// <summary>
    /// 저장(원자적). 임시파일에 먼저 쓰고 교체. 실패 시 백업 유지.
    /// </summary>
    public static async Task<bool> SaveAsync(SaveGame data) // 세이브 비동기 함수(true/false로 성공 여부 반환)
    {
        try
        {
            // 직렬화
            string json = JsonConvert.SerializeObject(data, JsonSettings);
            // SaveGame 객체 → JSON 문자열

            // 임시 파일 경로
            string tempPath = SavePath + ".new";
            // 교체 전 임시 파일(.new)에 먼저 쓴다 (도중 크래시 대비)

            // 디렉토리 보장
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            // 저장 폴더가 없을 수 있으니 만들어 둠. !는 null 경고 억제(여기선 null 아님)

            // 임시 파일에 먼저 기록(UTF8 BOM 없음)
            await File.WriteAllTextAsync(tempPath, json, new UTF8Encoding(false));
            // 실제 파일에 쓰기 전에 임시 위치에 기록. BOM 없는 UTF-8로 저장

            // 기존 파일이 있으면 백업
            if (File.Exists(SavePath))
            {
                // 기존 백업 삭제 후 교체
                if (File.Exists(BackupPath))
                    File.Delete(BackupPath);
                // 이전 백업은 지우고

                File.Copy(SavePath, BackupPath);
                // 현재 저장 파일을 백업 파일로 복사(백업 최신화)
            }

            // 플랫폼에 따라 File.Replace가 안될 수 있어 Move로 대체
            // 우선 기존 저장 삭제 후 임시->본 저장 교체
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            // 기존 본 저장 삭제(동일 경로 Move를 위해 비워둠)

            File.Move(tempPath, SavePath);
            // 임시 파일을 본 저장 파일명으로 “원자적 교체”에 가깝게 이동(같은 드라이브 내 rename은 매우 안전)

            return true; // 성공
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e}");
            return false; // 실패
        }
    }

    /// <summary>
    /// 세이브 파일 로드 실패 시 백업 로드(백업 자동 복구 시도).
    /// </summary>
    public static async Task<SaveGame> LoadAsync() // 세이브 로드 비동기 함수(항상 SaveGame 반환)
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                // 없으면 신규 데이터 반환
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
                    string json = await File.ReadAllTextAsync(BackupPath, Encoding.UTF8);
                    // 백업 파일 읽기

                    var data = JsonConvert.DeserializeObject<SaveGame>(json, JsonSettings);
                    // 역직렬화

                    if (data != null)
                    {
                        // 백업으로 복구 저장
                        await SaveAsync(data);
                        // 백업 내용을 본 저장으로 다시 저장(백업→본 저장 복구)

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
}
