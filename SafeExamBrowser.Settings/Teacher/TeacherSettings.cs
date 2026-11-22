/*
 * SEBастьян — панель преподавателя для Safe Exam Browser (Windows).
 * Порт панели из seb-teacher (macOS): те же уровни контроля, пароль и смысл переключателей,
 * но применяются они к настройкам Windows-версии SEB.
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using SafeExamBrowser.Settings.Browser;
using SafeExamBrowser.Settings.Security;

namespace SafeExamBrowser.Settings.Teacher
{
	public enum TeacherPreset
	{
		Free = 0,
		Standard = 1,
		Strict = 2,
		Custom = 3
	}

	public enum TeacherQuitPasswordMode
	{
		None = 0,
		Teacher = 1,
		Custom = 2
	}

	public class TeacherOption
	{
		public string Id { get; private set; }
		public int Section { get; private set; }
		public string Title { get; private set; }
		public string Hint { get; private set; }
		public bool Free { get; private set; }
		public bool Standard { get; private set; }
		public bool Strict { get; private set; }

		public TeacherOption(string id, int section, string title, string hint, bool free, bool standard, bool strict)
		{
			Id = id;
			Section = section;
			Title = title;
			Hint = hint;
			Free = free;
			Standard = standard;
			Strict = strict;
		}

		public bool ValueFor(TeacherPreset preset)
		{
			switch (preset)
			{
				case TeacherPreset.Free:
					return Free;
				case TeacherPreset.Strict:
					return Strict;
				default:
					return Standard;
			}
		}
	}

	public class TeacherSettings
	{
		/// <summary>
		/// SHA-256 пароля преподавателя (32123212). Чтобы сменить пароль, посчитайте SHA-256 нового пароля
		/// (например, в PowerShell: (Get-FileHash -Algorithm SHA256 -InputStream ([IO.MemoryStream]::new([Text.Encoding]::UTF8.GetBytes('НОВЫЙ')))).Hash.ToLower())
		/// и замените строку ниже. Пароль должен совпадать с macOS-версией.
		/// </summary>
		public const string TeacherPasswordHash = "cc889720062d23085370e56d1c66b16cdddd0be0e80f67377f18d06b43479767";

		public static readonly string[] SectionTitles = { "Экран и окна", "Ограничения системы", "Дополнения для студентов", "Выход и служебное" };

		public static readonly IList<TeacherOption> Options = new List<TeacherOption>
		{
			//                                                                                                         мягкий стандарт строгий
			new TeacherOption("fullscreen", 0, "Полноэкранный режим", "Окно контрольной занимает весь экран, без рамки и кнопок окна.", false, true, true),
			new TeacherOption("singleDisplay", 0, "Только один монитор", "Если подключён второй монитор или проектор, контрольная не начнётся.", false, true, true),
			new TeacherOption("newWindows", 0, "Разрешить ссылкам открывать новые окна", "Если выключено — ссылки открываются в том же окне.", true, true, false),
			new TeacherOption("showTaskBar", 0, "Показывать нижнюю панель SEB", "Часы, звук, сеть и кнопка выхода внизу экрана.", true, true, true),
			new TeacherOption("showToolbar", 0, "Панель кнопок над страницей", "Кнопки «Назад», «Вперёд», «Обновить» над страницей (без адресной строки).", true, false, false),
			new TeacherOption("showWiFi", 0, "Сеть (Wi-Fi) на нижней панели", "Показ и выбор сети Wi-Fi.", true, false, false),

			new TeacherOption("blockAppSwitching", 1, "Запретить другие программы", "SEB работает на отдельном рабочем столе, Alt+Tab, Win и Ctrl+Esc отключены.", false, true, true),
			new TeacherOption("blockSystemMenu", 1, "Заблокировать меню Ctrl+Alt+Del", "Диспетчер задач, смена пользователя, выход и блокировка недоступны. Нужна служба SEB.", false, true, true),
			new TeacherOption("blockScreenCapture", 1, "Запретить скриншоты и запись экрана", "Окно SEB не попадает на снимки и в запись, PrintScreen отключён.", false, true, true),
			new TeacherOption("blockRemote", 1, "Запретить удалённый доступ", "SEB не запустится в удалённом сеансе (RDP), удалённые подключения отключаются.", true, true, true),
			new TeacherOption("closeForbiddenApps", 1, "Закрывать запрещённые программы", "Discord, Teams, Zoom, OBS, TeamViewer, AnyDesk и т. п. будут закрыты на время контрольной.", true, true, true),
			new TeacherOption("blockVM", 1, "Запретить запуск в виртуальной машине", "SEB не запустится внутри VirtualBox, VMware, Hyper-V и т. п.", false, true, true),
			new TeacherOption("privateClipboard", 1, "Отдельный буфер обмена", "Нельзя вставить текст, скопированный до контрольной, и вынести текст из SEB.", false, true, true),
			new TeacherOption("colorGuard", 1, "Запретить смену цветов экрана", "Высокий контраст, цветофильтры, инверсия, тёмная тема. При изменении экран блокируется до ввода пароля.", false, false, true),

			new TeacherOption("spellCheck", 2, "Проверка орфографии", "Подчёркивание ошибок в полях ввода.", true, false, false),
			new TeacherOption("print", 2, "Печать", "Ctrl+P и кнопки «Печать» на сайте.", true, false, false),
			new TeacherOption("find", 2, "Поиск по странице (Ctrl+F)", "Поиск текста на странице.", true, true, false),
			new TeacherOption("zoom", 2, "Масштабирование страницы", "Увеличение и уменьшение (Ctrl+ / Ctrl−).", true, true, true),
			new TeacherOption("downUploads", 2, "Загрузка и скачивание файлов", "Прикрепление файлов к ответу и скачивание материалов.", true, true, false),
			new TeacherOption("rightMouse", 2, "Правая кнопка мыши", "Контекстное меню на странице.", true, false, false),
			new TeacherOption("backForward", 2, "Переход «Назад» и «Вперёд»", "Переход по истории страниц.", true, false, false),
			new TeacherOption("reload", 2, "Перезагрузка страницы", "F5 и кнопка «Обновить».", true, true, true),
			new TeacherOption("showTime", 2, "Часы на панели SEB", "Показывать текущее время.", true, true, true),
			new TeacherOption("devConsole", 2, "Консоль разработчика", "Только для отладки, студентам не включать.", false, false, false),

			new TeacherOption("allowQuit", 3, "Разрешить выход из SEB", "Кнопка выхода на панели. Пароль выхода задаётся ниже.", true, true, true),
			new TeacherOption("allowReconfiguration", 3, "Разрешить открывать другие .seb-файлы", "Студент сможет открыть ссылку seb:// или .seb-файл во время работы. Обычно не нужно.", false, false, false),
		};

		private const string StartUrlKey = "startURL";
		private const string QuitPasswordModeKey = "quitPasswordMode";
		private const string CustomQuitPasswordHashKey = "hashedCustomQuitPassword";
		private const string RequirePasswordToStartKey = "requirePasswordToStart";

		private readonly Dictionary<string, bool> values = new Dictionary<string, bool>();

		public string StartUrl { get; set; }
		public TeacherQuitPasswordMode QuitPasswordMode { get; set; }
		public string CustomQuitPasswordHash { get; set; }
		public bool RequirePasswordToStart { get; set; }

		public static string DirectoryPath
		{
			get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SEBastian"); }
		}

		public static string FilePath
		{
			get { return Path.Combine(DirectoryPath, "TeacherSettings.txt"); }
		}

		public TeacherSettings()
		{
			ResetToDefaults();
		}

		#region Пароль

		public static string Sha256(string text)
		{
			using (var algorithm = SHA256.Create())
			{
				var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes((text ?? string.Empty).Normalize(NormalizationForm.FormC)));
				var builder = new StringBuilder(hash.Length * 2);

				foreach (var b in hash)
				{
					builder.Append(b.ToString("x2"));
				}

				return builder.ToString();
			}
		}

		public static bool CheckTeacherPassword(string password)
		{
			return !string.IsNullOrEmpty(password) && string.Equals(Sha256(password), TeacherPasswordHash, StringComparison.OrdinalIgnoreCase);
		}

		#endregion

		#region Значения

		public bool Get(string id)
		{
			bool value;

			if (values.TryGetValue(id, out value))
			{
				return value;
			}

			foreach (var option in Options)
			{
				if (option.Id == id)
				{
					return option.Standard;
				}
			}

			return false;
		}

		public void Set(string id, bool value)
		{
			values[id] = value;
		}

		public void ApplyPreset(TeacherPreset preset)
		{
			if (preset == TeacherPreset.Custom)
			{
				return;
			}

			foreach (var option in Options)
			{
				values[option.Id] = option.ValueFor(preset);
			}
		}

		public TeacherPreset MatchingPreset()
		{
			foreach (var preset in new[] { TeacherPreset.Free, TeacherPreset.Standard, TeacherPreset.Strict })
			{
				var matches = true;

				foreach (var option in Options)
				{
					if (Get(option.Id) != option.ValueFor(preset))
					{
						matches = false;
						break;
					}
				}

				if (matches)
				{
					return preset;
				}
			}

			return TeacherPreset.Custom;
		}

		public static string TitleFor(TeacherPreset preset)
		{
			switch (preset)
			{
				case TeacherPreset.Free:
					return "Мягкий";
				case TeacherPreset.Standard:
					return "Стандартный";
				case TeacherPreset.Strict:
					return "Строгий";
				default:
					return "Свой";
			}
		}

		public void ResetToDefaults()
		{
			values.Clear();
			ApplyPreset(TeacherPreset.Standard);
			StartUrl = string.Empty;
			QuitPasswordMode = TeacherQuitPasswordMode.Teacher;
			CustomQuitPasswordHash = string.Empty;
			RequirePasswordToStart = false;
		}

		public bool ColorGuardEnabled
		{
			get { return Get("colorGuard"); }
		}

		#endregion

		#region Хранение (с подписью HMAC, чтобы файл нельзя было тихо поправить руками)

		private SortedDictionary<string, string> ToDictionary()
		{
			var data = new SortedDictionary<string, string>(StringComparer.Ordinal);

			foreach (var option in Options)
			{
				data[option.Id] = Get(option.Id) ? "1" : "0";
			}

			data[StartUrlKey] = Escape(StartUrl ?? string.Empty);
			data[QuitPasswordModeKey] = ((int) QuitPasswordMode).ToString();
			data[CustomQuitPasswordHashKey] = CustomQuitPasswordHash ?? string.Empty;
			data[RequirePasswordToStartKey] = RequirePasswordToStart ? "1" : "0";

			return data;
		}

		private static string Escape(string value)
		{
			return value.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n");
		}

		private static string Unescape(string value)
		{
			var builder = new StringBuilder();

			for (var i = 0; i < value.Length; i++)
			{
				if (value[i] == '\\' && i + 1 < value.Length)
				{
					var next = value[++i];
					builder.Append(next == 'n' ? '\n' : next == 'r' ? '\r' : next);
				}
				else
				{
					builder.Append(value[i]);
				}
			}

			return builder.ToString();
		}

		private static string Canonical(SortedDictionary<string, string> data)
		{
			var builder = new StringBuilder();

			foreach (var pair in data)
			{
				builder.Append(pair.Key).Append('=').Append(pair.Value).Append('\n');
			}

			return builder.ToString();
		}

		private static string Mac(string canonical)
		{
			using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("SEBTeacherPanel-v1:" + TeacherPasswordHash)))
			{
				var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
				var builder = new StringBuilder(hash.Length * 2);

				foreach (var b in hash)
				{
					builder.Append(b.ToString("x2"));
				}

				return builder.ToString();
			}
		}

		public bool Save()
		{
			try
			{
				var data = ToDictionary();
				var canonical = Canonical(data);
				var content = "# SEBастьян — настройки панели преподавателя. Не редактируйте вручную.\n" + canonical + "mac=" + Mac(canonical) + "\n";

				Directory.CreateDirectory(DirectoryPath);

				var temp = FilePath + ".tmp";
				File.WriteAllText(temp, content, new UTF8Encoding(false));

				if (File.Exists(FilePath))
				{
					File.Delete(FilePath);
				}

				File.Move(temp, FilePath);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static TeacherSettings Load()
		{
			var settings = new TeacherSettings();

			try
			{
				if (!File.Exists(FilePath))
				{
					return settings;
				}

				var data = new SortedDictionary<string, string>(StringComparer.Ordinal);
				var mac = string.Empty;

				foreach (var rawLine in File.ReadAllLines(FilePath, Encoding.UTF8))
				{
					var line = rawLine.TrimEnd('\r');
					var index = line.IndexOf('=');

					if (line.StartsWith("#") || index <= 0)
					{
						continue;
					}

					var key = line.Substring(0, index);
					var value = line.Substring(index + 1);

					if (key == "mac")
					{
						mac = value;
					}
					else
					{
						data[key] = value;
					}
				}

				if (!string.Equals(Mac(Canonical(data)), mac, StringComparison.OrdinalIgnoreCase))
				{
					// подпись не сошлась — используем «Стандартный»
					return settings;
				}

				foreach (var option in Options)
				{
					string value;

					if (data.TryGetValue(option.Id, out value))
					{
						settings.Set(option.Id, value == "1");
					}
				}

				string text;

				if (data.TryGetValue(StartUrlKey, out text))
				{
					settings.StartUrl = Unescape(text);
				}

				int mode;

				if (data.TryGetValue(QuitPasswordModeKey, out text) && int.TryParse(text, out mode) && mode >= 0 && mode <= 2)
				{
					settings.QuitPasswordMode = (TeacherQuitPasswordMode) mode;
				}

				if (data.TryGetValue(CustomQuitPasswordHashKey, out text))
				{
					settings.CustomQuitPasswordHash = text;
				}

				if (data.TryGetValue(RequirePasswordToStartKey, out text))
				{
					settings.RequirePasswordToStart = text == "1";
				}

				if (settings.QuitPasswordMode == TeacherQuitPasswordMode.Custom && string.IsNullOrEmpty(settings.CustomQuitPasswordHash))
				{
					settings.QuitPasswordMode = TeacherQuitPasswordMode.Teacher;
				}
			}
			catch (Exception)
			{
				settings.ResetToDefaults();
			}

			return settings;
		}

		#endregion

		#region Применение к настройкам SEB

		/// <summary>
		/// Накладывает настройки преподавателя поверх текущих настроек SEB (настройки по умолчанию или из .seb-файла).
		/// </summary>
		public void ApplyTo(AppSettings settings)
		{
			// Экран и окна
			settings.Browser.MainWindow.FullScreenMode = Get("fullscreen");

			settings.Display.AllowedDisplays = Get("singleDisplay") ? 1 : 10;
			settings.Display.IgnoreError = !Get("singleDisplay");
			settings.Display.InternalDisplayOnly = false;

			settings.Browser.PopupPolicy = Get("newWindows") ? PopupPolicy.Allow : PopupPolicy.AllowSameWindow;

			settings.UserInterface.Taskbar.EnableTaskbar = Get("showTaskBar");
			settings.UserInterface.Taskbar.ShowNetwork = Get("showWiFi");
			settings.UserInterface.ActionCenter.ShowNetwork = Get("showWiFi");

			settings.Browser.MainWindow.ShowToolbar = Get("showToolbar");
			settings.Browser.AdditionalWindow.ShowToolbar = Get("showToolbar");
			settings.Browser.MainWindow.AllowAddressBar = false;
			settings.Browser.AdditionalWindow.AllowAddressBar = false;

			// Ограничения системы
			var lockdown = Get("blockAppSwitching");

			settings.Security.KioskMode = lockdown ? KioskMode.CreateNewDesktop : KioskMode.None;
			settings.Keyboard.AllowAltTab = !lockdown;
			settings.Keyboard.AllowAltEsc = !lockdown;
			settings.Keyboard.AllowCtrlEsc = !lockdown;
			settings.Keyboard.AllowSystemKey = !lockdown;

			var systemMenu = Get("blockSystemMenu");

			settings.Service.DisableTaskManager = systemMenu;
			settings.Service.DisableUserLock = systemMenu;
			settings.Service.DisableUserSwitch = systemMenu;
			settings.Service.DisableSignout = systemMenu;
			settings.Service.DisablePasswordChange = systemMenu;
			settings.Service.DisablePowerOptions = systemMenu;
			settings.Service.DisableNetworkOptions = systemMenu;
			settings.Service.DisableEaseOfAccessOptions = systemMenu;

			settings.Security.AllowWindowCapture = !Get("blockScreenCapture");
			settings.Keyboard.AllowPrintScreen = !Get("blockScreenCapture");

			settings.Service.DisableRemoteConnections = Get("blockRemote");

			if (!Get("closeForbiddenApps"))
			{
				settings.Applications.Blacklist.Clear();
			}

			settings.Security.VirtualMachinePolicy = Get("blockVM") ? VirtualMachinePolicy.Deny : VirtualMachinePolicy.Allow;
			settings.Security.ClipboardPolicy = Get("privateClipboard") ? ClipboardPolicy.Isolated : ClipboardPolicy.Allow;
			settings.Browser.UseIsolatedClipboard = Get("privateClipboard");

			// Дополнения
			settings.Browser.AllowSpellChecking = Get("spellCheck");
			settings.Browser.AllowPrint = Get("print");
			settings.Browser.AllowFind = Get("find");
			settings.Browser.AllowPageZoom = Get("zoom");
			settings.Browser.AllowDownloads = Get("downUploads");
			settings.Browser.AllowUploads = Get("downUploads");
			settings.Mouse.AllowRightButton = Get("rightMouse");

			foreach (var window in new[] { settings.Browser.MainWindow, settings.Browser.AdditionalWindow })
			{
				window.AllowBackwardNavigation = Get("backForward");
				window.AllowForwardNavigation = Get("backForward");
				window.AllowReloading = Get("reload");
				window.ShowReloadButton = Get("reload");
				window.AllowDeveloperConsole = Get("devConsole");
			}

			settings.UserInterface.Taskbar.ShowClock = Get("showTime");
			settings.UserInterface.ActionCenter.ShowClock = Get("showTime");

			// Выход
			settings.Security.AllowTermination = Get("allowQuit");
			settings.Security.AllowReconfiguration = Get("allowReconfiguration");

			switch (QuitPasswordMode)
			{
				case TeacherQuitPasswordMode.Teacher:
					settings.Security.QuitPasswordHash = TeacherPasswordHash;
					break;
				case TeacherQuitPasswordMode.Custom:
					settings.Security.QuitPasswordHash = string.IsNullOrEmpty(CustomQuitPasswordHash) ? TeacherPasswordHash : CustomQuitPasswordHash;
					break;
				default:
					settings.Security.QuitPasswordHash = string.Empty;
					break;
			}

			// Адрес контрольной
			var url = (StartUrl ?? string.Empty).Trim();

			if (url.Length > 0)
			{
				if (!url.Contains("://"))
				{
					url = "https://" + url;
				}

				settings.Browser.StartUrl = url;
			}
		}

		#endregion
	}
}
