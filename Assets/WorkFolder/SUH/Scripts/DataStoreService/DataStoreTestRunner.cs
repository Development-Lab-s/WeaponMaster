using UnityEngine;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;

// 테스트용 임시 데이터 클래스
[System.Serializable]
public class TestPlayerData
{
    public string Nickname;
    public int Gold;
    public string SkinColorHex;
}

public class DataStoreTestRunner : MonoBehaviour
{
    private async void Start()
    {
        Debug.Log("== DataStore 테스트 시작 ==");

        // 1. UGSDataStore 생성 (스토어 이름: "TestStore")
        var store = DataStoreService.GetDataStore<TestPlayerData>("TestStore");

        // 2. 저장(SetAsync) 테스트
        var dummyData = new TestPlayerData
        {
            Nickname = "TestUser_01",
            Gold = 500,
            SkinColorHex = "#00FF00"
        };
        
        Debug.Log("[1] 데이터 저장 시도...");
        await store.SetAsync("UserSlot", dummyData);
        Debug.Log("[1] 데이터 저장 완료!");

        // 3. 불러오기(GetAsync) 테스트
        Debug.Log("[2] 데이터 불러오기 시도...");
        var loadedData = await store.GetAsync("UserSlot");
        Debug.Log($"[2] 로드 성공! 닉네임: {loadedData.Nickname}, 골드: {loadedData.Gold}, 색상: {loadedData.SkinColorHex}");

        // 4. 수정(UpdateAsync) 테스트
        Debug.Log("[3] 데이터 수정(골드 +100) 시도...");
        await store.UpdateAsync("UserSlot", current =>
        {
            current.Gold += 100;
            return current;
        });

        var updatedData = await store.GetAsync("UserSlot");
        Debug.Log($"[3] 수정 검증: 현재 골드 = {updatedData.Gold}");

        Debug.Log("== 모든 DataStore 동작 정상 완료 ==");
    }
}