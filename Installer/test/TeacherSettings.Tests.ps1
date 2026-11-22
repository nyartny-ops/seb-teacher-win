# Проверки логики панели преподавателя на собранной SafeExamBrowser.Settings.dll
param([string]$Dll)
$ErrorActionPreference = 'Stop'
Add-Type -Path $Dll
$fails = 0
function Check($cond, $what) {
  if ($cond) { Write-Host "  OK  $what" } else { Write-Host "  FAIL $what" -ForegroundColor Red; $script:fails++ }
}
$T = [SafeExamBrowser.Settings.Teacher.TeacherSettings]
$path = $T::FilePath
if (Test-Path $path) { Remove-Item $path -Force }

Check ($T::CheckTeacherPassword('32123212')) 'пароль 32123212 принимается'
Check (-not $T::CheckTeacherPassword('12345678')) 'неверный пароль отклоняется'
Check (-not $T::CheckTeacherPassword('')) 'пустой пароль отклоняется'

$s = $T::Load()
Check ($s.MatchingPreset() -eq 'Standard') 'по умолчанию — Стандартный'
$s.ApplyPreset('Strict');  Check ($s.MatchingPreset() -eq 'Strict') 'пресет Строгий'
$s.ApplyPreset('Free');    Check ($s.MatchingPreset() -eq 'Free') 'пресет Мягкий'
$s.Set('print', -not $s.Get('print')); Check ($s.MatchingPreset() -eq 'Custom') 'ручное изменение → Свой'

$s.ApplyPreset('Strict')
$s.StartUrl = "moodle.example.org/quiz?id=5"
$s.QuitPasswordMode = 'Custom'
$s.CustomQuitPasswordHash = $T::Sha256('exit-pass')
$s.RequirePasswordToStart = $true
Check ($s.Save()) 'сохранение'
$r = $T::Load()
Check ($r.MatchingPreset() -eq 'Strict') 'после загрузки — Строгий'
Check ($r.StartUrl -eq "moodle.example.org/quiz?id=5") 'адрес сохранился'
Check ($r.QuitPasswordMode -eq 'Custom' -and $r.RequirePasswordToStart) 'пароль выхода и «пароль для старта» сохранились'

# подделка файла → настройки сбрасываются на «Стандартный»
(Get-Content $path -Raw -Encoding UTF8).Replace('colorGuard=1', 'colorGuard=0') | Set-Content $path -Encoding UTF8 -NoNewline
$x = $T::Load()
Check ($x.MatchingPreset() -eq 'Standard') 'изменённый вручную файл не принимается'

# применение к настройкам SEB
$a = New-Object SafeExamBrowser.Settings.AppSettings
$r.ApplyTo($a)
Check ($a.Security.KioskMode -eq 'CreateNewDesktop') 'Строгий: отдельный рабочий стол'
Check ($a.Security.ClipboardPolicy -eq 'Isolated' -and $a.Browser.UseIsolatedClipboard) 'Строгий: отдельный буфер'
Check ($a.Security.VirtualMachinePolicy -eq 'Deny') 'Строгий: запрет ВМ'
Check (-not $a.Browser.AllowPrint) 'Строгий: печать запрещена'
Check ($a.Browser.StartUrl -eq 'https://moodle.example.org/quiz?id=5') 'адрес дополнен https://'
Check ($a.Security.QuitPasswordHash -eq $T::Sha256('exit-pass')) 'отдельный пароль выхода применён'
Check ($a.Display.AllowedDisplays -eq 1) 'один монитор'

$f = $T::Load(); $f.ApplyPreset('Free'); $f.QuitPasswordMode = 'Teacher'
$b = New-Object SafeExamBrowser.Settings.AppSettings
$b.Applications.Blacklist.Add((New-Object SafeExamBrowser.Settings.Applications.BlacklistApplication))
$f.ApplyTo($b)
Check ($b.Security.KioskMode -eq 'None' -and $b.Keyboard.AllowAltTab) 'Мягкий: другие программы доступны'
Check ($b.Browser.AllowPrint -and $b.Mouse.AllowRightButton) 'Мягкий: печать и правая кнопка'
Check ($b.Security.QuitPasswordHash -eq $T::TeacherPasswordHash) 'пароль выхода = пароль преподавателя'
Check ($b.Applications.Blacklist.Count -eq 1) 'Мягкий: запрещённые программы всё равно закрываются'

Remove-Item $path -Force
if ($fails -gt 0) { Write-Host "$fails проверок не прошли" -ForegroundColor Red; exit 1 }
Write-Host 'Все проверки панели преподавателя прошли'
