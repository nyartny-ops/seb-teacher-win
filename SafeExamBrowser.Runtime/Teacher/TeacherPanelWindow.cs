/*
 * SEBастьян — панель преподавателя (Windows). Показывается перед началом контрольной:
 * «Начать контрольную», «Настройки преподавателя…» (по паролю), «Выйти».
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SafeExamBrowser.Settings.Teacher;

namespace SafeExamBrowser.Runtime.Teacher
{
	internal class TeacherPanelWindow
	{
		private const int MaxPasswordAttempts = 5;
		private static readonly TimeSpan Lockout = TimeSpan.FromSeconds(30);

		private enum Pending { None, Settings, Start }

		private readonly TeacherSettings settings;
		private readonly Window window;
		private readonly ContentControl body;

		private bool startExam;
		private Pending pending;
		private int failedAttempts;
		private DateTime lockedUntil = DateTime.MinValue;

		// стартовый экран
		private StackPanel mainButtons, passwordBox;
		private TextBlock passwordPrompt, passwordError;
		private PasswordBox passwordField;

		// настройки
		private readonly Dictionary<string, CheckBox> checkboxes = new Dictionary<string, CheckBox>();
		private readonly List<ToggleButton> presetButtons = new List<ToggleButton>();
		private ComboBox quitMode;
		private PasswordBox customQuitPassword;
		private CheckBox requirePasswordToStart;
		private TextBox startUrl;
		private bool updatingPreset;

		internal TeacherPanelWindow(TeacherSettings settings)
		{
			this.settings = settings;
			window = SebUi.CreateWindow("SEBастьян — панель преподавателя", 760, 600, out body);
			window.Closing += (o, e) => { if (window.DialogResult == null) { e.Cancel = true; } };
			window.PreviewKeyDown += Window_PreviewKeyDown;
		}

		/// <summary>true — начать контрольную, false — выйти из SEB.</summary>
		internal bool ShowDialog()
		{
			ShowStart();
			window.Loaded += (o, e) => { window.Activate(); window.Focus(); };
			window.ShowDialog();
			return startExam;
		}

		private void Finish(bool start)
		{
			startExam = start;
			window.DialogResult = start;
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			// Alt+F4 не закрывает панель
			if (e.Key == Key.System && e.SystemKey == Key.F4)
			{
				e.Handled = true;
			}
		}

		#region Стартовый экран

		private void ShowStart()
		{
			window.Height = 600;

			var stack = new StackPanel { Margin = new Thickness(40, 30, 40, 24) };
			stack.Children.Add(SebUi.MonoText("safe_exam_browser.exe", 13, SebUi.Gray));
			stack.Children.Add(SebUi.Heading("Контрольная", 56));

			var preset = TeacherSettings.TitleFor(settings.MatchingPreset());
			var level = SebUi.Text("Уровень контроля: " + preset, 15);
			level.FontWeight = FontWeights.SemiBold;
			stack.Children.Add(level);

			var url = string.IsNullOrWhiteSpace(settings.StartUrl) ? "из настроек SEB / .seb-файла" : settings.StartUrl;
			var urlText = SebUi.Text("Адрес: " + url, 13, SebUi.Gray);
			urlText.Margin = new Thickness(0, 4, 0, 24);
			stack.Children.Add(urlText);

			var start = SebUi.Button("▶  Начать контрольную", SebUi.Lime, null, 340);
			start.FontSize = 17;
			start.MinHeight = 54;
			start.IsDefault = true;
			start.Click += (o, e) => { if (settings.RequirePasswordToStart) { AskPassword(Pending.Start); } else { Finish(true); } };

			var configure = SebUi.Button("⚙  Настройки преподавателя…", null, null, 340);
			configure.Click += (o, e) => AskPassword(Pending.Settings);

			var quit = SebUi.Button("Выйти", Brushes.White, null, 340);
			quit.Click += (o, e) => Finish(false);

			mainButtons = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
			foreach (var button in new[] { start, configure, quit })
			{
				button.HorizontalAlignment = HorizontalAlignment.Left;
				button.Margin = new Thickness(0, 0, 0, 10);
				mainButtons.Children.Add(button);
			}
			stack.Children.Add(mainButtons);

			passwordPrompt = SebUi.Text("Введите пароль преподавателя:", 15);
			passwordField = SebUi.Password();
			passwordField.Width = 340;
			passwordField.HorizontalAlignment = HorizontalAlignment.Left;
			passwordField.Margin = new Thickness(0, 8, 0, 6);
			passwordError = SebUi.Text("", 13, SebUi.Red);
			var ok = SebUi.Button("OK", SebUi.Lime, null, 120);
			ok.Click += (o, e) => CheckPassword();
			var cancel = SebUi.Button("Отмена", Brushes.White, null, 120);
			cancel.IsCancel = true;
			cancel.Click += (o, e) => HidePassword();
			passwordField.KeyDown += (o, e) => { if (e.Key == Key.Enter) { CheckPassword(); e.Handled = true; } };

			passwordBox = new StackPanel { Visibility = Visibility.Collapsed };
			passwordBox.Children.Add(passwordPrompt);
			passwordBox.Children.Add(passwordField);
			passwordBox.Children.Add(passwordError);
			var buttons = SebUi.Row(cancel, ok);
			buttons.Margin = new Thickness(0, 8, 0, 0);
			passwordBox.Children.Add(buttons);
			stack.Children.Add(passwordBox);

			var grid = new Grid();
			grid.Children.Add(stack);
			var tag = new Border
			{
				Background = SebUi.Pink,
				Padding = new Thickness(12, 4, 12, 6),
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0, 34, 36, 0),
				RenderTransform = new RotateTransform(-3),
				Child = new TextBlock { Text = "помогатор,\nкогда трудно", Foreground = Brushes.White, FontSize = 20, FontWeight = FontWeights.Black }
			};
			grid.Children.Add(tag);

			var bottom = new Border
			{
				Background = SebUi.Lime,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(0, 2, 0, 0),
				Height = 34,
				VerticalAlignment = VerticalAlignment.Bottom,
				Child = new TextBlock
				{
					Text = "SEBастьян · бета — используйте на свой страх и риск",
					FontFamily = SebUi.Mono,
					FontSize = 12,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(40, 0, 0, 0)
				}
			};
			grid.Children.Add(bottom);

			body.Content = grid;
		}

		private void AskPassword(Pending action)
		{
			pending = action;
			passwordPrompt.Text = action == Pending.Start
				? "Для начала контрольной введите пароль преподавателя:"
				: "Для открытия настроек введите пароль преподавателя:";
			passwordField.Password = string.Empty;
			passwordError.Text = string.Empty;
			mainButtons.Visibility = Visibility.Collapsed;
			passwordBox.Visibility = Visibility.Visible;
			passwordField.Focus();
			Keyboard.Focus(passwordField);
		}

		private void HidePassword()
		{
			pending = Pending.None;
			passwordField.Password = string.Empty;
			passwordBox.Visibility = Visibility.Collapsed;
			mainButtons.Visibility = Visibility.Visible;
		}

		private void CheckPassword()
		{
			if (DateTime.Now < lockedUntil)
			{
				passwordError.Text = string.Format("Слишком много попыток. Подождите {0} с.", (int) Math.Ceiling((lockedUntil - DateTime.Now).TotalSeconds));
				return;
			}

			if (TeacherSettings.CheckTeacherPassword(passwordField.Password))
			{
				failedAttempts = 0;
				var action = pending;
				HidePassword();

				if (action == Pending.Start)
				{
					Finish(true);
				}
				else
				{
					ShowSettings();
				}
			}
			else
			{
				failedAttempts++;
				passwordField.Password = string.Empty;

				if (failedAttempts >= MaxPasswordAttempts)
				{
					failedAttempts = 0;
					lockedUntil = DateTime.Now + Lockout;
					passwordError.Text = "Слишком много попыток. Подождите 30 секунд.";
				}
				else
				{
					passwordError.Text = "Неверный пароль.";
				}

				System.Media.SystemSounds.Beep.Play();
			}
		}

		#endregion

		#region Настройки

		private void ShowSettings()
		{
			window.Height = Math.Min(860, SystemParameters.WorkArea.Height - 40);
			window.Top = Math.Max(SystemParameters.WorkArea.Top + 10, (SystemParameters.WorkArea.Height - window.Height) / 2);
			checkboxes.Clear();
			presetButtons.Clear();

			var root = new DockPanel();

			// Уровень контроля
			var header = new StackPanel { Margin = new Thickness(24, 18, 24, 10) };
			header.Children.Add(SebUi.Heading("Настройки", 40));
			header.Children.Add(SebUi.MonoText("// уровень контроля", 13, SebUi.Gray));
			var presets = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
			foreach (var preset in new[] { TeacherPreset.Free, TeacherPreset.Standard, TeacherPreset.Strict, TeacherPreset.Custom })
			{
				var button = new ToggleButton
				{
					Content = TeacherSettings.TitleFor(preset),
					Tag = preset,
					FontFamily = SebUi.Mono,
					FontSize = 14,
					Padding = new Thickness(14, 6, 14, 6),
					Margin = new Thickness(0, 0, 6, 0),
					BorderBrush = Brushes.Black,
					BorderThickness = new Thickness(2),
					Background = Brushes.White,
					Cursor = Cursors.Hand,
					IsEnabled = preset != TeacherPreset.Custom
				};
				button.Checked += (o, e) => { button.Background = SebUi.Blue; button.Foreground = Brushes.White; };
				button.Unchecked += (o, e) => { button.Background = Brushes.White; button.Foreground = Brushes.Black; };
				button.Click += Preset_Click;
				presetButtons.Add(button);
				presets.Children.Add(button);
			}
			header.Children.Add(presets);
			DockPanel.SetDock(header, Dock.Top);
			root.Children.Add(header);

			// Кнопки
			var reset = SebUi.Button("Сбросить", Brushes.White, null, 110);
			reset.Click += (o, e) => { settings.ResetToDefaults(); ShowSettings(); };
			var cancel = SebUi.Button("Отмена", Brushes.White, null, 110);
			cancel.Click += (o, e) => { var fresh = TeacherSettings.Load(); CopyFrom(fresh); ShowStart(); };
			var save = SebUi.Button("Сохранить", null, null, 130);
			save.Click += (o, e) => { if (Collect()) { Save(); ShowStart(); } };
			var saveStart = SebUi.Button("Сохранить и начать ▶", SebUi.Lime, null, 200);
			saveStart.Click += (o, e) => { if (Collect()) { Save(); Finish(true); } };
			var buttons = new DockPanel { Margin = new Thickness(24, 10, 24, 14), LastChildFill = false };
			DockPanel.SetDock(reset, Dock.Left);
			DockPanel.SetDock(saveStart, Dock.Right);
			DockPanel.SetDock(save, Dock.Right);
			DockPanel.SetDock(cancel, Dock.Right);
			buttons.Children.Add(reset);
			buttons.Children.Add(saveStart);
			save.Margin = new Thickness(0, 0, 8, 0);
			buttons.Children.Add(save);
			cancel.Margin = new Thickness(0, 0, 8, 0);
			buttons.Children.Add(cancel);
			var buttonsBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 2, 0, 0), Background = SebUi.Paper, Child = buttons };
			DockPanel.SetDock(buttonsBorder, Dock.Bottom);
			root.Children.Add(buttonsBorder);

			// Переключатели
			var list = new StackPanel { Margin = new Thickness(24, 0, 24, 20) };

			for (var section = 0; section < TeacherSettings.SectionTitles.Length; section++)
			{
				var title = SebUi.MonoText("// " + TeacherSettings.SectionTitles[section].ToLowerInvariant(), 13, SebUi.Blue);
				title.Margin = new Thickness(0, 18, 0, 8);
				title.FontWeight = FontWeights.SemiBold;
				list.Children.Add(title);

				foreach (var option in TeacherSettings.Options)
				{
					if (option.Section != section)
					{
						continue;
					}

					var text = new StackPanel();
					text.Children.Add(SebUi.Text(option.Title, 14.5));
					text.Children.Add(SebUi.Text(option.Hint, 12.5, SebUi.Gray));

					var box = new CheckBox
					{
						Content = text,
						IsChecked = settings.Get(option.Id),
						Margin = new Thickness(0, 0, 0, 10),
						VerticalContentAlignment = VerticalAlignment.Top,
						Cursor = Cursors.Hand,
						Tag = option.Id
					};
					box.Checked += (o, e) => OptionChanged();
					box.Unchecked += (o, e) => OptionChanged();
					checkboxes[option.Id] = box;
					list.Children.Add(box);
				}

				if (section == 3)
				{
					list.Children.Add(SebUi.Text("Пароль выхода и разблокировки", 14.5));
					quitMode = new ComboBox { Width = 300, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 6), FontSize = 14 };
					quitMode.Items.Add("Без пароля");
					quitMode.Items.Add("Пароль преподавателя");
					quitMode.Items.Add("Отдельный пароль");
					quitMode.SelectedIndex = (int) settings.QuitPasswordMode;
					list.Children.Add(quitMode);

					customQuitPassword = SebUi.Password();
					customQuitPassword.Width = 300;
					customQuitPassword.HorizontalAlignment = HorizontalAlignment.Left;
					customQuitPassword.ToolTip = "Новый пароль выхода (оставьте пустым, чтобы не менять)";
					customQuitPassword.Visibility = settings.QuitPasswordMode == TeacherQuitPasswordMode.Custom ? Visibility.Visible : Visibility.Collapsed;
					list.Children.Add(customQuitPassword);
					var customHint = SebUi.Text(string.IsNullOrEmpty(settings.CustomQuitPasswordHash) ? "Введите отдельный пароль." : "Отдельный пароль задан. Введите новый, чтобы заменить.", 12.5, SebUi.Gray);
					customHint.Visibility = customQuitPassword.Visibility;
					list.Children.Add(customHint);
					quitMode.SelectionChanged += (o, e) =>
					{
						var visible = quitMode.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
						customQuitPassword.Visibility = visible;
						customHint.Visibility = visible;
					};

					requirePasswordToStart = new CheckBox
					{
						Content = SebUi.Text("Требовать пароль преподавателя для «Начать контрольную»", 14.5),
						IsChecked = settings.RequirePasswordToStart,
						Margin = new Thickness(0, 14, 0, 10)
					};
					list.Children.Add(requirePasswordToStart);

					list.Children.Add(SebUi.Text("Адрес контрольной (ссылка на тест)", 14.5));
					startUrl = SebUi.Input(14);
					startUrl.Text = settings.StartUrl ?? string.Empty;
					startUrl.Margin = new Thickness(0, 6, 0, 0);
					list.Children.Add(startUrl);
					list.Children.Add(SebUi.Text("Пусто — адрес из настроек SEB или .seb-файла.", 12.5, SebUi.Gray));
				}
			}

			var scroll = new ScrollViewer
			{
				Content = list,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
			};
			root.Children.Add(scroll);

			body.Content = root;
			UpdatePresetButtons();
		}

		private void Preset_Click(object sender, RoutedEventArgs e)
		{
			var preset = (TeacherPreset) ((ToggleButton) sender).Tag;

			updatingPreset = true;
			foreach (var option in TeacherSettings.Options)
			{
				CheckBox box;

				if (checkboxes.TryGetValue(option.Id, out box))
				{
					box.IsChecked = option.ValueFor(preset);
				}
			}
			updatingPreset = false;
			UpdatePresetButtons();
		}

		private void OptionChanged()
		{
			if (!updatingPreset)
			{
				UpdatePresetButtons();
			}
		}

		private void UpdatePresetButtons()
		{
			var probe = new TeacherSettings();

			foreach (var pair in checkboxes)
			{
				probe.Set(pair.Key, pair.Value.IsChecked == true);
			}

			var current = probe.MatchingPreset();

			foreach (var button in presetButtons)
			{
				button.IsChecked = (TeacherPreset) button.Tag == current;
			}
		}

		private bool Collect()
		{
			foreach (var pair in checkboxes)
			{
				settings.Set(pair.Key, pair.Value.IsChecked == true);
			}

			var mode = (TeacherQuitPasswordMode) Math.Max(0, quitMode.SelectedIndex);

			if (mode == TeacherQuitPasswordMode.Custom)
			{
				if (customQuitPassword.Password.Length > 0)
				{
					settings.CustomQuitPasswordHash = TeacherSettings.Sha256(customQuitPassword.Password);
				}
				else if (string.IsNullOrEmpty(settings.CustomQuitPasswordHash))
				{
					MessageBox.Show(window, "Введите отдельный пароль выхода или выберите другой вариант.", "SEBастьян", MessageBoxButton.OK, MessageBoxImage.Warning);
					customQuitPassword.Focus();
					return false;
				}
			}

			settings.QuitPasswordMode = mode;
			settings.RequirePasswordToStart = requirePasswordToStart.IsChecked == true;
			settings.StartUrl = (startUrl.Text ?? string.Empty).Trim();

			return true;
		}

		private void Save()
		{
			if (!settings.Save())
			{
				MessageBox.Show(window, "Не удалось сохранить настройки. Они будут действовать только для этого запуска.", "SEBастьян", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void CopyFrom(TeacherSettings other)
		{
			foreach (var option in TeacherSettings.Options)
			{
				settings.Set(option.Id, other.Get(option.Id));
			}

			settings.StartUrl = other.StartUrl;
			settings.QuitPasswordMode = other.QuitPasswordMode;
			settings.CustomQuitPasswordHash = other.CustomQuitPasswordHash;
			settings.RequirePasswordToStart = other.RequirePasswordToStart;
		}

		#endregion
	}
}
