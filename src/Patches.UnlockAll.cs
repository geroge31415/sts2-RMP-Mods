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

				object revealedEnum = Enum.Parse(AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.EpochState"), "Revealed");
				var obtainEpochOverride = AccessTools.Method(typeof(ProgressState), "ObtainEpochOverride");

				if (EpochModel.AllEpochIds != null)
				{
					// リセチEして不正なエポックE開発中のキャラ等）を消去
					AccessTools.Method(typeof(ProgressState), "ResetEpochs")?.Invoke(progress, null);

					string[] safePrefixes = new[] { "IRONCLAD", "SILENT", "DEFECT", "REGENT", "NECROBINDER", "COLORLESS", "RELIC", "EVENT", "POTION", "DAILY", "CUSTOM", "NEOW", "ACT2", "ACT3", "UNDERDOCKS", "DARV", "OROBAS" };

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

						if (obtainEpochOverride != null)
						{
							obtainEpochOverride.Invoke(progress, new object[] { epochId, revealedEnum });
						}
					}
				}

				progress.MaxMultiplayerAscension = 10;

				var allChars = MegaCrit.Sts2.Core.Models.ModelDb.AllCharacters;
				var getStatsMethod = AccessTools.Method(typeof(ProgressState), "GetOrCreateCharacterStats");

				if (allChars != null && getStatsMethod != null)
				{
					foreach (var character in allChars)
					{
						// Skip known invalid characters just in case
						string charIdStr = character.Id.ToString();
						if (charIdStr.Contains("AUTOMATON") || charIdStr.Contains("AWAKENED") || charIdStr.Contains("CHAMP"))
							continue;

						var stats = getStatsMethod.Invoke(progress, new object[] { character.Id });
						if (stats != null)
						{
							var maxAscProp = AccessTools.PropertySetter(stats.GetType(), "MaxAscension");
							maxAscProp?.Invoke(stats, new object[] { 10 });
						}
					}
				}
                
                // Clear pending unlock
                var pendingProp = AccessTools.PropertySetter(progress.GetType(), "PendingCharacterUnlock");
				if (pendingProp != null)
				{
					pendingProp.Invoke(progress, new object[] { Activator.CreateInstance(AccessTools.TypeByName("MegaCrit.Sts2.Core.Models.ModelId")) });
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

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Saves.ProgressState), "GrantNextUnlock")]
internal static class ProgressStateGrantNextUnlockPatch
{
    private static bool Prefix(ref string? __result)
    {
        __result = null!;
        return false; // Skip original execution
    }
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Saves.ProgressState), "get_TotalUnlocks")]
internal static class ProgressStateTotalUnlocksPatch
{
    private static bool Prefix(ref int __result)
    {
        __result = 999999;
        return false; // Skip original execution
    }
}
