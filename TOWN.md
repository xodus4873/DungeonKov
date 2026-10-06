# 돌빛 마을 · Stonehaven

중세 쿼터뷰 도트 마을입니다. 기존 숲 전투 장면과 별도로 편집하고 실행합니다.

## 열기

- Unity: `Assets/Scenes/Town.unity`를 더블클릭하세요. Play 전에도 건물과 NPC가 보입니다.
- Windows: `Build/Town/Stonehaven.exe` 실행. 배포 시 `Build/Town` 폴더 전체를 복사합니다.
- 빌드 메뉴: **Mosslight → Build town**. 먼저 Ctrl+S로 장면을 저장하세요.
- 기존 숲: `Assets/Scenes/Mosslight.unity`.

## 조작과 서비스

우클릭으로 이동합니다. NPC 몸체를 좌클릭하면 건물을 피해 가까이 이동하고 대화창을 엽니다.
대화창의 서비스 버튼을 클릭하세요. 닫기 또는 Esc로 대화를 끝냅니다.

| 건물 / NPC | 서비스 |
| --- | --- |
| 대장간 / 대장장이 브람 | 40골드로 검 강화. 최대 +3 |
| 교회 / 사제 엘린 | 무료 체력 회복. 20골드로 최대 체력 +2 축복, 한 번만 가능 |
| 잡화점 / 잡화상 미라 | 15골드로 회복 물약 구매. 하단 물약 사용 버튼으로 체력 3 회복 |

시작 골드는 120, 시작 체력은 4/6입니다. 치료와 물약을 바로 시험해 볼 수 있습니다.
이 마을은 상호작용 시제품입니다. 돈·물약·검·축복은 현재 실행 중에만 유지됩니다.
검 강화 수치는 마을 UI에 반영되며 기존 숲의 공격력과 아직 연결되지 않았습니다.
건물 내부 진입과 맵 간 이동은 아직 구현하지 않았습니다.

## 직접 수정하기

- **Town — editable map / Buildings**: 건물 이동·크기 조정. `Town Obstacle → Ground Size`는 바닥 충돌 크기입니다.
- **NPCs — edit dialogue and prices**: NPC 위치와 `Town Npc`의 Display Name, Greeting, Price 수정.
- **Ground and roads**: 바닥 타일 배치와 Sprite Renderer 색상 수정.
- **Trees and lamps**: 나무·가로등·장식 배치.
- **Player — editable start**: 플레이어 시작 위치.
- **Town — Settings**: 이동 속도, 시작 골드, 시작 체력.
- **Assets/Town/Prefabs**: 건물과 NPC 프리팹. Scene으로 드래그해서 재사용합니다.
- **Main Camera**: 위치·Orthographic Size로 화면 구도 조정.

맵 이동 범위는 논리 좌표 ±36의 마름모입니다. 기존 ±9에서 가로·세로가 각각 4배, 이동 가능 면적이 16배가 됐습니다. 바닥 타일도 19×19(361개)에서 76×76(5,776개)로 16배 늘었습니다. NPC는 건물 충돌 영역 밖에 배치해야 접근할 수 있습니다.
영구 변경은 Play를 끈 상태에서 하고 Ctrl+S로 저장하세요. 빌드는 저장된 Town 장면을 그대로 사용합니다.

## 코드와 확인 기록

- `Assets/Scripts/TownGame.cs`: 마우스 이동, 대화 UI, 거래·치료·축복·물약
- `Assets/Scripts/TownNpc.cs`: NPC 대사·이름·서비스 설정
- `Assets/Scripts/TownObstacle.cs`: 건물과 나무의 충돌 영역
- `Assets/Editor/TownBuilder.cs`: 초기 마을 생성, 도트 건물 에셋 생성, 빌드
- `TownPreview.png`: 저장된 마을 장면의 카메라 렌더
- `TownBuild.log`, `TownSmoke.log`: 빌드와 자동 검사 기록

자동 검사는 세 NPC까지의 경로, 건물 회피, 검 강화, 물약 구매·사용, 치료, 중복 축복 방지, 골드 부족 처리를 확인합니다.
실행 파일에 `-batchmode -nographics --town-smoke-test -logFile TownSmoke.log` 인자를 주면 재검사합니다.

## 확장 마을 디자인

- 중앙: 테두리 무늬를 넣은 석조 광장, 우물, 화단과 깃발.
- 공방 구역: 대장간과 자재 상자, 따뜻한 붉은 지붕.
- 교회 구역: 큰 교회와 밝은 석판 앞마당, 꽃 정원.
- 시장 구역: 잡화점과 6개 노점, 가로등과 보행로.
- 주거 구역: 황토·슬레이트·벽돌색 지붕의 주택 20채.
- 외곽: 밀밭, 숲, 과수원, 개울과 3개 나무다리.
- 바닥: 세 종류의 풀 타일과 자갈길 변형, 광장 석판, 밭고랑, 물결, 다리 판자 텍스처.

휠로 확대·축소하고 **M**으로 지도를 여세요. 지도에서 우클릭하면 먼 지점까지 이동합니다.
카메라는 플레이어를 따라갑니다. Town — Settings에서 Map Limit, Follow Player, Camera Zoom을 조절할 수 있습니다.
새 장애물 배치는 Play 시작 시 경로 탐색에 반영됩니다. 실행 중 장애물 이동은 경로 캐시 재생성이 필요하므로 편집은 Play를 끄고 진행하세요.
기존 마을 백업은 Assets/Scenes/Backups 폴더에 있습니다.
전체 미리보기: TownPreview.png / 중심 구역 미리보기: TownDistrictPreview.png.

## 중앙 포탈 광장

중앙 우물을 제거하고 룬 원형 바닥과 푸른 던전 포탈을 배치했습니다.
포탈을 좌클릭하면 가까이 이동한 뒤 입장창이 열립니다. **던전 입장**을 누르면 기존 숲 던전(Mosslight)으로 이동합니다.
던전에서 Esc를 눌러 일시정지하거나 승리/패배 화면에서 **RETURN TO TOWN** 버튼으로 돌아올 수 있습니다.
귀환 위치는 포탈 앞이며, 마을의 골드·물약·검 강화·축복 상태는 왕복 시 유지됩니다. 던전은 입장할 때마다 초기화됩니다.
마을 장비 수치를 던전 전투에 적용하는 기능과 디스크 저장은 아직 별도입니다.
Scene의 **Portal Plaza — dungeon entrance**를 이동하면 포탈 상호작용 위치와 귀환 위치도 따라갑니다.
포탈은 **Assets/Town/Prefabs/PortalPlaza.prefab**으로 재사용할 수 있습니다. Town Portal의 Dungeon Scene으로 입장 대상을 지정합니다.
기존 장면은 Assets/Scenes/Backups/Town_before_portal_*.unity에 보관했습니다.
자동 왕복 검사: 실행 파일에 `-batchmode -nographics --portal-smoke-test -logFile PortalTravel.log` 인자를 전달하세요.
