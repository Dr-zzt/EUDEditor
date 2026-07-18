# EUD Editor 2 SE — Patch Notes

## 0.18.2.0 (zzt fork version)

### More readable project file format (YAML)

Project files (`.e2p` / `.e2s`) are now saved in YAML format. The main goal is readability — in particular, less confusion when merging in Git.

- Data is stored as named fields.
  - dat: stored as `{dat: units, field: Hit Points, id: 15, value: 500}`. The field name comes from the dat definitions, and `value` is the resulting absolute value.
    - To keep storing offsets from the default value as before, add `DatEditStyle: delta` to the `project` section and entries are stored as `delta: 100` instead.
  - dat entries, button sets, requirements, FireGraft and wireframe overrides are annotated with the entry name as a YAML comment (e.g. `# Terran Civilian`), and requirement tables are named (`units`, `upgrades`, `techResearch`, `techUse`, `orders`).
- Git now produces meaningful diffs, so changes are a bit easier to understand and merges are far less painful.
- Escaping of special characters is handled by the YAML library, improving reliability.

**Compatibility**: existing projects open as before and are converted to the new format the next time you save.

### Saving

- The file is now written to a temporary location first and then swapped in, so the project file can no longer be corrupted if the program terminates abnormally during a save.
- The previous version of the file is kept as a **`.bak` backup** on every save.

### Relative paths

- **Input Map / Output Map** are stored as paths relative to the project file whenever they live inside the project folder.
- The project keeps working after you move the folder or open it on another PC (e.g. via OneDrive sync).
- Files on another drive are still stored as absolute paths, as before.

### Compatibility notes

- Older versions of EUD Editor 2 SE **cannot open** projects saved in the new format (an "invalid file" message is shown).
- If you need compatibility with an older version, keep a copy of the original file before saving with this version (the automatically created `.bak` file works too).

---

## 0.18.2.0 (zzt fork version) — 한국어

### 프로젝트 파일 포맷의 가독성 개선 (YAML)

프로젝트 파일(`.e2p` / `.e2s`)을 YAML 포맷으로 저장하도록 변경했습니다. 주 목적은 가독성 개선, 특히 git merge에서의 혼란 줄이기입니다.

- 각종 데이터가 이름 있는 필드로 저장됩니다.
  - dat: `{dat: units, field: Hit Points, id: 15, value: 500}`와 같이 저장됩니다. 필드 이름은 dat 정의에서 가져오고, `value`는 최종 절대값입니다.
    - 기존과 같이 기본값 대비 변화량을 저장하고 싶다면 `project` 섹션에 `DatEditStyle: delta`를 추가하면 됩니다. 이후 `delta: 100`와 같이 저장됩니다.
  - dat 엔트리·버튼셋·요구사항·FireGraft·와이어프레임 항목에 엔트리 이름이 YAML 주석으로 붙고(예: `# Terran Civilian`), 요구사항 테이블도 이름(`units`, `upgrades`, `techResearch`, `techUse`, `orders`)으로 저장됩니다.
- Git에서 의미 있는 diff가 나오므로 변경 사항을 이해하기 조금 더 쉬워졌고 merge시 혼란이 크게 줄었습니다.
- 특수문자 등의 이스케이프 처리를 YAML에서 처리하도록 하여 안정성을 높였습니다.

**호환성**: 기존 프로젝트는 그대로 열리고, 다음 저장 때 새 포맷으로 변환됩니다.

### 저장

- 임시 파일에 먼저 쓴 뒤 교체하도록 변경했습니다. 저장 중 프로그램이 비정상 종료되어도 프로젝트 파일이 오염되지 않습니다.
- 저장할 때마다 직전 버전이 **`.bak` 백업**으로 보존됩니다.

### 상대 경로 지원

- **Input Map / Output Map**이 프로젝트 폴더 안에 있으면 상대 경로로 저장됩니다.
- 폴더를 옮기거나 다른 PC에서 열어도(예: OneDrive 동기화) 경로가 깨지지 않습니다.
- 다른 드라이브에 있는 파일은 기존처럼 절대 경로로 저장됩니다.

### 호환성 안내

- 구버전 EUD Editor 2 SE는 새 포맷으로 저장된 프로젝트를 **열 수 없습니다** ("잘못된 파일" 메시지가 표시됩니다).
- 구버전과의 호환이 필요하면 이 버전으로 저장하기 전에 원본을 따로 보관하세요 (자동 생성되는 `.bak` 파일을 활용해도 됩니다).
