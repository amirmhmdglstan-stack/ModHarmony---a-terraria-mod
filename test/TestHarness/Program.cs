using System;
using System.Collections.Generic;
using System.Linq;
using ModHarmony.Common.Arbitration;
using ModHarmony.Common.Core;
using ModHarmony.Common.Detection;
using ModHarmony.Content.Config;

namespace ModHarmony.Tests;

/// <summary>
/// Automated tests for ModHarmony's detection logic. Runs the real detector
/// code against synthetic modpacks and asserts on the resulting conflicts.
/// Requires a .NET 8 SDK and the tModLoader reference assemblies (see
/// TestHarness.csproj). Exit code 0 = all tests passed.
/// </summary>
public static class Program
{
	private static int _passed;
	private static int _failed;

	public static int Main()
	{
		Run("Empty modpack produces no conflicts", TestEmptyPack);
		Run("Hook overlap: two mods overriding GlobalNPC.AI", TestHookOverlap);
		Run("Hook overlap escalates on 3+ shared damage hooks", TestHookEscalation);
		Run("Hook overlap aggregates for large contestant sets", TestAggregation);
		Run("Global class overlap (no shared hooks) is informational", TestGlobalClassOverlap);
		Run("ModPlayer overlap on item damage", TestModPlayerOverlap);
		Run("ModSystem overlap on world generation", TestModSystemOverlap);
		Run("Recipe overlap: same result from two mods", TestRecipeOverlap);
		Run("Recipe groups shared by multiple mods", TestRecipeGroup);
		Run("Dependency cycle detected with warning", TestDependencyCycle);
		Run("Missing optional dependency is informational", TestMissingOptional);
		Run("Asset duplicates detected", TestAssetDuplicates);
		Run("Health score is lowered by high-risk conflicts with breakdown", TestHealth);
		Run("Conflict ids are stable and distinct", TestStableIds);
		Run("Arbitration: seeded random is deterministic", TestSeededRandom);
		Run("Arbitration: manual priority ordering", TestManualPriority);
		Run("Arbitration: weighted random validation", TestWeightValidation);
		Run("Arbitration: lock keeps winner", TestLockKeepsWinner);

		Console.WriteLine();
		Console.WriteLine($"Passed: {_passed}, Failed: {_failed}");
		return _failed == 0 ? 0 : 1;
	}

	private static void Run(string name, Action test)
	{
		try {
			test();
			_passed++;
			Console.WriteLine($"  PASS  {name}");
		}
		catch (Exception e) {
			_failed++;
			Console.WriteLine($"  FAIL  {name}: {e.Message}");
		}
	}

	private static void Assert(bool condition, string message)
	{
		if (!condition)
			throw new Exception(message);
	}

	// ------------------------------------------------------------------ helpers

	private static ModFacts Mod(string name, int loadIndex = 0, params (string baseType, string hook, string system)[] hooks)
	{
		var facts = new ModFacts { Name = name, DisplayName = name, Version = new Version(1, 0), LoadIndex = loadIndex };
		foreach (var (baseType, hook, system) in hooks) {
			facts.Hooks.Add(new HookUse(baseType, hook, system, $"{name}.{baseType}"));
			facts.HookCounts.TryGetValue(system, out var n);
			facts.HookCounts[system] = n + 1;
			facts.GlobalClasses.Add(baseType);
		}
		return facts;
	}

	private static DetectorContext Context(params ModFacts[] mods)
	{
		var ctx = new DetectorContext { Config = new ModHarmonyConfig() };
		foreach (var m in mods) {
			ctx.Mods.Add(m);
			ctx.ByName[m.Name] = m;
		}
		ctx.RebuildSystemOverlapCounts();
		return ctx;
	}

	private static T Conflict<T>(List<Conflict> conflicts, string systemId, string detectorId) where T : class
	{
		var c = conflicts.FirstOrDefault(x => x.SystemId == systemId && x.DetectorId == detectorId);
		Assert(c != null, $"expected a conflict on '{systemId}' from '{detectorId}'");
		return c as T;
	}

	// ------------------------------------------------------------------ tests

	private static void TestEmptyPack()
	{
		var ctx = Context();
		var conflicts = new HookOverlapDetector().Detect(ctx);
		conflicts.AddRange(new GlobalClassOverlapDetector().Detect(ctx));
		conflicts.AddRange(new ModPlayerOverlapDetector().Detect(ctx));
		conflicts.AddRange(new ModSystemOverlapDetector().Detect(ctx));
		conflicts.AddRange(new RecipeDetector().Detect(ctx));
		conflicts.AddRange(new DependencyDetector().Detect(ctx));
		Assert(conflicts.Count == 0, "empty pack produced conflicts");
	}

	private static void TestHookOverlap()
	{
		var a = Mod("ModA", 0, ("GlobalNPC", "AI", "npc.ai"));
		var b = Mod("ModB", 1, ("GlobalNPC", "AI", "npc.ai"));
		var ctx = Context(a, b);

		var conflicts = new HookOverlapDetector().Detect(ctx);
		var c = conflicts.FirstOrDefault(x => x.SystemId == "npc.ai");
		Assert(c != null, "no npc.ai conflict");
		Assert(c.Severity == Severity.Medium, $"expected Medium, got {c.Severity}");
		Assert(c.Confidence == Confidence.Strong, $"expected Strong, got {c.Confidence}");
		Assert(c.Mods.Contains("ModA") && c.Mods.Contains("ModB"), "wrong mods");
		Assert(c.Evidence.Count >= 2, "expected per-mod hook evidence");
		Assert(c.Evidence.Any(e => e.Key == "HookOverlap.ModHooks"), "missing ModHooks evidence");
		Assert(!string.IsNullOrEmpty(c.Id), "missing conflict id");
	}

	private static void TestHookEscalation()
	{
		var a = Mod("ModA", 0,
			("GlobalNPC", "ModifyHitByProjectile", "npc.damage"),
			("GlobalNPC", "ModifyHitByItem", "npc.damage"),
			("GlobalNPC", "OnHitByProjectile", "npc.damage"));
		var b = Mod("ModB", 1,
			("GlobalNPC", "ModifyHitByProjectile", "npc.damage"),
			("GlobalNPC", "ModifyHitByItem", "npc.damage"),
			("GlobalNPC", "OnHitByProjectile", "npc.damage"));
		var ctx = Context(a, b);

		var c = new HookOverlapDetector().Detect(ctx).FirstOrDefault(x => x.SystemId == "npc.damage");
		Assert(c != null, "no npc.damage conflict");
		Assert(c.Severity == Severity.Significant, $"3+ shared hooks should escalate to Significant, got {c.Severity}");
	}

	private static void TestAggregation()
	{
		var mods = new List<ModFacts>();
		for (int i = 0; i < 10; i++)
			mods.Add(Mod($"Mod{i}", i, ("GlobalNPC", "AI", "npc.ai")));
		var ctx = Context(mods.ToArray());

		var conflicts = new HookOverlapDetector().Detect(ctx);
		var c = conflicts.FirstOrDefault(x => x.SystemId == "npc.ai");
		Assert(c != null, "no aggregated conflict");
		Assert(c.Mods.Count >= 10, "aggregated conflict should list all mods");
		Assert(conflicts.Count(x => x.SystemId == "npc.ai") == 1, "expected exactly one aggregated conflict, not 45 pairs");
		Assert(c.Evidence.Any(e => e.Key == "HookOverlap.Aggregate"), "missing aggregate evidence");
	}

	private static void TestGlobalClassOverlap()
	{
		var a = Mod("ModA", 0, ("GlobalNPC", "OnKill", "npc.stats"));
		var b = Mod("ModB", 1, ("GlobalNPC", "ModifyShop", "npc.shop"));
		var ctx = Context(a, b);

		var c = new GlobalClassOverlapDetector().Detect(ctx).FirstOrDefault(x => x.SystemId == "content.globalNPC");
		Assert(c != null, "no content.globalNPC conflict");
		Assert(c.Severity == Severity.Info, "class overlap should be informational");
		Assert(c.Confidence == Confidence.Confirmed, "class overlap is confirmed from content registry");
	}

	private static void TestModPlayerOverlap()
	{
		var a = Mod("ModA", 0, ("ModPlayer", "ModifyWeaponDamage", "item.damage"));
		var b = Mod("ModB", 1, ("ModPlayer", "ModifyWeaponDamage", "item.damage"));
		var ctx = Context(a, b);

		var c = new ModPlayerOverlapDetector().Detect(ctx).FirstOrDefault(x => x.SystemId == "item.damage");
		Assert(c != null, "no ModPlayer item.damage conflict");
		Assert(c.Severity == Severity.Medium, $"expected Medium, got {c.Severity}");
	}

	private static void TestModSystemOverlap()
	{
		var a = Mod("ModA", 0, ("ModSystem", "ModifyWorldGenTasks", "world.gen"));
		var b = Mod("ModB", 1, ("ModSystem", "ModifyWorldGenTasks", "world.gen"));
		var ctx = Context(a, b);

		var c = new ModSystemOverlapDetector().Detect(ctx).FirstOrDefault(x => x.SystemId == "world.gen");
		Assert(c != null, "no world.gen conflict");
		Assert(c.Severity == Severity.Medium, $"expected Medium, got {c.Severity}");
	}

	private static void TestRecipeOverlap()
	{
		var ctx = Context(Mod("ModA"), Mod("ModB"));
		ctx.Recipes.ByResult[1] = new List<RecipeSnapshot.RecipeEntry> {
			new() { ResultType = 1, ResultName = "Copper Bar", OwnerMod = "ModA", IngredientCount = 2 },
			new() { ResultType = 1, ResultName = "Copper Bar", OwnerMod = "ModB", IngredientCount = 3 }
		};

		var c = new RecipeDetector().Detect(ctx).FirstOrDefault(x => x.SystemId == "recipe.add");
		Assert(c != null, "no recipe.add conflict");
		Assert(c.Severity == Severity.Low, "recipe overlap should stay Low (common and usually intentional)");
		Assert(c.Confidence == Confidence.Confirmed, "recipe ownership is confirmed via Recipe.Mod");
		Assert(c.Evidence.Any(e => e.Key == "RecipeOverlap.ModRecipes"), "missing per-mod recipe evidence");
	}

	private static void TestRecipeGroup()
	{
		var ctx = Context(Mod("ModA"), Mod("ModB"));
		ctx.Recipes.GroupContributorMods[0] = new HashSet<string> { "ModA", "ModB" };

		var c = new RecipeDetector().Detect(ctx).FirstOrDefault(x => x.SystemId == "recipe.group");
		Assert(c != null, "no recipe.group conflict");
		Assert(c.Severity == Severity.Info, "shared groups should be informational");
	}

	private static void TestDependencyCycle()
	{
		var a = Mod("ModA");
		var b = Mod("ModB");
		a.Dependencies.Add("ModB");
		b.Dependencies.Add("ModA");
		var ctx = Context(a, b);

		var c = new DependencyDetector().Detect(ctx).FirstOrDefault(x => x.SystemId == "dependency.cycle");
		Assert(c != null, "no dependency.cycle conflict");
		Assert(ctx.LoadOrderWarnings.Count >= 1, "cycle should be recorded as a load-order warning");
	}

	private static void TestMissingOptional()
	{
		var a = Mod("ModA");
		a.MissingOptionalDependencies.Add("FixtureOptionalFriend");
		var ctx = Context(a);

		var c = new DependencyDetector().Detect(ctx).FirstOrDefault(x => x.SystemId == "dependency.optional");
		Assert(c != null, "no dependency.optional conflict");
		Assert(c.Severity == Severity.Info && c.IsConditional, "missing optional should be informational and conditional");
	}

	private static void TestAssetDuplicates()
	{
		var ctx = Context();
		ctx.InstalledMods.Add(new InstalledModInfo { Name = "ModA", FileName = "ModA.tmod" });
		ctx.InstalledMods.Add(new InstalledModInfo { Name = "ModA", FileName = "ModA_old.tmod" });

		var c = new AssetDetector().Detect(ctx).FirstOrDefault(x => x.SystemId == "asset.duplicate");
		Assert(c != null, "no asset.duplicate conflict");
		Assert(c.Severity == Severity.Medium && c.Confidence == Confidence.Confirmed, "duplicate files are confirmed medium risk");
	}

	private static void TestHealth()
	{
		var a = Mod("ModA", 0, ("GlobalNPC", "AI", "npc.ai"));
		var b = Mod("ModB", 1, ("GlobalNPC", "AI", "npc.ai"));
		var c = Mod("ModC", 2, ("GlobalNPC", "AI", "npc.ai"));
		var ctx = Context(a, b, c);
		var conflicts = new HookOverlapDetector().Detect(ctx);

		var health = HealthCalculator.Calculate(conflicts, ctx.SystemOverlapCounts, ctx.LoadOrderWarnings,
			new Dictionary<string, DetectorStatus>());
		Assert(health.Score < 100, "conflicts should lower the score");
		Assert(health.Breakdown.Count > 0, "breakdown should explain the deduction");
	}

	private static void TestStableIds()
	{
		var id1 = Conflict.MakeStableId("HookOverlap", "npc.ai", new[] { "ModB", "ModA" });
		var id2 = Conflict.MakeStableId("HookOverlap", "npc.ai", new[] { "ModA", "ModB" });
		var id3 = Conflict.MakeStableId("HookOverlap", "npc.ai", new[] { "ModA", "ModC" });
		Assert(id1 == id2, "ids should be independent of argument order");
		Assert(id1 != id3, "different mod sets should give different ids");
		Assert(id1.All(char.IsLetterOrDigit), "ids should be typable");
	}

	private static void TestSeededRandom()
	{
		var group = new ArbitrationGroup {
			GroupId = "system.npc.spawn",
			SystemId = "npc.spawn",
			Strategy = ArbitrationStrategy.Random,
			Seed = 12345,
			MechanismAvailable = true,
			Candidates = {
				new ArbitrationCandidate { ModName = "ModA", LoadIndex = 0 },
				new ArbitrationCandidate { ModName = "ModB", LoadIndex = 1 },
				new ArbitrationCandidate { ModName = "ModC", LoadIndex = 2 }
			}
		};
		var config = new ModHarmonyConfig();

		var w1 = ArbitrationManager.ResolveWinner(group, config);
		var w2 = ArbitrationManager.ResolveWinner(group, config);
		Assert(w1 == w2, "same seed must produce the same winner");
		Assert(new[] { "ModA", "ModB", "ModC" }.Contains(w1), "winner must be a candidate");

		group.Seed = 999;
		Assert(ArbitrationManager.EffectiveSeed(group, config) == 999, "explicit seed wins");
	}

	private static void TestManualPriority()
	{
		var group = new ArbitrationGroup {
			GroupId = "system.npc.damage",
			SystemId = "npc.damage",
			Strategy = ArbitrationStrategy.ManualPriority,
			MechanismAvailable = true,
			Candidates = {
				new ArbitrationCandidate { ModName = "ModA", ManualPriority = 1, LoadIndex = 0 },
				new ArbitrationCandidate { ModName = "ModB", ManualPriority = 5, LoadIndex = 1 }
			}
		};
		var winner = ArbitrationManager.ResolveWinner(group, new ModHarmonyConfig());
		Assert(winner == "ModB", "higher manual priority must win");
	}

	private static void TestWeightValidation()
	{
		var group = new ArbitrationGroup {
			GroupId = "system.npc.spawn",
			SystemId = "npc.spawn",
			Strategy = ArbitrationStrategy.WeightedRandom,
			Seed = 42,
			MechanismAvailable = true,
			Candidates = {
				new ArbitrationCandidate { ModName = "ModA", Weight = 0f },
				new ArbitrationCandidate { ModName = "ModB", Weight = 0f }
			}
		};
		Assert(ArbitrationManager.ValidateWeights(group) != null, "all-zero weights must be rejected");

		group.Candidates[0].Weight = 60f;
		group.Candidates[1].Weight = 40f;
		Assert(ArbitrationManager.ValidateWeights(group) == null, "valid weights accepted");

		// 60/40 should stay within the expected distribution over many rolls.
		var config = new ModHarmonyConfig();
		int a = 0;
		for (int i = 0; i < 2000; i++) {
			group.Seed = i;
			if (ArbitrationManager.ResolveWinner(group, config) == "ModA")
				a++;
		}
		Assert(a > 950 && a < 1450, $"60/40 split over 2000 seeded rolls expected ~1200, got {a}");
	}

	private static void TestLockKeepsWinner()
	{
		var group = new ArbitrationGroup {
			GroupId = "system.npc.spawn",
			SystemId = "npc.spawn",
			Strategy = ArbitrationStrategy.Random,
			Seed = 7,
			MechanismAvailable = true,
			Candidates = {
				new ArbitrationCandidate { ModName = "ModA", LoadIndex = 0 },
				new ArbitrationCandidate { ModName = "ModB", LoadIndex = 1 }
			}
		};
		var config = new ModHarmonyConfig();
		ArbitrationManager.Resolve(group, config);
		var winner = group.ResolvedWinner;
		Assert(!string.IsNullOrEmpty(winner), "a resolvable group must produce a winner");

		// Simulate "regenerate" — new seed, then resolve; winner may change but
		// must remain stable across repeated resolves with the same seed.
		group.Seed = 123;
		ArbitrationManager.Resolve(group, config);
		var afterSeed = group.ResolvedWinner;
		ArbitrationManager.Resolve(group, config);
		Assert(group.ResolvedWinner == afterSeed, "winner must not change per resolve call");
	}
}
