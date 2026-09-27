# Quiet Courtyard · 숨 고르는 안뜰

차분하고 편안한 로비를 위한 오리지널 무보컬 BGM.
부드러운 전자 건반, 가벼운 발현음, 낮은 베이스와 따뜻한 패드를 사용했습니다.
중간 구간에만 부드러운 관악 선율을 더했습니다. 드럼은 없습니다.
외부 음원이나 샘플 없이 음표를 작곡하고 합성한 버전입니다.

- 템포 / 조성: **80 BPM / D major / 4분의 4박자**
- 길이: **96초, 32마디**
- 포맷: **44.1 kHz / 16-bit PCM / stereo WAV**
- 구조: 0–24초 기본 선율 → 24–48초 반주 확장 → 48–72초 관악 변주 → 72–96초 여백을 둔 회귀

## 파일

- Unity용 원본: `Assets/WorkFolder/BK/06_Audio/BGM/Lobby_Quiet_Courtyard_80BPM.wav`
- 48초 미리듣기: `Lobby_Quiet_Courtyard_80BPM_Preview.wav` — 첫 24초와 변주 24초를 연결하고 끝을 페이드 처리했습니다.
- 루프 연결 확인: `Lobby_Quiet_Courtyard_80BPM_LoopCheck.wav` — 원본의 마지막 6초와 처음 6초를 연결했습니다. 6초 지점이 반복 경계입니다.
- 편집용 악보: `Lobby_Quiet_Courtyard_80BPM.mid` — MIDI 악기는 합성 WAV의 음색과 다를 수 있습니다.
- 재생성 스크립트: `render_lobby_bgm.py` — Python 3 + NumPy 필요. 프로젝트 루트에서 `python output/music/render_lobby_bgm.py`로 실행합니다.
- 수치 검증: `render_report.json`

## Unity에서 사용

로비의 AudioSource에 원본 WAV를 넣고 다음과 같이 설정합니다.

- **Loop**: 켜기
- **Spatial Blend**: 0 (2D)
- **Play On Awake**: 로비 진입 시 바로 재생할 경우 켜기
- **Volume**: 0.5부터 시작해서 효과음과 함께 조절
- **Output**: 프로젝트에서 사용하는 BGM AudioMixer 그룹에 연결

원본은 첫 샘플부터 마지막 샘플까지 정확히 32마디입니다. 음표 잔향과 리버브를
앞부분으로 순환 합산했으므로, 원본에 별도 페이드나 앞뒤 무음을 추가하지 않습니다.
시작/종료 페이드는 필요하면 AudioSource 볼륨으로 처리합니다.
Unity 임포터는 Streaming / Vorbis 품질 0.8로 설정했습니다.
씬의 AudioSource 연결은 사용자가 원하는 로비 오브젝트에서 설정하면 됩니다.

## 검증 범위

PCM WAV의 길이, 샘플 형식, 클리핑 여부와 반복 경계의 파형을 수치로 검사했습니다.
Unity에서 임포트 후 재생하거나 최종 믹서 볼륨으로 청감 확인한 상태는 아닙니다.
