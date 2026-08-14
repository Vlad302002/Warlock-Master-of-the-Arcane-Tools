using System;
using System.Collections.Generic;

namespace WarlockTools
{
    enum UiLanguage
    {
        Ru,
        En
    }

    static class UiLang
    {
        public static UiLanguage Current = UiLanguage.Ru;

        static readonly Dictionary<string, string[]> Map = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "WinTitle", new[] { "Warlock Tools — pack / xr / binary", "Warlock Tools — pack / xr / binary" } },
            { "Title", new[] { "Warlock Tools", "Warlock Tools" } },
            { "Subtitle", new[] {
                "Слава Ардании!  ·  Vlad302002",
                "Слава Ардании!  ·  Vlad302002" } },
            { "DropHint", new[] {
                "Перетащите .pack / .xr / .binary / .xml / папку  ·  или нажмите",
                "Drop a .pack / .xr / .binary / .xml / folder  ·  or click" } },
            { "Input", new[] { "Вход", "Input" } },
            { "Output", new[] { "Выход", "Output" } },
            { "Browse", new[] { "Обзор…", "Browse…" } },
            { "Action", new[] { "Действие", "Action" } },
            { "Run", new[] { "Выполнить", "Run" } },
            { "OpenOut", new[] { "Открыть выход", "Open output" } },
            { "UnpackAll", new[] { "Все паки игры", "All game packs" } },
            { "ActUnpack", new[] { "Распаковать .pack", "Unpack .pack" } },
            { "ActPack", new[] { "Упаковать папку → .pack", "Pack folder → .pack" } },
            { "ActBin2Xml", new[] { ".binary → .xml", ".binary → .xml" } },
            { "ActXml2Bin", new[] { ".xml → .binary", ".xml → .binary" } },
            { "ActXr2Xml", new[] { ".xr → .xml", ".xr → .xml" } },
            { "ActXml2Xr", new[] { ".xml → .xr", ".xml → .xr" } },
            { "ActMd2Txt", new[] { "locdata.md → текст", "locdata.md → text" } },
            { "ActTxt2Md", new[] { "текст → locdata.md", "text → locdata.md" } },
            { "Ready1", new[] {
                "Готово. Киньте файл — действие угадается само.",
                "Ready. Drop a file — the action is detected automatically." } },
            { "Ready2", new[] {
                "Большие d100/d110 лучше через «Все паки» или по одному: это ~1.3 ГБ.",
                "Large d100/d110: use “All game packs” or one by one (~1.3 GB)." } },
            { "Ready3", new[] {
                "Squeezer не любит пробелы в пути — GUI гоняет его через относительные пути.",
                "Squeezer breaks on spaces in paths — the GUI uses relative paths." } },
            { "LangChanged", new[] { "Язык: русский", "Language: English" } },
            { "PickAny", new[] { "Выберите файл", "Select a file" } },
            { "FilterAny", new[] {
                "Warlock|*.pack;*.xr;*.binary;*.xml;*.md;*.txt|Все файлы (*.*)|*.*",
                "Warlock|*.pack;*.xr;*.binary;*.xml;*.md;*.txt|All files (*.*)|*.*" } },
            { "PickFolder", new[] { "Папка для упаковки", "Folder to pack" } },
            { "SaveAs", new[] { "Куда сохранить", "Save as" } },
            { "Busy", new[] { "Уже выполняется…", "Already running…" } },
            { "NeedInput", new[] { "Укажите вход.", "Specify an input." } },
            { "NeedOutput", new[] { "Укажите выход.", "Specify an output." } },
            { "InputMissing", new[] { "Вход не найден.", "Input not found." } },
            { "ToolsMissing", new[] {
                "Не найдены Squeezer / XRconvert / TXMLConvert.\nОни должны лежать в папке «not use» рядом с этой программой.",
                "Squeezer / XRconvert / TXMLConvert not found.\nThey should be in the “not use” folder next to this program." } },
            { "NoOutPath", new[] { "Нет выходного пути.", "No output path." } },
            { "OutMissing", new[] { "Папка выхода не существует.", "Output folder does not exist." } },
            { "Cancelled", new[] { "Отменено.", "Cancelled." } },
            { "ConfirmAll", new[] {
                "Распаковать все .pack из папки игры?\n{0}\n\nВыход: {1}\nЭто может занять несколько минут (~1.3 ГБ).",
                "Unpack every .pack from the game folder?\n{0}\n\nOutput: {1}\nThis may take several minutes (~1.3 GB)." } },
            { "Confirm", new[] { "Подтверждение", "Confirm" } },
            { "Overwrite", new[] {
                "Уже существует:\n{0}\n\nПерезаписать?",
                "Already exists:\n{0}\n\nOverwrite?" } },
            { "Guessed", new[] { "Угадано: {0}", "Detected: {0}" } },
            { "Work", new[] { "→ {0}", "→ {0}" } },
            { "Ok", new[] { "Готово ✓  {0}", "Done ✓  {0}" } },
            { "Fail", new[] { "Ошибка: {0}", "Error: {0}" } },
            { "ExitCode", new[] { "код {0}", "exit {0}" } },
            { "NoPacks", new[] { "В папке игры нет .pack", "No .pack files in the game folder" } },
            { "GameRoot", new[] { "Игра: {0}", "Game: {0}" } },
            { "Help", new[] { "Справка", "Help" } },
            { "HelpText", new[] {
                "Warlock Tools — одна оболочка. Сами конвертеры не трогаем.\n\n" +
                "• .pack → папка     (Squeezer)\n" +
                "• папка → .pack     (Squeezer, сжатие 5)\n" +
                "• .binary ↔ .xml    (TXMLConvert)\n" +
                "• .xr ↔ .xml        (XRconvert)\n" +
                "• locdata.md ↔ txt  (наш разбор, UTF-8)\n\n" +
                "Кинь файл сюда или нажми «Обзор». Действие угадается само.\n" +
                "«Все паки игры» распакует d100 / d110 / … в _extract\\ (~1.3 ГБ).\n\n" +
                "Squeezer ломается на пробелах в пути — GUI гоняет его\n" +
                "относительными путями из папки «not use».\n\n" +
                "Батники (Decompile / Compile / xr2xml …) больше не нужны.\n\n" +
                "Сделал Vlad302002 — истинный Арданской король и покровитель Ардании.\n" +
                "Слава Ардании!",
                "Warlock Tools — one shell. The converters themselves are unchanged.\n\n" +
                "• .pack → folder    (Squeezer)\n" +
                "• folder → .pack    (Squeezer, compression 5)\n" +
                "• .binary ↔ .xml    (TXMLConvert)\n" +
                "• .xr ↔ .xml        (XRconvert)\n" +
                "• locdata.md ↔ txt  (our parser, UTF-8)\n\n" +
                "Drop a file here or click Browse. The action is detected.\n" +
                "“All game packs” unpacks d100 / d110 / … into _extract\\ (~1.3 GB).\n\n" +
                "Squeezer breaks on spaces in paths — the GUI runs it\n" +
                "with relative paths from the “not use” folder.\n\n" +
                "The old .bat wrappers are no longer needed.\n\n" +
                "Made by Vlad302002 — the true King of Ardania and patron of Ardania.\n" +
                "Слава Ардании!" } },
            { "Batch", new[] { "Пачка: {0} файлов", "Batch: {0} files" } },
        };

        public static string T(string key)
        {
            string[] row;
            if (!Map.TryGetValue(key, out row))
                return key;
            int i = (int)Current;
            if (i < 0 || i >= row.Length)
                i = 0;
            return row[i];
        }

        public static string Tf(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }
    }
}
