# Warlock Tools

**Слава Ардании!**

Сделал **Vlad302002** — истинный Арданской король и покровитель Ардании.  
Made by **Vlad302002** — the true King of Ardania and patron of Ardania.

---

## Русский

### Что это

Одна оболочка (`WarlockTools_GUI.exe`) вокруг тулов Ino-Co / Paradox для *Warlock: Master of the Arcane*:

| Действие | Кто делает |
|----------|------------|
| `.pack` ↔ папка | `Squeezer.exe` (лежит в `not use`) |
| `.binary` ↔ `.xml` | `TXMLConvert.exe` |
| `.xr` ↔ `.xml` | `XRconvert_Final.exe` |
| `locdata.md` ↔ текст | **наш** разбор (`WarlockMd.cs`) |

Сборка: `build.ps1` (csc, .NET 4.x). Exe кладётся на уровень выше, рядом с GUI.

### Особенность `.pack`

Это **не** ZIP и **не** Majesty `.pak`.

- Свой формат Squeezer: в начале `uint64` — число файлов, дальше 24-байтные имена (`texquality.sub`, `locdata.md`, `dlc0.txt`…).
- Шифрования нет. Сжатие есть; уже сжатые `.dds/.ogg/.fsb/…` кладут как есть.
- **Пробелы в пути ломают Squeezer.** Каталог игры называется `Warlock - Master of the Arcane` — поэтому GUI гоняет его **относительными** путями из `not use`, а не полным `E:\Steam\...`.
- Распаковка: папка выхода должна уже существовать.

Состав оригинальных паков:

| Пак | Суть |
|-----|------|
| `d100` / `d110` / `d120` | Текстуры и модели, ~1.3 ГБ на всех |
| `l100` | Локаль: **`locdata.md`**, голос `.fsb`, `final.bin` |
| `s100` | Скрипты `.lua` и данные `.binary` (юниты, спеллы, здания) |
| `m00`–`m04` | Флаги DLC, один крошечный `dlcN.txt` |

### Особенность `locdata.md` (не Majesty)

Магия та же (`mdmp`), дальше **другая игра**.

- Majesty-ридер **не** открывает: другая ёмкость таблиц, другой хеш.
- Пул строк — **UTF-8** (русский читается сразу). В шапке поле `1251` — это метка, не кодировка пула.
- Игра при загрузке проверяет штамп **`0xC4194`** и читает ровно **`0x1D7B80`** байт после магии. Паковать в другой размер нельзя.
- 6398 пар. Block1 на 13313 слотов. Хеш: `signed char`, `h = h*31+c`, корзина `h % 16641`. В корзине максимум **4** слота (у Majesty 7 и 19200 корзин). Лишнее — список overflow на 64.
- Указатель на строку в файле: `StrPtr + 0x34024`. Пул с `0x117B74`, первая строка пустая (`\0`), ключи вида `#SWORD_KNIGHT`.
- Текст: `#КЛЮЧ=значение`, первая строка служебная. `\n` в значении пишем как `\n`.

`locdata.xr` в `not use` — **не** игровая локаль, а подмены картинок мастерской Steam.

---

## English

### What this is

One shell (`WarlockTools_GUI.exe`) around the Ino-Co / Paradox helpers for *Warlock: Master of the Arcane*:

| Action | Engine |
|--------|--------|
| `.pack` ↔ folder | `Squeezer.exe` (lives in `not use`) |
| `.binary` ↔ `.xml` | `TXMLConvert.exe` |
| `.xr` ↔ `.xml` | `XRconvert_Final.exe` |
| `locdata.md` ↔ text | **ours** (`WarlockMd.cs`) |

Build: `build.ps1` (csc, .NET 4.x). The exe is written one level up, next to the GUI.

### What is special about `.pack`

This is **not** ZIP and **not** Majesty `.pak`.

- Custom Squeezer format: a `uint64` file count, then 24-byte names (`texquality.sub`, `locdata.md`, `dlc0.txt`…).
- No encryption. Compression yes; already-compressed `.dds/.ogg/.fsb/…` are stored raw.
- **Spaces in the path break Squeezer.** The game folder is `Warlock - Master of the Arcane`, so the GUI runs it with **relative** paths from `not use`, never a full `E:\Steam\...` string.
- Extract: the output folder must already exist.

Original packs:

| Pack | Contents |
|------|----------|
| `d100` / `d110` / `d120` | Textures and meshes, ~1.3 GB together |
| `l100` | Locale: **`locdata.md`**, voice `.fsb`, `final.bin` |
| `s100` | `.lua` scripts and `.binary` data (units, spells, buildings) |
| `m00`–`m04` | DLC flags, one tiny `dlcN.txt` each |

### What is special about `locdata.md` (not Majesty)

Same magic (`mdmp`), **different game** after that.

- The Majesty MD reader **cannot** open it: different table sizes, different hash.
- The string pool is **UTF-8** (Russian is plain UTF-8). The header field `1251` is a stamp, not the pool encoding.
- On load the exe checks stamp **`0xC4194`** and reads exactly **`0x1D7B80`** bytes after the magic. Do not change the payload size.
- 6398 pairs. Block1 holds 13313 slots. Hash: `signed char`, `h = h*31+c`, bucket `h % 16641`. At most **4** slots per bucket (Majesty uses 7 and 19200 buckets). Spill goes to a 64-slot overflow list.
- On-disk string pointer: `StrPtr + 0x34024`. Pool starts at `0x117B74`, first string is empty (`\0`), keys look like `#SWORD_KNIGHT`.
- Text format: `#KEY=value`, first line is a header. Encode newlines in values as `\n`.

`locdata.xr` in `not use` is **not** game locale — Steam Workshop image substitutions.

---

**Слава Ардании!**
