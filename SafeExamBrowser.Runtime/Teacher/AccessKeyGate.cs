/*
 * SEBастьян — ключ доступа перед запуском (Windows). Если на сервере включён вход по ключу,
 * SEB просит одноразовый ключ из Telegram-бота и проверяет его на сервере.
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.Settings.Teacher;

namespace SafeExamBrowser.Runtime.Teacher
{
	internal class AccessKeyGate
	{
		internal const string DefaultServer = "https://seb.korova.pro";

		private readonly ILogger logger;
		private readonly string server;
		private string botUrl;

		internal AccessKeyGate(ILogger logger)
		{
			this.logger = logger;
			// Другой адрес сервера — только для автотеста: нужна и переменная окружения, и служебный файл
			// в папке программы (туда может писать только администратор).
			var custom = Environment.GetEnvironmentVariable("SEBASTIAN_SERVER");
			var testFlag = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sebastian-test-server.flag");

			if (string.IsNullOrWhiteSpace(custom) || !File.Exists(testFlag))
			{
				custom = null;
			}

			server = (custom == null ? DefaultServer : custom.Trim()).TrimEnd('/');
			botUrl = server + "/bot";

			try
			{
				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			}
			catch (Exception)
			{
			}
		}

		#region Сеть

		internal class Response
		{
			internal int Status;
			internal Dictionary<string, object> Json;
			internal bool NetworkError;

			internal string Message
			{
				get
				{
					object value;
					return Json != null && Json.TryGetValue("message", out value) && value is string ? (string) value : null;
				}
			}

			internal bool Flag(string key)
			{
				object value;
				return Json != null && Json.TryGetValue(key, out value) && value is bool && (bool) value;
			}
		}

		private Response Request(string path, Dictionary<string, object> body)
		{
			var response = new Response();

			try
			{
				var request = (HttpWebRequest) WebRequest.Create(server + path);
				request.Timeout = 10000;
				request.ReadWriteTimeout = 10000;
				request.UserAgent = "SEBastian-Windows";
				request.Proxy = WebRequest.DefaultWebProxy;

				if (body != null)
				{
					var data = Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(body));
					request.Method = "POST";
					request.ContentType = "application/json";
					request.ContentLength = data.Length;

					using (var stream = request.GetRequestStream())
					{
						stream.Write(data, 0, data.Length);
					}
				}

				HttpWebResponse http;

				try
				{
					http = (HttpWebResponse) request.GetResponse();
				}
				catch (WebException e)
				{
					http = e.Response as HttpWebResponse;

					if (http == null)
					{
						throw;
					}
				}

				using (http)
				using (var reader = new StreamReader(http.GetResponseStream(), Encoding.UTF8))
				{
					response.Status = (int) http.StatusCode;
					var text = reader.ReadToEnd();

					try
					{
						response.Json = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(text);
					}
					catch (Exception)
					{
						response.Json = null;
					}
				}
			}
			catch (Exception e)
			{
				response.NetworkError = true;
				logger.Warn("SEBastian: request " + path + " failed: " + e.Message);
			}

			return response;
		}

		private static Dictionary<string, object> DeviceInfo()
		{
			var version = string.Empty;

			try
			{
				var attribute = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
				version = attribute != null ? attribute.InformationalVersion : string.Empty;
			}
			catch (Exception)
			{
			}

			return new Dictionary<string, object>
			{
				{ "platform", "win" },
				{ "device", Environment.MachineName },
				{ "user", Environment.UserName },
				{ "version", version }
			};
		}

		#endregion

		#region Кэш режима (если сервер недоступен)

		private static string CachePath
		{
			get { return Path.Combine(TeacherSettings.DirectoryPath, "AccessKeyGate.txt"); }
		}

		/// <summary>-1 — неизвестно, 0 — ключ не нужен, 1 — нужен.</summary>
		private static int CachedRequired()
		{
			try
			{
				if (File.Exists(CachePath))
				{
					var text = File.ReadAllText(CachePath).Trim();
					return text == "required=1" ? 1 : text == "required=0" ? 0 : -1;
				}
			}
			catch (Exception)
			{
			}

			return -1;
		}

		private static void StoreRequired(bool required)
		{
			try
			{
				Directory.CreateDirectory(TeacherSettings.DirectoryPath);
				File.WriteAllText(CachePath, required ? "required=1" : "required=0");
			}
			catch (Exception)
			{
			}
		}

		#endregion

		/// <summary>
		/// true — можно продолжать запуск, false — пользователь выбрал «Выйти».
		/// Вызывается из фонового потока операции сессии.
		/// </summary>
		internal bool Run()
		{
			var gate = Request("/api/v1/gate?platform=win", null);

			if (!gate.NetworkError && gate.Status == 200 && gate.Json != null && gate.Json.ContainsKey("required"))
			{
				var required = gate.Flag("required");
				object bot;

				if (gate.Json.TryGetValue("bot", out bot) && bot is string && ((string) bot).StartsWith("http"))
				{
					botUrl = (string) bot;
				}

				StoreRequired(required);
				logger.Info("SEBastian: access key required = " + required);

				if (!required)
				{
					var info = DeviceInfo();
					info["event"] = "launch";
					Task.Run(() => Request("/api/v1/launch", info));

					return true;
				}

				return SebUi.OnUiThread(() => new AccessKeyWindow(this, false, null).ShowDialog());
			}

			logger.Warn("SEBastian: gate check failed (HTTP " + gate.Status + ").");

			if (CachedRequired() == 0)
			{
				return true;
			}

			return SebUi.OnUiThread(() => new AccessKeyWindow(this, true, "Нет связи с сервером SEBастьяна. Проверь интернет, выключи VPN и нажми «Повторить».").ShowDialog());
		}

		/// <summary>Повторная проверка связи из окна (фоновый поток). null — нет связи.</summary>
		internal bool? CheckRequired()
		{
			var gate = Request("/api/v1/gate?platform=win", null);

			if (gate.NetworkError || gate.Status != 200 || gate.Json == null)
			{
				return null;
			}

			var required = gate.Flag("required");
			StoreRequired(required);

			return required;
		}

		/// <summary>Проверка ключа (фоновый поток). null — ключ принят, иначе текст ошибки.</summary>
		internal string Redeem(string key)
		{
			var body = DeviceInfo();
			body["key"] = key;

			var response = Request("/api/v1/redeem", body);

			if (!response.NetworkError && response.Status == 200 && response.Flag("ok"))
			{
				logger.Info("SEBastian: access key accepted.");
				return null;
			}

			if (response.NetworkError)
			{
				return "Нет связи с сервером SEBастьяна. Проверь интернет и VPN, потом попробуй ещё раз.";
			}

			return response.Message ?? "Сервер ответил ошибкой (" + response.Status + "). Попробуй ещё раз.";
		}

		internal void OpenBot()
		{
			try
			{
				Process.Start(new ProcessStartInfo(botUrl) { UseShellExecute = true });
			}
			catch (Exception e)
			{
				logger.Warn("SEBastian: could not open bot link: " + e.Message);
			}
		}
	}

	internal class AccessKeyWindow
	{
		private readonly AccessKeyGate gate;
		private readonly Window window;
		private readonly TextBox keyField;
		private readonly TextBlock error;
		private readonly Button continueButton, retryButton;
		private readonly TextBlock spinner;
		private bool offline, passed, busy;

		internal AccessKeyWindow(AccessKeyGate gate, bool offline, string message)
		{
			this.gate = gate;
			this.offline = offline;

			ContentControl body;
			window = SebUi.CreateWindow("SEBастьян — ключ доступа", 560, 430, out body);
			window.Closing += (o, e) => { if (window.DialogResult == null) { e.Cancel = true; } };
			window.PreviewKeyDown += (o, e) => { if (e.Key == Key.System && e.SystemKey == Key.F4) { e.Handled = true; } };

			var stack = new StackPanel { Margin = new Thickness(32, 22, 32, 18) };
			stack.Children.Add(SebUi.Heading("Ключ доступа", 44));
			stack.Children.Add(SebUi.Text("Получи ключ в Telegram-боте SEBастьян («🔑 Получить ключ») и введи его ниже. Один ключ — один запуск.", 14.5));

			keyField = SebUi.Input(28, true);
			keyField.CharacterCasing = CharacterCasing.Upper;
			keyField.MaxLength = 16;
			keyField.TextAlignment = TextAlignment.Center;
			keyField.FontWeight = FontWeights.SemiBold;
			keyField.Margin = new Thickness(0, 18, 0, 4);
			keyField.Height = 58;
			keyField.KeyDown += (o, e) => { if (e.Key == Key.Enter) { Continue(); e.Handled = true; } };
			stack.Children.Add(keyField);
			stack.Children.Add(SebUi.MonoText("например K7F3QX · регистр не важен", 12, SebUi.Gray));

			spinner = SebUi.MonoText("проверяю…", 13, SebUi.Blue);
			spinner.Visibility = Visibility.Collapsed;
			spinner.Margin = new Thickness(0, 10, 0, 0);
			stack.Children.Add(spinner);

			error = SebUi.Text(message ?? string.Empty, 13.5, SebUi.Red);
			error.Margin = new Thickness(0, 10, 0, 0);
			stack.Children.Add(error);

			var quit = SebUi.Button("Выйти", Brushes.White, null, 100);
			quit.Click += (o, e) => Close(false);
			var bot = SebUi.Button("Открыть бота", null, null, 140);
			bot.Click += (o, e) => gate.OpenBot();
			continueButton = SebUi.Button("Продолжить →", SebUi.Lime, null, 160);
			continueButton.Click += (o, e) => Continue();
			retryButton = SebUi.Button("Повторить", SebUi.Lime, null, 160);
			retryButton.Click += (o, e) => Retry();

			var buttons = SebUi.Row(quit, bot, continueButton, retryButton);
			buttons.HorizontalAlignment = HorizontalAlignment.Right;
			buttons.Margin = new Thickness(0, 16, 0, 0);

			var root = new DockPanel();
			DockPanel.SetDock(buttons, Dock.Bottom);
			buttons.Margin = new Thickness(32, 0, 32, 20);
			root.Children.Add(buttons);
			root.Children.Add(stack);
			body.Content = root;

			ApplyMode();
			window.Loaded += (o, e) =>
			{
				window.Activate();
				if (!this.offline)
				{
					keyField.Focus();
					Keyboard.Focus(keyField);
				}
			};
		}

		internal bool ShowDialog()
		{
			window.ShowDialog();
			return passed;
		}

		private void ApplyMode()
		{
			keyField.IsEnabled = !offline && !busy;
			continueButton.Visibility = offline ? Visibility.Collapsed : Visibility.Visible;
			retryButton.Visibility = offline ? Visibility.Visible : Visibility.Collapsed;
			continueButton.IsEnabled = !busy;
			retryButton.IsEnabled = !busy;
			continueButton.IsDefault = !offline;
			retryButton.IsDefault = offline;
			spinner.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
		}

		private void Close(bool result)
		{
			passed = result;
			window.DialogResult = result;
		}

		private void Continue()
		{
			if (busy || offline)
			{
				return;
			}

			var key = (keyField.Text ?? string.Empty).Trim();

			if (key.Length == 0)
			{
				error.Text = "Введи ключ из бота.";
				System.Media.SystemSounds.Beep.Play();
				return;
			}

			error.Text = string.Empty;
			busy = true;
			ApplyMode();

			Task.Run(() => gate.Redeem(key)).ContinueWith(task =>
			{
				window.Dispatcher.BeginInvoke(new Action(() =>
				{
					busy = false;
					var problem = task.IsFaulted ? "Ошибка проверки ключа, попробуй ещё раз." : task.Result;

					if (problem == null)
					{
						Close(true);
						return;
					}

					error.Text = problem;
					ApplyMode();
					System.Media.SystemSounds.Beep.Play();
					keyField.Focus();
					keyField.SelectAll();
				}));
			});
		}

		private void Retry()
		{
			if (busy)
			{
				return;
			}

			busy = true;
			error.Text = string.Empty;
			ApplyMode();

			Task.Run(() => gate.CheckRequired()).ContinueWith(task =>
			{
				window.Dispatcher.BeginInvoke(new Action(() =>
				{
					busy = false;
					var required = task.IsFaulted ? null : task.Result;

					if (required == null)
					{
						error.Text = "Всё ещё нет связи с сервером. Проверь интернет и VPN.";
						ApplyMode();
						return;
					}

					if (required == false)
					{
						Close(true);
						return;
					}

					offline = false;
					ApplyMode();
					keyField.Focus();
				}));
			});
		}
	}
}
