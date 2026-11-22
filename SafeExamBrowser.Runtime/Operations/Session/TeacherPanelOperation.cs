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
			var teacher = TeacherSettings.Load();

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

					var start = SebUi.OnUiThread(() => new TeacherPanelWindow(teacher).ShowDialog());

					if (!start)
					{
						Logger.Info("SEBastian: the user chose to quit at the teacher panel.");
						return OperationResult.Aborted;
					}
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

			teacher.ApplyTo(Context.Next.Settings);
			Logger.Info("SEBastian: applied teacher settings (" + TeacherSettings.TitleFor(teacher.MatchingPreset()) + ").");

			return OperationResult.Success;
		}
	}
}
