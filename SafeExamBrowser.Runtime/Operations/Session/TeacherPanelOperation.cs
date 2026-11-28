/*
 * SEBастьян — ключ доступа и панель преподавателя в последовательности запуска сессии.
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using SafeExamBrowser.Core.Contracts.OperationModel;
using SafeExamBrowser.Core.Contracts.OperationModel.Events;
using SafeExamBrowser.Runtime.Teacher;
using SafeExamBrowser.Settings.Teacher;

namespace SafeExamBrowser.Runtime.Operations.Session
{
	/// <summary>
	/// При первом запуске сессии: ключ доступа (если включён на сервере), затем панель преподавателя.
	/// При каждом (пере)запуске сессии настройки преподавателя накладываются поверх настроек SEB.
	/// </summary>
	internal class TeacherPanelOperation : SessionOperation
	{
		private static bool passed;
		private static TeacherSettings current;

		public override event StatusChangedEventHandler StatusChanged;

		public TeacherPanelOperation(Dependencies dependencies) : base(dependencies)
		{
		}

		public override OperationResult Perform()
		{
			return Run();
		}

		public override OperationResult Repeat()
		{
			return Run();
		}

		public override OperationResult Revert()
		{
			return OperationResult.Success;
		}

		private OperationResult Run()
		{
			if (!passed)
			{
				try
				{
					RuntimeWindow.Hide();
				}
				catch (Exception)
				{
				}

				try
				{
					if (!new AccessKeyGate(Logger).Run())
					{
						Logger.Info("SEBastian: the user chose to quit at the access key window.");
						return OperationResult.Aborted;
					}

					// настройки из бота (если преподаватель их задал) уже учтены в Load()
					var teacher = TeacherSettings.Load();
					var start = SebUi.OnUiThread(() => new TeacherPanelWindow(teacher).ShowDialog());

					if (!start)
					{
						Logger.Info("SEBastian: the user chose to quit at the teacher panel.");
						return OperationResult.Aborted;
					}

					current = teacher;
				}
				catch (Exception e)
				{
					Logger.Error("SEBastian: teacher panel failed!", e);
					return OperationResult.Failed;
				}
				finally
				{
					try
					{
						RuntimeWindow.Show();
					}
					catch (Exception)
					{
					}
				}

				passed = true;
			}

			// при перенастройке (тест из Moodle) — те же настройки преподавателя поверх новых настроек теста
			var settings = current ?? TeacherSettings.Load();

			settings.ApplyTo(Context.Next.Settings);
			settings.SaveSession();
			Logger.Info("SEBastian: applied teacher settings (" + TeacherSettings.TitleFor(settings.MatchingPreset()) + (settings.FromServer ? ", from the bot" : "") +
				"), start URL: " + Context.Next.Settings.Browser.StartUrl);

			return OperationResult.Success;
		}
	}
}
