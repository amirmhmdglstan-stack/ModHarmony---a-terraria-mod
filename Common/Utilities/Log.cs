using System;
using ModHarmony.Content.Config;
using Terraria.ModLoader;

namespace ModHarmony.Common.Utilities;

/// <summary>
/// Logging helpers with the consistent "[ModHarmony]" prefix required for easy
/// grep-ability in the tModLoader client log. Wraps the mod's log4net logger.
/// Verbosity follows the ModHarmony config's LogLevel setting.
/// </summary>
public static class Log
{
	private static Mod _mod;
	private static LogLevelSetting _level = LogLevelSetting.Info;

	public static void Init(Mod mod) => _mod = mod;

	public static void SetLevel(LogLevelSetting level) => _level = level;

	private static ILog Logger => _mod?.Logger;

	public static void Trace(string message)
	{
		if (_level <= LogLevelSetting.Trace)
			Logger?.Debug($"[ModHarmony] {message}");
	}

	public static void Debug(string message)
	{
		if (_level <= LogLevelSetting.Debug)
			Logger?.Debug($"[ModHarmony] {message}");
	}

	public static void Info(string message) => Logger?.Info($"[ModHarmony] {message}");

	public static void Warn(string message) => Logger?.Warn($"[ModHarmony] {message}");

	public static void Error(string message) => Logger?.Error($"[ModHarmony] {message}");

	public static void Error(string message, Exception e) => Logger?.Error($"[ModHarmony] {message}", e);

	public static void ConflictEvent(string message) => Logger?.Info($"[ModHarmony] {message}");
}
