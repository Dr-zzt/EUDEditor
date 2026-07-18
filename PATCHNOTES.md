# EUD Editor 2 SE — Patch Notes

## 0.18.2.0 (Unreleased)

### ✨ New project file format (YAML)

Project files (`.e2p` / `.e2s`) are now saved in a clean, human-readable YAML format.

**What you get:**

- **Readable & editable** — open your project file in any text editor and actually understand it. Button sets, requirements, and triggers are stored as named, structured fields instead of comma blobs.
- **Semantic DatEdit entries** — dat edits are stored as `{dat: units, field: Hit Points, id: 15, value: 500}` instead of an uninterpretable number tuple: the field name comes from the dat definitions and `value` is the resulting absolute value. Prefer offsets from the original value? Add `DatEditStyle: delta` to the `project` section and entries are stored as `delta: 100` instead.
- **Name annotations** — dat entries, button sets, requirements, FireGraft and wireframe overrides are annotated with the entry name as a YAML comment (e.g. `# Terran Civilian`), and requirement tables are named (`units`, `upgrades`, `techResearch`, `techUse`, `orders`).
- **Version-control friendly** — project files now produce meaningful diffs in Git, so you can track and review changes to your map project.
- **More robust** — values are properly quoted and escaped by the YAML library. Special characters and multi-line trigger code can no longer silently corrupt a save file.

**Migration is automatic.** Your existing projects open exactly as before; the file is converted the next time you save. No action needed.

### 🛟 Safer saving

- Saves are now **atomic**: the file is written to a temporary location first, then swapped in. A crash or power loss during save can no longer destroy your project.
- The previous version of your project file is kept next to it as a **`.bak` backup** on every save.

### 📁 Relative paths

- **Input Map** and **Output Map** are stored as paths relative to the project file whenever they live inside the project folder.
- Your project keeps working after you move the folder, rename a parent directory, or sync it to another PC (e.g. via OneDrive).
- Absolute paths are still used automatically when a file lives on another drive.

### ⚠️ Compatibility notes

- Older versions of EUD Editor 2 SE **cannot open** projects saved in the new format (they will show an "invalid file" message).
- To keep a project compatible with an older version, keep a copy of the original file before saving with this version (or use the automatically created `.bak` file).

---

## 0.18.2.0 (미공개) — 한국어

### ✨ 새 프로젝트 파일 포맷 (YAML)

프로젝트 파일(`.e2p` / `.e2s`)이 사람이 읽을 수 있는 깔끔한 YAML 포맷으로 저장됩니다.

- **읽고 편집할 수 있는 파일** — 텍스트 에디터로 열어도 구조를 알아볼 수 있습니다. 버튼셋·요구사항·트리거가 쉼표 덩어리 대신 이름 있는 필드로 저장됩니다.
- **의미 있는 DatEdit 저장** — dat 수정 내역이 해석 불가능한 숫자 튜플 대신 `{dat: units, field: Hit Points, id: 15, value: 500}`처럼 저장됩니다. 필드 이름은 dat 정의에서 가져오고, `value`는 최종 절대값입니다. 원본 대비 증분으로 저장하고 싶다면 `project` 섹션에 `DatEditStyle: delta`를 추가하면 `delta: 100` 형태로 저장됩니다.
- **이름 주석** — dat 엔트리·버튼셋·요구사항·FireGraft·와이어프레임 항목에 엔트리 이름이 YAML 주석으로 붙고(예: `# Terran Civilian`), 요구사항 테이블도 이름(`units`, `upgrades`, `techResearch`, `techUse`, `orders`)으로 저장됩니다.
- **버전 관리 친화적** — Git에서 의미 있는 diff가 나오므로 맵 프로젝트의 변경 내역을 추적할 수 있습니다.
- **더 튼튼한 저장** — 값의 인용과 이스케이프를 YAML 라이브러리가 처리하므로, 특수 문자나 여러 줄 트리거 코드 때문에 세이브 파일이 조용히 깨지는 일이 사라졌습니다.

**전환은 자동입니다.** 기존 프로젝트는 그대로 열리고, 다음 저장 때 새 포맷으로 변환됩니다.

### 🛟 더 안전한 저장

- **원자적 저장**: 임시 파일에 먼저 쓴 뒤 교체하므로, 저장 중 크래시나 정전이 나도 프로젝트가 파괴되지 않습니다.
- 저장할 때마다 직전 버전이 **`.bak` 백업**으로 보존됩니다.

### 📁 상대 경로 지원

- **Input Map / Output Map**이 프로젝트 폴더 안에 있으면 상대 경로로 저장됩니다.
- 폴더를 옮기거나 다른 PC에서 열어도(예: OneDrive 동기화) 경로가 깨지지 않습니다.
- 다른 드라이브에 있는 파일은 기존처럼 절대 경로로 저장됩니다.

### ⚠️ 호환성 안내

- 구버전 EUD Editor 2 SE는 새 포맷으로 저장된 프로젝트를 **열 수 없습니다** ("잘못된 파일" 메시지가 표시됩니다).
- 구버전과의 호환이 필요하면 이 버전으로 저장하기 전에 원본을 따로 보관하세요 (자동 생성되는 `.bak` 파일을 활용해도 됩니다).
