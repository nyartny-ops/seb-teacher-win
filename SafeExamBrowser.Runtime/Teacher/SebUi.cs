/*
 * SEBастьян — общий стиль окон (ретро-Windows + палитра SEBастьяна).
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace SafeExamBrowser.Runtime.Teacher
{
	internal static class SebUi
	{
		internal static readonly Brush Blue = Freeze(new SolidColorBrush(Color.FromRgb(0x23, 0x00, 0xFA)));
		internal static readonly Brush Lime = Freeze(new SolidColorBrush(Color.FromRgb(0xDC, 0xFF, 0x05)));
		internal static readonly Brush Pink = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0x02, 0xE6)));
		internal static readonly Brush Paper = Freeze(new SolidColorBrush(Color.FromRgb(0xF6, 0xF6, 0xF6)));
		internal static readonly Brush Gray = Freeze(new SolidColorBrush(Color.FromRgb(0x7F, 0x7F, 0x7F)));
		internal static readonly Brush Win = Freeze(new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)));
		internal static readonly Brush Red = Freeze(new SolidColorBrush(Color.FromRgb(0xD0, 0x00, 0x2A)));

		private static FontFamily pixel, mono;

		internal static FontFamily Pixel
		{
			get { return pixel ?? (pixel = LoadFont("Nihonium113", "Consolas")); }
		}

		internal static FontFamily Mono
		{
			get { return mono ?? (mono = LoadFont("Fira Mono", "Consolas")); }
		}

		internal static readonly FontFamily Sans = new FontFamily("Segoe UI");

		private static T Freeze<T>(T freezable) where T : Freezable
		{
			freezable.Freeze();
			return freezable;
		}

		private static FontFamily LoadFont(string family, string fallback)
		{
			try
			{
				return new FontFamily(new Uri("pack://application:,,,/"), "./Teacher/Fonts/#" + family + ", " + fallback);
			}
			catch (Exception)
			{
				return new FontFamily(fallback);
			}
		}

		/// <summary>Окно без системной рамки, с синим заголовком в стиле Windows 95.</summary>
		internal static Window CreateWindow(string title, double width, double height, out ContentControl body)
		{
			var window = new Window
			{
				Title = title,
				Width = width,
				Height = height,
				WindowStyle = WindowStyle.None,
				ResizeMode = ResizeMode.NoResize,
				WindowStartupLocation = WindowStartupLocation.CenterScreen,
				Topmost = true,
				ShowInTaskbar = true,
				Background = Win,
				FontFamily = Sans,
				FontSize = 14,
				SnapsToDevicePixels = true,
				UseLayoutRounding = true
			};

			var outer = new Border
			{
				BorderThickness = new Thickness(2),
				BorderBrush = Brushes.Black,
				Background = Win,
				Padding = new Thickness(3)
			};
			var dock = new DockPanel { LastChildFill = true };

			var bar = new Border
			{
				Height = 30,
				Background = new LinearGradientBrush(Color.FromRgb(0x23, 0x00, 0xFA), Color.FromRgb(0x6B, 0x5B, 0xFF), 0),
				Padding = new Thickness(10, 0, 6, 0)
			};
			var barGrid = new DockPanel();
			// знак SEBастьяна: «>_» в лаймовом квадрате
			var logo = new Border
			{
				Background = Lime,
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(1.5),
				Padding = new Thickness(5, 0, 5, 1),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0, 0, 10, 0),
				Child = new TextBlock { Text = ">_", FontFamily = Mono, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, FontSize = 13 }
			};
			var caption = new TextBlock
			{
				Text = title,
				FontFamily = Mono,
				Foreground = Brushes.White,
				FontSize = 14,
				VerticalAlignment = VerticalAlignment.Center
			};
			var beta = new Border
			{
				Background = Lime,
				Padding = new Thickness(6, 0, 6, 1),
				VerticalAlignment = VerticalAlignment.Center,
				Child = new TextBlock { Text = "β бета", FontFamily = Mono, FontSize = 11, Foreground = Brushes.Black },
				RenderTransform = new RotateTransform(3)
			};
			DockPanel.SetDock(logo, Dock.Left);
			DockPanel.SetDock(beta, Dock.Right);
			barGrid.Children.Add(logo);
			barGrid.Children.Add(beta);
			barGrid.Children.Add(caption);
			bar.Child = barGrid;
			bar.MouseLeftButtonDown += (o, e) => { try { window.DragMove(); } catch (InvalidOperationException) { } };

			DockPanel.SetDock(bar, Dock.Top);
			dock.Children.Add(bar);

			var inner = new Border
			{
				Background = Brushes.White,
				BorderThickness = new Thickness(2, 2, 1, 1),
				BorderBrush = Gray,
				Margin = new Thickness(0, 3, 0, 0)
			};
			body = new ContentControl();
			inner.Child = body;
			dock.Children.Add(inner);

			outer.Child = dock;
			window.Content = outer;

			return window;
		}

		internal static TextBlock Heading(string text, double size = 40)
		{
			return new TextBlock
			{
				Text = text.ToUpperInvariant(),
				FontFamily = Pixel,
				FontSize = size,
				Foreground = Brushes.Black,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 0, 0, 8)
			};
		}

		internal static TextBlock Text(string text, double size = 14, Brush color = null, bool wrap = true)
		{
			return new TextBlock
			{
				Text = text,
				FontSize = size,
				Foreground = color ?? Brushes.Black,
				TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap
			};
		}

		internal static TextBlock MonoText(string text, double size = 13, Brush color = null)
		{
			var block = Text(text, size, color);
			block.FontFamily = Mono;
			return block;
		}

		/// <summary>Объёмная кнопка в стиле Windows 95 с фирменным цветом.</summary>
		internal static Button Button(string text, Brush background = null, Brush foreground = null, double minWidth = 120)
		{
			var button = new Button
			{
				Content = text,
				Background = background ?? Win,
				Foreground = foreground ?? Brushes.Black,
				FontFamily = Mono,
				FontSize = 14,
				MinWidth = minWidth,
				MinHeight = 40,
				Padding = new Thickness(16, 6, 16, 6),
				Cursor = Cursors.Hand,
				Template = ButtonTemplate()
			};

			return button;
		}

		private static ControlTemplate buttonTemplate;

		private static ControlTemplate ButtonTemplate()
		{
			if (buttonTemplate != null)
			{
				return buttonTemplate;
			}

			var template = new ControlTemplate(typeof(Button));
			var outer = new FrameworkElementFactory(typeof(Border), "outer");
			outer.SetValue(Border.BorderThicknessProperty, new Thickness(2));
			outer.SetValue(Border.BorderBrushProperty, new LinearGradientBrush(Colors.White, Colors.Black, 45));
			outer.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
			outer.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

			var content = new FrameworkElementFactory(typeof(ContentPresenter), "content");
			content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
			content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
			content.SetValue(FrameworkElement.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
			outer.AppendChild(content);
			template.VisualTree = outer;

			var pressed = new Trigger { Property = ButtonBase.IsPressedProperty, Value = true };
			pressed.Setters.Add(new Setter(Border.BorderBrushProperty, new LinearGradientBrush(Colors.Black, Colors.White, 45), "outer"));
			pressed.Setters.Add(new Setter(UIElement.RenderTransformProperty, new TranslateTransform(1, 1), "outer"));
			template.Triggers.Add(pressed);

			var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
			disabled.Setters.Add(new Setter(UIElement.OpacityProperty, 0.5, "outer"));
			template.Triggers.Add(disabled);

			var focus = new Trigger { Property = UIElement.IsKeyboardFocusedProperty, Value = true };
			focus.Setters.Add(new Setter(Border.BorderBrushProperty, Pink, "outer"));
			template.Triggers.Add(focus);

			buttonTemplate = template;

			return template;
		}

		internal static Border Card(UIElement child, Brush background = null, Brush border = null)
		{
			return new Border
			{
				Background = background ?? Brushes.White,
				BorderBrush = border ?? Brushes.Black,
				BorderThickness = new Thickness(2),
				Padding = new Thickness(14),
				Child = child
			};
		}

		internal static TextBox Input(double fontSize = 16, bool mono = false)
		{
			return new TextBox
			{
				FontSize = fontSize,
				FontFamily = mono ? Mono : Sans,
				Padding = new Thickness(8, 6, 8, 6),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(2),
				VerticalContentAlignment = VerticalAlignment.Center
			};
		}

		internal static PasswordBox Password()
		{
			return new PasswordBox
			{
				FontSize = 16,
				Padding = new Thickness(8, 6, 8, 6),
				BorderBrush = Brushes.Black,
				BorderThickness = new Thickness(2),
				VerticalContentAlignment = VerticalAlignment.Center
			};
		}

		internal static StackPanel Row(params UIElement[] children)
		{
			var panel = new StackPanel { Orientation = Orientation.Horizontal };

			foreach (var child in children)
			{
				var element = child as FrameworkElement;

				if (element != null && panel.Children.Count > 0)
				{
					element.Margin = new Thickness(8, element.Margin.Top, element.Margin.Right, element.Margin.Bottom);
				}

				panel.Children.Add(child);
			}

			return panel;
		}

		/// <summary>Показывает окно модально на UI-потоке (операции сессии выполняются в фоновом потоке).</summary>
		internal static T OnUiThread<T>(Func<T> func)
		{
			var dispatcher = Application.Current != null ? Application.Current.Dispatcher : null;

			if (dispatcher == null || dispatcher.CheckAccess())
			{
				return func();
			}

			return (T) dispatcher.Invoke(func);
		}
	}
}
