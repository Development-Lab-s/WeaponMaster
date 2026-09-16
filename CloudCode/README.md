# SetUniquePlayerName 배포

1. Unity Dashboard에서 게임과 클라이언트가 사용하는 Environment를 선택합니다.
2. Cloud Save → Indexes에서 **Player Data / Default**, `PlayerName` (String, Ascending) 인덱스를 생성하고 사용 가능한 상태인지 확인합니다.
3. Cloud Code의 `SetUniquePlayerName` JavaScript 스크립트를 이 폴더의 JS 내용으로 교체합니다. `requestedName`은 필수 String 파라미터입니다. 저장 후 Publish합니다.
4. 서로 다른 계정으로 새 이름 성공, 중복 이름 거부, 본인 이름 재저장 성공, 재접속 후 이름 유지 여부를 확인합니다.

SDK 1.3에는 DefaultApi와 인덱스 조회 메서드가 없습니다. 1.4의 DataApi.queryDefaultPlayerData와 setItem을 사용합니다. 전체 플레이어 조회에는 context.serviceToken을 사용합니다. 중복은 `{ result: "Conflict" }`, 저장 성공은 `{ result: "Success", playerName }`으로 반환하고 서버 오류는 그대로 전파합니다.

기존 Public `V1_<playerId>` 프로필은 그대로 유지합니다. Default의 최상위 PlayerName이 기준 이름입니다. 초기화 시 이를 읽으므로 프로필 저장 시점에 따라 예전 이름이 복구되지 않습니다. Public 프로필에만 저장된 기존 닉네임은 이 인덱스에서 검색되지 않으므로 기존 사용자 데이터가 있다면 별도 마이그레이션이 필요합니다.

조회 후 저장 방식은 동시 요청 간 원자적 유일성을 보장하지 않습니다. 엄격한 고유 닉네임 보장이 필요하면 고유 제약을 지원하는 저장소 또는 원자적 이름 예약 기능이 필요합니다.

SUH의 BodyColorInterface는 ushort 인덱스를 Color로 바꾸는 `bodyColors` 팔레트를 Inspector에 지정해야 합니다. PlayerDataStore.BodyColor는 Color이므로 uint를 직접 저장하지 않습니다.

공식 SDK: https://cloud-code-sdk-documentation.cloud.unity3d.com/cloud-save/v1.4
