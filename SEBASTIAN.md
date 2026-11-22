# SEBастьян для Windows

Safe Exam Browser 3.10.3 (Windows) + панель преподавателя + ключ доступа SEBастьяна + установщик.
⚠ Бета-версия — используйте на свой страх и риск.

## Что добавлено

| Где | Что |
|---|---|
| `SafeExamBrowser.Settings/Teacher/TeacherSettings.cs` | Переключатели, уровни (Мягкий/Стандартный/Строгий/Свой), пароль `32123212`, хранение с подписью, применение к настройкам SEB |
| `SafeExamBrowser.Runtime/Teacher/TeacherPanelWindow.cs` | Окно панели преподавателя |
| `SafeExamBrowser.Runtime/Teacher/AccessKeyGate.cs` | Ключ доступа: проверка на сервере `seb.korova.pro`, окно ввода |
| `SafeExamBrowser.Runtime/Teacher/SebUi.cs` | Фирменный стиль окон (шрифты Nihonium113 и Fira Mono — в `Teacher/Fonts`) |
| `SafeExamBrowser.Runtime/Operations/Session/TeacherPanelOperation.cs` | Шаг запуска: ключ → панель → наложение настроек |
| `SafeExamBrowser.Client/Responsibilities/TeacherColorGuardResponsibility.cs` | «Запретить смену цветов экрана» |
| `Installer/SEBastian.iss` | Установщик Inno Setup (русский, ставит VC++ и службу, убирает официальный SEB) |
| `Installer/test/` | Автотесты: логика панели на собранной DLL + запуск SEB против тестового сервера |
| `.github/workflows/build-windows.yml` | Сборка, тесты, установщик, релиз |

Настройки преподавателя хранятся в `%LOCALAPPDATA%\SEBastian\TeacherSettings.txt` (для текущего пользователя Windows).

## Сборка

GitHub → Actions → «Build SEBастьян (Windows)» → Run workflow (или просто push в `main`).
Результат: `SEBastian-win-setup.exe` в Artifacts и в Releases. Скриншоты автотеста — артефакт `test-results`.

## Смена пароля преподавателя

В `TeacherSettings.cs` замените `TeacherPasswordHash` на SHA-256 нового пароля (тот же, что в macOS-версии).
