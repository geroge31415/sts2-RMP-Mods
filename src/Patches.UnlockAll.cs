using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Timeline;

namespace RemoveMultiplayerPlayerLimit
{
	[HarmonyPatch(typeof(SaveManager), "InitProgressData")]
	internal static class UnlockAllPatch
	{
		private static void Postfix(SaveManager __instance)
		{
			try
			{
				var progress = __instance.Progress;
				if (progress == null) return;

				object obtainedEnum = Enum.Parse(AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.EpochState"), "Obtained");
				var obtainEpochOverride = AccessTools.Method(typeof(ProgressState), "ObtainEpochOverride");

				if (EpochModel.AllEpochIds != null)
				{
					foreach (var epochId in EpochModel.AllEpochIds)
					{
						progress.RevealEpoch(epochId);
						progress.UnlockSlot(epochId);
						if (obtainEpochOverride != null)
						{
							obtainEpochOverride.Invoke(progress, new object[] { epochId, obtainedEnum });
						}
					}
				}

				progress.MaxMultiplayerAscension = 20;

				if (progress.CharacterStats != null)
				{
					foreach (var stat in progress.CharacterStats.Values)
					{
						if (stat != null)
						{
							stat.MaxAscension = 20;
						}
					}
				}

				AccessTools.Method(typeof(SaveManager), "SaveProgressFile")?.Invoke(__instance, null);
			}
			catch (Exception e)
			{
				Console.WriteLine($"[RMP Mod] Unlock All Failed: {e}");
			}
		}
	}
}
