# 악세사리 연결 / UGS 설정

## 저장과 표시
- 기존 Public Player Data `V1_<UGS Player ID>` JSON 안의 `HairStyle`을 `AccessoryStyle`로 변경했다.
- 예전 JSON은 읽을 때 자동 변환하고 다음 프로필 저장 시 새 키만 기록한다. 두 키가 함께 있으면 `AccessoryStyle`이 우선한다(0도 포함).
- `0`: 없음, `1`: `Torus.002`, `2`: `Torus.003`. 로비의 기존 `ChangeBtnHair` 버튼 연결은 유지된다.
- `Player.Spawned`가 모든 클라이언트에서 `PlayerAccessories`를 붙인다. 기존 플레이어/로비 모델 하위에서 정확한 이름으로 찾으며 비활성 오브젝트도 찾는다.
- 선택값과 왕관은 기존 `Player`의 Fusion Networked 상태이므로 늦게 들어온 플레이어도 받는다. 새 NetworkBehaviour를 프리팹에 추가할 필요는 없다.

## 관리자 데이터: 배포 필요
1. 이 프로젝트와 같은 UGS 환경에 JavaScript Cloud Code 스크립트 `GetPlayerAdminStatus`를 생성하고 옆의 `CloudCode/GetPlayerAdminStatus.js`를 붙여 넣어 Publish한다.
2. 필수 String 매개변수 `targetPlayerId`를 등록한다.
3. Cloud Save → 대상 플레이어 → **Protected** Player Data에 `IsAdmin` 키를 JSON Boolean **true** 또는 **false**로 등록한다. 문자열 `"true"`는 허용하지 않는다. 키가 없으면 false이다.
4. `IsAdmin`은 일반 Public 프로필 `V1_...` 안에 넣지 않는다. 게임 클라이언트는 관리자 권한을 저장하거나 올릴 수 없으며 대시보드/서비스 계정에서 관리한다.

각 관찰 클라이언트가 Cloud Code의 서버 판정을 받아 `policehat`을 표시한다. 성공 후 60초마다 갱신한다. 최초 조회가 끝나기 전에는 모자와 왕관을 모두 숨긴다. 조회 실패 시 마지막으로 확인된 관리자 상태를 유지하고 30초 뒤 재시도하되, 관리자 상태가 확인되지 않은 일반 표시 경로에서는 로컬 1등 왕관을 허용한다. 스크립트 미배포(NotFound)가 왕관까지 차단하지 않는다. 이후 관리자로 확인되면 즉시 왕관을 숨기고 관리자 모자로 전환한다. 관리자는 순위에 관계없이 관리자 모자만 표시하며 왕관은 표시하지 않는다.

## 로컬 왕관
기존 로컬 리더보드의 KillCount 내림차순, 이름 오름차순을 재사용한다. 동률은 PlayerId로 결정한다. 마스터 클라이언트만 순위 결과를 RPC로 보내고 수신 측은 실제 Fusion 마스터 발신자인지 검사한다. 결과를 각 플레이어의 Networked 상태에 기록한다. 퇴장/순위 변경/마스터 교체 후 다음 리더보드 갱신에 다시 결정한다.

Shared 모드의 마스터는 전용 신뢰 서버가 아니다. 이 변경은 원격 클라이언트까지 표시를 일치시키며 UGS가 관리자 값을 판정하지만, 기존 클라이언트 소유 `playerId`와 킬 데이터의 위변조까지 방어하는 인증 시스템을 추가하지는 않는다.

## 검증
- JavaScript: `node Assets/WorkFolder/SUH/Scripts/CharacterVisual/CloudCode/GetPlayerAdminStatus.test.js`
- Unity 에디터에서 스크립트 재컴파일/Fusion weaving 후 같은 빌드의 클라이언트 2개로 확인한다.
- 선택 0/1/2 저장 후 재입장, 나중 입장한 클라이언트의 목걸이/왕관, 관리자 true/false, 1등 퇴장, 마스터 퇴장 후 왕관 이전을 확인한다.
- UGS 실제 배포 및 두 클라이언트 플레이 검증은 로컬 코드 빌드와 별도로 필요하다.

Protected 접근 근거: https://docs.unity.com/en-us/cloud-save/tutorials/cloud-code

## 등장 연출

모자/왕관이 새로 표시될 때 원래 부착 위치의 월드 위쪽 0.3m에서 파티클 24개를 방출한다. 0.12초 뒤 아이템이 0.2초 동안 나타나고, DOTween으로 0.42초 동안 원래 로컬 위치로 내려온다. 원래 회전/크기를 보존한다. 관리자 파티클은 파랑, 왕관은 금색이다. 상태가 유지되면 연출을 반복하지 않으며, 표시 해제/비활성/파괴 시 트윈을 취소하고 위치를 복구한다. 각 관찰 클라이언트가 상태 변화 수신 시 연출하므로 시작 프레임은 네트워크 수신 시점에 따라 다를 수 있다.
