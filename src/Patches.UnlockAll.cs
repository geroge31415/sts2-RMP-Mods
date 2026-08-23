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
					// リセットして不正なエポック（開発中のキャラ等）を消去
					AccessTools.Method(typeof(ProgressState), "ResetEpochs")?.Invoke(progress, null);

					string[] safePrefixes = new[] { "Ironclad", "Silent", "Defect", "Regent", "Necrobinder", "Colorless", "Relic", "Event", "Potion", "DailyRun", "CustomAndSeeds", "Neow", "Act3B" };

					foreach (var epochId in EpochModel.AllEpochIds)
					{
						bool isSafe = false;
						foreach (var prefix in safePrefixes)
						{
							if (epochId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
							{
								isSafe = true;
								break;
							}
						}
						
						if (!isSafe) continue;

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
					foreach (var kvp in progress.CharacterStats)
					{
						var charId = kvp.Key.ToString();
						// 不正なキャラのAscensionを上げない
						if (charId.Contains("AUTOMATON") || charId.Contains("AWAKENED") || charId.Contains("CHAMP")) 
							continue;
							
						if (kvp.Value != null)
						{
							kvp.Value.MaxAscension = 20;
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
