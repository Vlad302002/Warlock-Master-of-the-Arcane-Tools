# Warlock Tools — pack / xr / binary / locdata.md

**Слава Ардании!**  
Сделал **Vlad302002** — истинный Арданской король и покровитель Ардании.

**Glory to Ardania!**  
Made by **Vlad302002** — the true King of Ardania and patron of Ardania.

---

## Что это / What this is

Удобная программа с графическим интерфейсом — одна оболочка `WarlockTools_GUI.exe` вокруг конвертеров для игр *Warlock: Master of the Arcane* и *Warlock 2: The Exiled* (Paradox Interactive). Никаких `.bat`-скриптов и консоли: просто перетащил файл — действие определилось само.

A friendly GUI — one shell `WarlockTools_GUI.exe` around the converters for *Warlock: Master of the Arcane* and *Warlock 2: The Exiled* (Paradox Interactive). No `.bat` scripts or console: drop a file and the action is detected automatically.

| Действие / Action | Инструмент / Engine |
|-------------------|---------------------|
| `.pack` ↔ папка / folder | `Squeezer.exe` |
| `.binary` ↔ `.xml` | `TXMLConvert.exe` |
| `.xr` ↔ `.xml` | `XRconvert_Final.exe` |
| `locdata.md` ↔ текст / text | собственный разбор / our parser |

Главная фишка: **взломан формат `locdata.md`** (`mdmp`) — файл локализации **Warlock 1 и Warlock 2**. Его можно распаковать в обычный читаемый текст, отредактировать и собрать обратно. Строки лежат в UTF-8. Игра определяется автоматически по штампу в файле.

The key feature: **the `locdata.md` format (`mdmp`) is cracked** — the localization file for **both Warlock 1 and Warlock 2**. Unpack to readable text, edit, pack back. The string pool is UTF-8. The game version is auto-detected from the file stamp.

| Игра / Game | stamp | пар / pairs (типично) |
|-------------|-------|------------------------|
| Warlock 1 | `0xC4194` | ~6398 |
| Warlock 2 | `0x101F70` | ~10499 |

---

## Как пользоваться / How to use

### Русский

1. Положи `WarlockTools_GUI.exe` **рядом** с папкой `not use` (не внутрь неё). Внутри `not use` лежат программы (`Squeezer.exe`, `XRconvert_Final.exe`, `TXMLConvert.exe`), на которые ссылается этот exe, поэтому в эту папку можно даже не заходить — программа найдёт их сама.
2. Запусти `WarlockTools_GUI.exe`.
3. **Перетащи** файл (`.pack`, `.xr`, `.binary`, `.xml`, `.md`, `.txt`) или **папку** в окно — действие подставится автоматически.
4. Проверь поле **Вход** и **Выход** (при необходимости нажми «Обзор…»).
5. Нажми **Выполнить**.

Доступные действия:

- **Распаковать `.pack`** — архив в папку.
- **Упаковать папку → `.pack`** — собрать архив обратно.
- **`.binary` → `.xml`** и **`.xml` → `.binary`** — бинарные данные игры.
- **`.xr` → `.xml`** и **`.xml` → `.xr`** — контейнеры данных.
- **`locdata.md` → текст** и **текст → `locdata.md`** — правка локализации (Warlock 1 и Warlock 2).

Кнопка **«Все паки игры»** распакует `d100 / d110 / …` в папку `_extract\` (~1.3 ГБ).  
Кнопка **«Открыть выход»** откроет папку с результатом.

**Локализация Warlock 2:** распакуй `l100.pack` → внутри `locdata.md` (RU) и/или `locdataen.md` (EN) → **locdata.md → текст**. Формат текста:

```text
00101f70 000004e3
#KEY=значение
```

Первая строка служебная (stamp + codepage) — её лучше не менять. Переносы внутри значений пиши как `\n`. Обратная сборка читает stamp из этой строки и пишет файл в нужном layout (W1 или W2).

### English

1. Put `WarlockTools_GUI.exe` **next to** the `not use` folder (not inside it). Inside `not use` are the programs (`Squeezer.exe`, `XRconvert_Final.exe`, `TXMLConvert.exe`) this exe references, so you don't even need to open that folder — the program finds them automatically.
2. Run `WarlockTools_GUI.exe`.
3. **Drag and drop** a file (`.pack`, `.xr`, `.binary`, `.xml`, `.md`, `.txt`) or a **folder** into the window — the action is set automatically.
4. Check the **Input** and **Output** fields (use “Browse…” if needed).
5. Click **Run**.

Available actions:

- **Unpack `.pack`** — archive to folder.
- **Pack folder → `.pack`** — build the archive back.
- **`.binary` → `.xml`** and **`.xml` → `.binary`** — binary game data.
- **`.xr` → `.xml`** and **`.xml` → `.xr`** — data containers.
- **`locdata.md` → text** and **text → `locdata.md`** — localization editing (Warlock 1 and Warlock 2).

The **“All game packs”** button unpacks `d100 / d110 / …` into `_extract\` (~1.3 GB).  
The **“Open output”** button opens the result folder.

**Warlock 2 localization:** unpack `l100.pack` → inside you get `locdata.md` (RU) and/or `locdataen.md` (EN) → **locdata.md → text**. Text format:

```text
00101f70 000004e4
#KEY=value
```

The first line is a header (stamp + codepage) — leave it alone. Encode newlines in values as `\n`. Packing reads the stamp from that line and writes the correct layout (W1 or W2).

---

## Примечания / Notes

- `Squeezer.exe` не любит пробелы в пути — GUI гоняет его относительными путями, поэтому полный путь вида `E:\Steam\...` не проблема.
- `Squeezer.exe` breaks on spaces in paths — the GUI uses relative paths, so a full `E:\Steam\...` path is not a problem.
- Распаковка `.pack`: папка выхода должна уже существовать (GUI создаёт её сам). / `.pack` extraction: the output folder must exist (the GUI creates it).
- Паки (`.pack`) у W1 и W2 совместимы с одним Squeezer; формат `locdata.md` — **разный**, парсер понимает оба. / Packs work with the same Squeezer for both games; `locdata.md` layouts **differ**, the parser supports both.
- Исходники лежат в папке [`WarlockTools_src`](WarlockTools_src). Сборка: [`build.ps1`](WarlockTools_src/build.ps1) (csc, .NET Framework 4.x). / Sources live in [`WarlockTools_src`](WarlockTools_src). Build: [`build.ps1`](WarlockTools_src/build.ps1) (csc, .NET Framework 4.x).

---

**Слава Ардании!**
