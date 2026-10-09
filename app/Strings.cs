using System.Globalization;

namespace CabinPlay;

/// <summary>
/// The few texts the host window shows itself (tray menu, startup error). Everything on
/// the screen is translated in ui/i18n.js; keep the language list the same in both.
/// </summary>
internal static class Strings
{
    private static readonly Dictionary<string, Dictionary<string, string>> Table = new()
    {
        ["en"] = new()
        {
            ["show"] = "Show", ["hide"] = "Hide", ["topmost"] = "Always on top",
            ["center"] = "Move to the middle of the screen", ["exit"] = "Exit",
            ["startfail"] = "CabinPlay could not start:",
        },
        ["tr"] = new()
        {
            ["show"] = "Göster", ["hide"] = "Gizle", ["topmost"] = "Her zaman üstte",
            ["center"] = "Ekranın ortasına al", ["exit"] = "Çıkış",
            ["startfail"] = "CabinPlay başlatılamadı:",
        },
        ["de"] = new()
        {
            ["show"] = "Anzeigen", ["hide"] = "Ausblenden", ["topmost"] = "Immer im Vordergrund",
            ["center"] = "In die Bildschirmmitte verschieben", ["exit"] = "Beenden",
            ["startfail"] = "CabinPlay konnte nicht gestartet werden:",
        },
        ["pl"] = new()
        {
            ["show"] = "Pokaż", ["hide"] = "Ukryj", ["topmost"] = "Zawsze na wierzchu",
            ["center"] = "Przenieś na środek ekranu", ["exit"] = "Zakończ",
            ["startfail"] = "Nie udało się uruchomić CabinPlay:",
        },
        ["fr"] = new()
        {
            ["show"] = "Afficher", ["hide"] = "Masquer", ["topmost"] = "Toujours au premier plan",
            ["center"] = "Placer au centre de l'écran", ["exit"] = "Quitter",
            ["startfail"] = "Impossible de démarrer CabinPlay :",
        },
        ["es"] = new()
        {
            ["show"] = "Mostrar", ["hide"] = "Ocultar", ["topmost"] = "Siempre visible",
            ["center"] = "Mover al centro de la pantalla", ["exit"] = "Salir",
            ["startfail"] = "No se pudo iniciar CabinPlay:",
        },
        ["ru"] = new()
        {
            ["show"] = "Показать", ["hide"] = "Скрыть", ["topmost"] = "Поверх всех окон",
            ["center"] = "Переместить в центр экрана", ["exit"] = "Выход",
            ["startfail"] = "Не удалось запустить CabinPlay:",
        },
    };

    /// <summary>The Windows display language if it is one of ours, otherwise English.</summary>
    public static string SystemLanguage
    {
        get
        {
            string code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return Table.ContainsKey(code) ? code : "en";
        }
    }

    public static string Get(string language, string key) =>
        Table.TryGetValue(language, out var texts) && texts.TryGetValue(key, out string? text) ? text : Table["en"][key];
}
