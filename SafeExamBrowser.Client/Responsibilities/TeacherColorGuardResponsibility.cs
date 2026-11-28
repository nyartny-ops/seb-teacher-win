/*
 * SEBастьян — «Запретить смену цветов экрана» (Windows).
 * Во время контрольной следит за высоким контрастом, цветофильтрами, инверсией лупы и темой Windows.
 * При изменении блокирует экран SEB до ввода пароля выхода/разблокировки.
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using Microsoft.Win32;
using SafeExamBrowser.Client.Contracts;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.Settings.Teacher;
using SafeExamBrowser.UserInterface.Contracts.Windows.Data;

namespace SafeExamBrowser.Client.Responsibilities
{
	internal class TeacherColorGuardResponsibility : ClientResponsibility
	{
		private readonly ICoordinator coordinator;
		private readonly Timer timer;
		private readonly object sync = new object();

		private Dictionary<string, string> baseline;
		private bool running;

		public TeacherColorGuardResponsibility(ClientContext context, ICoordinator coordinator, ILogger logger) : base(context, logger)
		{
			this.coordinator = coordinator;
			this.timer = new Timer { AutoReset = false, Interval = 2000 };
			this.timer.Elapsed += Timer_Elapsed;
		}

		public override void Assume(ClientTask task)
		{
			switch (task)
			{
				case ClientTask.StartMonitoring:
					Start();
					break;
				case ClientTask.PrepareShutdown_Wave2:
					Stop();
					break;
			}
		}

		private void Start()
		{
			try
			{
				if (!TeacherSettings.LoadForSession().ColorGuardEnabled)
				{
					Logger.Info("SEBastian color guard is disabled.");
					return;
				}

				lock (sync)
				{
					baseline = ReadState();
					running = true;
				}

				timer.Start();
				Logger.Info("SEBastian color guard started.");
			}
			catch (Exception e)
			{
				Logger.Error("SEBastian color guard failed to start!", e);
			}
		}

		private void Stop()
		{
			lock (sync)
			{
				running = false;
			}

			timer.Stop();
		}

		private void Timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			try
			{
				Check();
			}
			catch (Exception ex)
			{
				Logger.Error("SEBastian color guard check failed!", ex);
			}
			finally
			{
				lock (sync)
				{
					if (running)
					{
						timer.Start();
					}
				}
			}
		}

		private void Check()
		{
			Dictionary<string, string> current;
			List<string> changed;

			lock (sync)
			{
				if (!running)
				{
					return;
				}

				current = ReadState();
				changed = current.Where(p => baseline.ContainsKey(p.Key) && baseline[p.Key] != p.Value).Select(p => p.Key).ToList();

				if (changed.Count == 0)
				{
					return;
				}

				// новое состояние становится базовым, чтобы одно и то же изменение не блокировало повторно
				baseline = current;
			}

			var reason = string.Join(", ", changed);
			Logger.Warn("SEBastian: screen colors changed during the exam (" + reason + "), locking SEB.");

			if (coordinator.RequestSessionLock())
			{
				try
				{
					var message = "Во время контрольной изменились цвета экрана: " + reason + "." + Environment.NewLine + Environment.NewLine +
						"Для продолжения введите пароль разблокировки.";

					ShowLockScreen(message, "SEB заблокирован", Enumerable.Empty<LockScreenOption>());
				}
				finally
				{
					coordinator.ReleaseSessionLock();
				}

				lock (sync)
				{
					baseline = ReadState();
				}
			}
		}

		private static Dictionary<string, string> ReadState()
		{
			var state = new Dictionary<string, string>
			{
				{ "Высокий контраст", HighContrast() },
				{ "Цветофильтр", Read(@"Software\Microsoft\ColorFiltering", "Active") },
				{ "Тип цветофильтра", Read(@"Software\Microsoft\ColorFiltering", "FilterType") },
				{ "Инверсия лупы", Read(@"Software\Microsoft\ScreenMagnifier", "Invert") },
				{ "Тема приложений", Read(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme") },
				{ "Тема Windows", Read(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme") },
				{ "Экранная лупа", MagnifierRunning() ? "1" : "0" }
			};

			return state;
		}

		private static bool MagnifierRunning()
		{
			var processes = Process.GetProcessesByName("Magnify");

			foreach (var process in processes)
			{
				process.Dispose();
			}

			return processes.Length > 0;
		}

		private static string HighContrast()
		{
			var flags = Read(@"Control Panel\Accessibility\HighContrast", "Flags");
			int value;

			return int.TryParse(flags, out value) ? ((value & 1) == 1 ? "1" : "0") : flags;
		}

		private static string Read(string path, string name)
		{
			try
			{
				using (var key = Registry.CurrentUser.OpenSubKey(path))
				{
					var value = key != null ? key.GetValue(name) : null;
					return value != null ? Convert.ToString(value) : "-";
				}
			}
			catch (Exception)
			{
				return "?";
			}
		}
	}
}
