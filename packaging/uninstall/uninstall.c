/* Uninstall.exe: starts the uninstaller that CabinPlay's setup registered with Windows.
 *
 * The setup (Inno Setup) names its own uninstaller unins000.exe and keeps it in the app
 * folder; this gives people a file with an obvious name, next to the app and on the
 * release page, that does the same as Settings > Apps > CabinPlay > Uninstall. */
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <wchar.h>

/* AppId in packaging/setup.iss, plus the "_is1" Inno Setup appends. */
#define UNINSTALL_KEY L"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{EA7CAAE8-A55D-479D-86EB-8C4F281D283D}_is1"

static const wchar_t *not_installed(void)
{
    switch (PRIMARYLANGID(GetUserDefaultUILanguage())) {
    case LANG_TURKISH:
        return L"CabinPlay bu bilgisayarda kurulum programıyla kurulmamış.\n\n"
               L"Dosyaları elle kopyaladıysan onları silmen yeterli.";
    case LANG_GERMAN:
        return L"CabinPlay wurde auf diesem PC nicht mit dem Setup installiert.\n\n"
               L"Hast du die Dateien von Hand kopiert, lösche sie einfach wieder.";
    case LANG_POLISH:
        return L"CabinPlay nie został zainstalowany na tym komputerze za pomocą instalatora.\n\n"
               L"Jeśli pliki skopiowano ręcznie, wystarczy je usunąć.";
    case LANG_FRENCH:
        return L"CabinPlay n'a pas été installé sur ce PC avec le programme d'installation.\n\n"
               L"Si vous avez copié les fichiers à la main, il suffit de les supprimer.";
    case LANG_SPANISH:
        return L"CabinPlay no se instaló en este PC con el instalador.\n\n"
               L"Si copiaste los archivos a mano, basta con borrarlos.";
    case LANG_RUSSIAN:
        return L"CabinPlay не был установлен на этом компьютере через программу установки.\n\n"
               L"Если вы копировали файлы вручную, просто удалите их.";
    default:
        return L"CabinPlay was not installed on this PC with its setup.\n\n"
               L"If you copied the files by hand, just delete them.";
    }
}

/* The registered command, e.g. "C:\...\CabinPlay\unins000.exe" with the quotes. */
static int registered_command(wchar_t *command, DWORD size)
{
    static const HKEY roots[] = {HKEY_CURRENT_USER, HKEY_LOCAL_MACHINE};
    for (int i = 0; i < 2; i++) {
        DWORD bytes = size * sizeof(wchar_t);
        if (RegGetValueW(roots[i], UNINSTALL_KEY, L"UninstallString", RRF_RT_REG_SZ, NULL, command, &bytes) ==
                ERROR_SUCCESS &&
            command[0])
            return 1;
    }
    return 0;
}

/* The program part of the command, without quotes, to check that it still exists. */
static int program_exists(const wchar_t *command)
{
    wchar_t path[MAX_PATH];
    const wchar_t *start = command, *end;
    if (*start == L'"') {
        start++;
        end = wcschr(start, L'"');
    } else {
        end = wcschr(start, L' ');
    }
    size_t len = end ? (size_t)(end - start) : wcslen(start);
    if (len == 0 || len >= MAX_PATH)
        return 0;
    wmemcpy(path, start, len);
    path[len] = 0;
    return GetFileAttributesW(path) != INVALID_FILE_ATTRIBUTES;
}

int WINAPI wWinMain(HINSTANCE instance, HINSTANCE previous, PWSTR args, int show)
{
    wchar_t command[2 * MAX_PATH];
    if (!registered_command(command, sizeof(command) / sizeof(command[0])) || !program_exists(command)) {
        MessageBoxW(NULL, not_installed(), L"CabinPlay", MB_OK | MB_ICONINFORMATION);
        return 1;
    }

    STARTUPINFOW si;
    PROCESS_INFORMATION pi;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    if (!CreateProcessW(NULL, command, NULL, NULL, FALSE, 0, NULL, NULL, &si, &pi)) {
        MessageBoxW(NULL, not_installed(), L"CabinPlay", MB_OK | MB_ICONINFORMATION);
        return 1;
    }
    /* Return at once: the uninstaller removes the folder this file is in. */
    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);
    return 0;
}
