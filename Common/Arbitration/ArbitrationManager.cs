using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ModHarmony.Common.Core;
using ModHarmony.Common.Utilities;
using ModHarmony.Content.Config;

namespace ModHarmony.Common.Arbitration;

/// <summary>
/// Resolves arbitration winners. The core rules:
///  • Resolution is deterministic for a given (group, strategy, seed, candidates).
///  • Random/WeightedRandom use a controlled <see cref="Random"/> seeded from
///    the group's seed (or, for "auto", from a stable hash of the group id and
///    the master config seed) — the winner does NOT change per frame.
///  • A locked group keeps its last winner until the player unlocks it.
///  • Decisions are logged through the mod logger with the [ModHarmony] prefix.
/// </summary>
public static class ArbitrationManager
{
	/// <summary>Recomputes every group's winner from its current state.</summary>
	public static void ResolveAll(List<ArbitrationGroup> groups, ModHarmonyConfig config)
	{
		foreach (var group in groups)
			Resolve(group, config);
	}

	public static void Resolve(ArbitrationGroup group, ModHarmonyConfig config)
	{
		group.ResolvedWinner = "";
		group.DecisionLog = "";

		if (!group.CanResolve) {
			if (group.Strategy == ArbitrationStrategy.Disabled)
				group.DecisionLog = "arbitration disabled";
			else if (!group.MechanismAvailable)
				group.DecisionLog = "no arbitration mechanism available for this system";
			else
				group.DecisionLog = "no candidates";
			return;
		}

		var winner = ResolveWinner(group, config);
		if (string.IsNullOrEmpty(winner)) {
			group.DecisionLog = "could not resolve a winner";
			return;
		}

		group.ResolvedWinner = winner;
		group.DecisionLog = $"{DateTime.Now:HH:mm:ss} strategy={group.Strategy} seed={EffectiveSeed(group, config)} winner={winner}";
		Log.ConflictEvent($"Arbitration group {group.GroupId}: {group.DecisionLog}");
	}

	public static string ResolveWinner(ArbitrationGroup group, ModHarmonyConfig config)
	{
		if (group.Candidates.Count == 0)
			return null;

		switch (group.Strategy) {
			case ArbitrationStrategy.ManualPriority:
				return group.Candidates
					.OrderByDescending(c => c.ManualPriority)
					.ThenBy(c => group.Candidates.IndexOf(c))
					.First().ModName;

			case ArbitrationStrategy.LoadOrder:
			case ArbitrationStrategy.FirstRegistered:
				return group.Candidates
					.OrderBy(c => c.LoadIndex)
					.ThenBy(c => group.Candidates.IndexOf(c))
					.First().ModName;

			case ArbitrationStrategy.LastRegistered:
				return group.Candidates
					.OrderByDescending(c => group.Candidates.IndexOf(c))
					.First().ModName;

			case ArbitrationStrategy.Random:
			case ArbitrationStrategy.WeightedRandom:
				return PickRandom(group, config);

			case ArbitrationStrategy.Disabled:
			default:
				return null;
		}
	}

	private static string PickRandom(ArbitrationGroup group, ModHarmonyConfig config)
	{
		var rng = new Random(EffectiveSeed(group, config));

		if (group.Strategy == ArbitrationStrategy.WeightedRandom) {
			var weights = group.Candidates.Select(c => c.Weight).ToArray();
			if (weights.All(w => w <= 0f)) {
				// Invalid weights (all zero/negative): fall back to uniform.
				return group.Candidates[rng.Next(group.Candidates.Count)].ModName;
			}
			foreach (var w in weights)
				if (w < 0f)
					return group.Candidates[rng.Next(group.Candidates.Count)].ModName;

			var total = weights.Sum();
			var roll = (float)(rng.NextDouble() * total);
			float acc = 0f;
			for (int i = 0; i < group.Candidates.Count; i++) {
				acc += weights[i];
				if (roll < acc)
					return group.Candidates[i].ModName;
			}
			return group.Candidates[^1].ModName;
		}

		return group.Candidates[rng.Next(group.Candidates.Count)].ModName;
	}

	/// <summary>
	/// Auto seed (-1) derives a deterministic seed from the group id and the
	/// master config seed, so the same pack + config always rolls the same
	/// winner until the player regenerates.
	/// </summary>
	public static int EffectiveSeed(ArbitrationGroup group, ModHarmonyConfig config)
	{
		if (group.Seed >= 0)
			return group.Seed;
		var material = $"ModHarmony-arbitration|{group.GroupId}|{config.RandomSeed}";
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(material));
		return BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
	}

	/// <summary>Validates weights; returns a human-readable problem or null when OK.</summary>
	public static string ValidateWeights(ArbitrationGroup group)
	{
		if (group.Candidates.Any(c => c.Weight < 0f))
			return "negative weight";
		if (group.Candidates.All(c => c.Weight <= 0f))
			return "all weights are zero";
		return null;
	}

	/// <summary>Rerolls Random/WeightedRandom with a fresh random seed and unlocks the group.</summary>
	public static void Regenerate(ArbitrationGroup group, ModHarmonyConfig config)
	{
		if (group.Strategy != ArbitrationStrategy.Random && group.Strategy != ArbitrationStrategy.WeightedRandom)
			return;
		group.Seed = new Random().Next(int.MaxValue);
		group.Locked = false;
		Resolve(group, config);
		Log.ConflictEvent($"Arbitration group {group.GroupId}: seed regenerated to {group.Seed}, winner={group.ResolvedWinner}");
	}
}
