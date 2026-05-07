# План проекта: Slash Arena (рабочее название)

> Этот документ — единый источник правды по проекту. Его читают и человек, и AI-ассистенты (Claude как архитектор, Cursor как редактор скриптов). Любые изменения архитектуры и роадмапа фиксируются здесь.

---

## 1. О проекте

**Жанр:** ареновый слешер от 3-го лица с волнами врагов.
**Движок:** Unity 6000.2.10f1 (URP).
**Ключевые пакеты:** Input System (новый), Cinemachine 3.x, AI Navigation 2.x, TextMesh Pro.
**Стиль:** low-poly (Blink Humans + PurePoly Weapons + позже добавим CC0-ассеты).

**Цели:**
- Полноценный учебный пет-проект, на котором владелец проходит ключевые концепции геймдева в Unity.
- Возможный релиз на itch.io (минимально достаточный) или Steam (если проект «зайдёт» — потребует доп. фазы полировки и юр. подготовки).
- НЕ MMO, НЕ мультиплеер. Однопользовательский слешер с прогрессией внутри забега.

**Стиль кода и архитектуры:**
- Data-driven: всё, что может быть данными, — это ScriptableObject (оружие, скины, архетипы врагов, конфиги).
- Композиция важнее наследования.
- Минимум `FindObjectOfType` в `Update`, кэшировать ссылки в `Awake`/`Start`.
- `IDamageable` уже есть — расширяем интерфейсами по мере роста (`IInteractable`, `IPickup` и т. д.).

---

## 2. Команда и роли

| Кто | Что делает |
|---|---|
| **Владелец (человек)** | Принимает решения, делает работу в Unity Editor (расстановка, Animator, Inspector, Prefabs, NavMesh bake), тестирует, играет |
| **Claude (архитектор)** | Дизайн систем, ScriptableObject-модели, согласование структуры папок и публичного API скриптов, инструкции «куда тыкать в Editor», ревью спорных решений, обучение концепциям |
| **Cursor (in-file editor)** | Быстрые правки внутри уже существующих скриптов: баги, мелкие рефакторинги, добавление методов в рамках согласованного API, форматирование, комментарии |

### Правила взаимодействия с Cursor

Cursor может **сам**:
- править тело методов
- добавлять private-поля и приватные методы
- чинить баги, на которые есть чёткое описание
- рефакторить локально (внутри одного файла)
- добавлять `[SerializeField]` к существующим private-полям

Cursor **должен спросить Claude / владельца** перед:
- созданием новых файлов (особенно ScriptableObject-классов)
- переименованием файлов и публичных классов/методов
- изменением публичного API существующего скрипта (новые public-поля, новые public-методы, изменение сигнатур)
- созданием новых папок или перемещением файлов
- удалением скриптов
- добавлением зависимостей от новых пакетов

### Конвенции, которым следуют ВСЕ (включая Cursor)

1. **Папки:** свой код и ассеты только в `Assets/_Project/`. Папка `Assets/Imported/` — только для покупных/CC0 пакетов, её не трогаем.
2. **Namespace:** пока без namespaces — слишком ранний этап. Введём позже, если разрастётся.
3. **Имена:** PascalCase для классов, методов, properties; camelCase для private-полей; ALL_CAPS для констант.
4. **`[SerializeField] private`** предпочтительнее `public` для полей, которые редактируются в Inspector, но не используются другими скриптами.
5. **`[Header]` и `[Tooltip]`** обязательны на всех видных в Inspector полях, чтобы Editor-работа была понятной.
6. **Логи:** используем `Debug.Log` с префиксом в квадратных скобках (`[Jump]`, `[Spawn]`) — это уже сложилось в проекте, продолжаем.
7. **Без LINQ в `Update`** и горячих циклах (аллокации).
8. **Никаких `GameObject.Find` / `FindObjectOfType` в `Update`** — кэшируем в `Awake`.

---

## 3. Архитектурные решения (decisions log)

### D1: Data-driven через ScriptableObject
Все игровые данные — отдельные `.asset` файлы:
- `WeaponDefinition` (имя, иконка, префаб, стат-блок, override-аниматор, звук)
- `EnemyArchetype` (HP, скорость, броня, оружие, AI-профиль, drop-таблица)
- `SkinDefinition` (имя, превью, материалы, меш override)
- `GameConfig` (глобальные настройки)

### D2: Структура папок (`Assets/_Project/`)
```
Assets/
├── _Project/                  ← всё своё
│   ├── Animations/
│   ├── Art/
│   ├── Audio/
│   ├── Data/                  ← все ScriptableObject .asset
│   │   ├── Weapons/
│   │   ├── Enemies/
│   │   ├── Skins/
│   │   └── Configs/
│   ├── Editor/
│   ├── Input/
│   ├── Prefabs/
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── Anim/
│   │   ├── Combat/
│   │   ├── Data/              ← классы ScriptableObject
│   │   ├── Debug/
│   │   ├── Enemy/
│   │   ├── Interaction/       ← пикапы, взаимодействия (Фаза 2)
│   │   ├── Player/
│   │   ├── UI/
│   │   └── Util/
│   └── Settings/              ← URP, Render, Quality
├── Imported/                  ← НЕ ТРОГАЕМ (Blink, PurePoly, будущие CC0)
├── Samples/                   ← оставляем как есть
└── TextMesh Pro/              ← оставляем
```

### D3: Сейвы — JSON в `Application.persistentDataPath`
Не `PlayerPrefs` (слишком плоско). Один файл `save.json`, обновляется через типизированный `SaveService`. Структура: settings, leaderboard[], lastSelectedSkin, totalRuns.

### D4: Animator на Humanoid
Модели приводим к Humanoid Avatar — открывает доступ к Mixamo и кросс-совместимым анимациям. Решение принято в Фазе 1.

### D5: Лицензии ассетов
Записываем источник и лицензию каждого нового ассета в этот документ (раздел «Библиотека ассетов»). Особенно важно при возможном релизе на Steam.

---

## 4. Дорожная карта

Каждая фаза имеет: **цель → концепции → код-работы → editor-работы → acceptance criteria**. Двигаемся последовательно. После каждой фазы — пауза, ревью, решение «дальше или передохнуть».

### Фаза 0 — Фундамент

**Цель:** базовая инфраструктура для совместной работы и масштабирования.

- Git + `.gitignore` + первый коммит, приватный репозиторий на GitHub.
- Реорганизация `Assets/` под структуру D2 (см. выше).
- Этот файл `PROJECT_PLAN.md` в корне — единый источник правды.
- Пилотный `GameConfig.asset` — первый ScriptableObject в проекте, чтобы пощупать концепцию.

**Концепции:** версионирование, чистая структура проекта, ScriptableObject как контейнер данных.

**Acceptance:** проект открывается, всё на местах, в Git нет `Library/`, `GameConfig.asset` редактируется в Inspector.

### Фаза 1 — Оптимизация анимации главного персонажа

**Цель:** анимации работают плавно, потребляют мало памяти, легко расширяемы.

**Работы:**
- FBX import-настройки (Compression, Anim. Compression Error, Resample, Loop Time).
- Перевод модели на **Humanoid Avatar** (важно: без этого Фаза 4 со скинами с Mixamo не взлетит).
- Замена россыпи стейтов движения на **2D Blend Tree** (Idle/Walk/Run на одном стейте через `MoveX`/`MoveY`/`Speed`).
- **Avatar Mask** для Upper Body Layer (`upperBodyLayerIndex = 1` уже зарезервирован в `ThirdPersonController`) — атаки в движении.
- Чистка дублей логики момента удара: один источник правды (StateMachineBehaviour `AttackStateWindow` или Animation Events — выберем один).

**Концепции:** Humanoid retargeting, Blend Trees, Animator Layers и Masks, Animation Events vs StateMachineBehaviour.

**Acceptance:** персонаж плавно идёт/бежит, бьёт во время бега, прыжок не глючит, в Profiler нет лишних аллокаций от Animator.

### Фаза 1.5 — Расширенный мувмент и боевой комплект

**Цель:** довести управление до уровня экшн-слешера.

**Биндинги (зафиксированы):**
| Действие | Кнопка | Реализация |
|---|---|---|
| Бег во все стороны | WASD | уже есть (Blend Tree) |
| Спринт | Shift | InputAction `Sprint`, `Speed` параметр в Locomotion-tree (0/1/2) |
| Перекат / уклонение | **Alt** | импульс по `desired`-вектору + Roll 2D Blend Tree (`RollForward/Back/Left/Right` из Blink), кулдаун ~0.8 сек |
| Прыжок | **Space** | 3-стейтный: `JumpWhileRunning` (Up) → `FallingLoop` → blend в Locomotion. Apply Root Motion = OFF на Player, важно. Заморожен до Шага 7 |
| Лёгкий удар | LMB | уже есть (`Attack_Punch` / `Attack_OneHandSword` через `WeaponDefinition` после Phase 2) |
| Тяжёлый удар | E | `AttackStrength.Heavy` уже есть в `PlayerAttack`, нужен InputAction + триггер |
| Удар ногой | Q | новый клип (Mixamo — Roundhouse Kick), отдельный hitshape |
| Защита (блок) | RMB (hold) | bool-параметр `isBlocking`, замедляет движение, поглощает % урона; `BlockingLoop.fbx` уже есть в Blink |
| Бросок | G | позже (Phase 2.5+) |
| Магия / способности | 1, 2, 3, 4 | позже (отдельная фаза) |
| Crouch (присесть) | C (или Ctrl alt) | опционально, оставлен в Input Actions |
| Смена оружия | Mouse Scroll / 1-9 | используется в Phase 2.5 (Previous/Next в Input Actions) |

**Acceptance:** на клавиатуре можно сделать всю «золотую петлю» слешера — бег + спринт + уклонение + прыжок + лёгкий/тяжёлый удар + блок. Анимации соответствуют действиям.

### Фаза 2 — Подбор оружия со стойки

**Цель:** игрок может подойти к стойке и взять оружие. Оружие — данные.

**Биндинг подбора:** **F** (Pickup / Drop), как в большинстве слешеров.

**Работы:**
- `WeaponDefinition` (ScriptableObject): id, displayName, icon, hand-prefab, `WeaponConfig` (light/heavy hitshape — `PlayerAttack` уже знает структуру), **`RuntimeAnimatorController` (Animator Override Controller)**, sfx.
  - **Принцип:** базовый `Player_Animator` задаёт структуру (стейты `LightAttack`/`HeavyAttack`/`Kick`/`Block`), а Override Controller под каждое оружие **подменяет клипы**. Без оружия (Fist) → `PunchLeft`/`PunchRight`. Меч → `MeleeAttack_OneHanded`. И т. д.
  - В коде при смене оружия: `animator.runtimeAnimatorController = weaponDefinition.overrideController;`
- `WeaponHolder` на персонаже (точка `RightHandSocket`, привязанная к кости правой руки).
- `WeaponPickup` (компонент на стойке): triggerCollider + ссылка на `WeaponDefinition` + UI-prompt.
- `IInteractable` интерфейс (на будущее: двери, рычаги, сундуки).
- Рефакторинг `PlayerAttack`: статы атаки берутся из текущего `WeaponDefinition`, а не из жёстко прошитых полей.
- **Слоты оружия (упрощённо):** один активный слот в Phase 2. Множественные слоты + переключение по колесу мыши — отложено в **Phase 2.5**, чтобы не растягивать Phase 2.

**Концепции:** ScriptableObject-как-данные, интерфейсы интеракций, Animator Override Controller, костные сокеты.

**Acceptance:** подходишь к стойке → подсказка «F: взять Меч» → нажал F → меч в руке, статы и анимации поменялись; снова F рядом со стойкой → кулаки.

### Фаза 2.5 — Несколько слотов оружия (опционально)

Когда Phase 2 устаканится, расширим: 2–4 слота, переключение колесом мыши или цифрами 5/6, иконки в HUD. Не делаем, пока однослотовая версия не работает идеально.

### Фаза 3 — Категории врагов

**Цель:** разнообразие противников через данные, а не через дублирование скриптов.

**Работы:**
- `EnemyArchetype` (ScriptableObject): hp, speed, armor (% reduction), weapon (`WeaponDefinition?`), aiProfile (Charge / Ranged / Tank / Patrol), bodyMaterial, drop.
- 4–5 архетипов: `Skeleton_Unarmed`, `Skeleton_Sword`, `Skeleton_Heavy_Armor`, `Bandit_Bow`, `MiniBoss`.
- Каждый архетип = **Prefab Variant** базового скелета.
- `EnemyAI`/`EnemyHealth` принимают `EnemyArchetype` и применяют параметры в `Awake`/`Start`.
- `EnemySpawner` → взвешенный пул архетипов + кривая по времени (используем уже существующий `minutesToMaxDifficulty`).

**Концепции:** Prefab Variants, поведенческие профили, weighted random, кривая сложности.

**Acceptance:** на арене смешанные толпы, видна разница визуально и по тактике, прогрессия по времени читается.

### Фаза 4 — Скины и перекраска персонажей

**Цель:** игрок может выбирать визуал; в проект добавлены минимум 2 альтернативных персонажа из бесплатных источников.

**Работы:**
- Перекраска текущей Blink-модели через дублирование palette-текстуры `LowPolyCharacterTexture.png` (это палитровая 256x256, UV «тыкают» в квадратики).
- Дублирование материалов `Body.mat` → `Body_Red.mat`, `Body_Blue.mat` и т. д.
- Импорт CC0/free-персонажей (см. библиотеку ниже) с настройкой Humanoid (требует Фазу 1).
- `SkinDefinition` (ScriptableObject): displayName, previewSprite, characterPrefab или materialOverrides.
- `SkinApplier` на персонаже: на старте сцены применяет выбранный скин из сейва.

**Концепции:** palette texturing, Humanoid retargeting на новые модели, Material Instances, превью через Render Texture.

**Acceptance:** минимум 3 скина в `Data/Skins/`, переключаются и сохраняются.

### Фаза 5 — Главное меню, рейтинг, выбор скина

**Цель:** игра имеет «обёртку», локально хранит результаты и настройки.

**Работы:**
- Сцена `MainMenu.unity`, Build Settings, кнопки Start / Skins / Leaderboard / Settings / Quit.
- `SaveService`: сериализация JSON в `Application.persistentDataPath`.
- `LeaderboardEntry { nickname, score, timeSurvivedSec, dateUtc }`, топ-10, добавление в `GameManager.ShowGameOver()` с полем ввода никнейма.
- Экран Skins: карусель `SkinDefinition`, превью через Render Texture (отдельная мини-сцена `_SkinPreview`).
- Экран Settings: sensitivity, master volume, sfx volume.

**Концепции:** SceneManager, JSON-сериализация (`JsonUtility`), Render Texture, UI-навигация.

**Acceptance:** игра запускается из MainMenu, после смерти спрашивает имя, рекорды сохраняются между запусками, скин применяется и помнится.

### Фаза 6 — Полировка и подготовка к публикации

**Цель:** играбельный билд, который не стыдно показать.

**Работы:**
- Quality Settings, разрешения, иконка, splash.
- Пауза, опции, рестарт без перезагрузки сцены (по возможности).
- Билд под Windows (x64), README.
- Тест на чужой машине.
- Написать страницу itch.io (или Steam page при таком решении).
- Лицензионные кредиты в игре (важно для CC0 даже без обязательства атрибуции — хороший тон).

**Acceptance:** есть собранный `.exe`, играется на чужой машине без Unity, не падает за 10-минутный забег.

### Фаза 7 (опционально) — онлайн-таблица и прочее

Если захочется онлайна: Unity Gaming Services Cloud Save / Firebase Free Tier / PlayFab. Не делаем, пока локальная таблица не работает уверенно.

---

## 5. Библиотека ассетов (актуальный список)

### Уже в проекте

| Пакет | Что | Лицензия | Где |
|---|---|---|---|
| Blink — FREE Human LowPoly + Animations Starter Pack | Персонаж + ~25 анимаций | Asset Store free | `Assets/Imported/Blink/` |
| PurePoly — Free Fantasy RPG Weapons | Оружие low-poly | Asset Store free | `Assets/Imported/PurePoly/` |

### Кандидаты на добавление (бесплатные, легальные)

| Источник | Что есть | Лицензия | Когда подключим |
|---|---|---|---|
| **Mixamo** (mixamo.com, Adobe) | Персонажи + любые анимации, Humanoid-совместимо | Free, разрешено в коммерции | Фазы 1, 4 |
| **KayKit by Kay Lousberg** (kaylousberg.com) | Adventurers, Skeletons, Dungeon, Mini-Game packs — стилизованные паки | CC0 (полная свобода) | Фаза 4 |
| **Quaternius** (quaternius.com) | Огромная библиотека CC0 моделей и аниматов | CC0 | Фазы 4, 3 |
| **Kenney.nl** | Mini-Characters, оружие, иконки UI | CC0 | UI/иконки |
| **Synty Free POLYGON Starter Pack** | Бесплатный пробник Synty | Free, см. EULA | Фаза 4 |

> **Правило:** перед добавлением любого нового ассета в `Imported/` — добавляем строку в эту таблицу с указанием лицензии и ссылки на источник.

---

## 6. Текущий статус

**Фаза 0 — Фундамент: ✅ ЗАКРЫТА (2026-05-04)**

| Шаг | Статус |
|---|---|
| `.gitignore` для Unity | ✅ |
| `PROJECT_PLAN.md` | ✅ |
| Git init + приватный репо на GitHub + push | ✅ |
| Cursor rules (`.cursor/rules/architecture.mdc`, `unity-csharp.mdc`) | ✅ |
| Реорганизация `Assets/_Project/` | ✅ |
| `GameConfig.cs` + `GameConfig.asset` (пилотный SO) | ✅ |

**Следующая фаза:** 1 — Оптимизация анимации главного персонажа.

---

## 7. Журнал решений и заметок

> Сюда пишем короткими записями с датой: что решили и почему. Не удаляем — это память проекта.

- **2026-05-04.** Стартовали проект-план. Цель — itch.io (минимум) или Steam (опционально). Подтверждена дорожная карта из 6 фаз. Согласовано разделение ролей Claude/Cursor.
- **2026-05-04.** Принято: сейвы — JSON, не PlayerPrefs (D3).
- **2026-05-04.** Принято: всё своё в `Assets/_Project/`, Imported не трогаем (D2).
- **2026-05-04.** Принято: data-driven через ScriptableObject — основа архитектуры (D1).
- **2026-05-04.** Создан `.gitignore` (стандартный Unity-набор). Создан `PROJECT_PLAN.md`.
- **2026-05-04.** Git init, приватный репо на GitHub, первый push. Добавлены правила Cursor.
- **2026-05-04.** Реорганизация `Assets/` под `_Project/` через Unity Editor (drag-and-drop с переносом meta-файлов).
- **2026-05-04.** Создан `GameConfig.cs` + `GameConfig.asset` — первый ScriptableObject в проекте. **Фаза 0 закрыта.**
- **2026-05-04.** Старт Фазы 1. Подтверждено: Humanoid Avatar уже стоит, 2D Blend Tree собран на 7 направлений, UpperBody Layer с Avatar Mask `UpperBodyMask.mask` существует, `AttackLayerWeight` (StateMachineBehaviour) автоматически плавно регулирует вес слоя.
- **2026-05-04.** В `ThirdPersonController.cs` добавлен `SetTrigger("Jump")` в `DoJumpIfAllowed`. Теперь анимация прыжка триггерится при физическом прыжке.
- **2026-05-04.** Зафиксирован полный мувмент-комплект: Phase 1.5 («Расширенный мувмент»). Биндинги: WASD/Shift/Ctrl/Space/LMB/E/Q/RMB/F. Слоты оружия — Phase 2.5.
- **2026-05-05.** **Прыжок ЗАМОРОЖЕН.** Базовая механика работает физически, но анимация выглядит криво (вероятно из-за Apply Root Motion / неподходящего клипа в `Jumps.fbx`). В геймплее слешера прыжок не критичен — приоритет на спринт, уклонение, блок и удары. Возвращаемся к нему в Phase 1.5 после остального мувмента или позже. В коде `SetTrigger("Jump")` остаётся — просто не даёт ожидаемого результата визуально.
- **2026-05-05.** Аудит анимаций Blink. Зафиксирована новая архитектура мувмента: единый Locomotion 1D-by-Speed с вложенными 2D Run-tree и Sprint-tree (idle/run/sprint × 6 направлений). Стрейф **убран** из плана как нехарактерный для слешера. Roll/Dodge → **Alt**, прыжок → **Space** (нормальный 3-стейтный, возвращаем в Шаге 7). Шаги Phase 1.5: 3 (UpperBody атаки), 4 (рефакторинг кода), 5 (Locomotion-tree), 6 (Roll), 7 (нормальный прыжок).

---

## 8. Что уже есть в коде (на момент старта плана)

Скрипты в `Assets/Scripts/` (будут перенесены в `Assets/_Project/Scripts/` в Фазе 0):

- **Player:** `ThirdPersonController.cs` (CharacterController + Input System, прыжок с coyote/buffer), `PlayerAttack.cs` (Fist/Sword, Light/Heavy, OverlapSphere), `PlayerHealth.cs`.
- **Enemy:** `EnemyAI.cs` (NavMeshAgent + windup attack), `EnemyAtack.cs` (старый, на удаление/слияние), `EnemyHealth.cs` (с world-space HP-bar), `EnemySpawner.cs` (прогрессия по времени).
- **Combat:** `Hurtbox.cs`, `IDamageable`.
- **Anim:** `AttackLayerWeight.cs`, `AttackStateWindow.cs` (StateMachineBehaviour).
- **GameManager.cs**, `BoundaryZone.cs`, `CursorLocker.cs`, `LookAtCamera.cs`, `RestartButtonScript.cs`, `Debug/DiagnosticsHUD.cs`.
- **Editor:** `SmashArenaProjectValidator.cs` (`Tools ▸ SmashArena ▸ Validate Setup`).

Дублей: `EnemyAtack.cs` (старый) и `EnemyAI.cs` — в Фазе 3 объединим/удалим лишнее.
