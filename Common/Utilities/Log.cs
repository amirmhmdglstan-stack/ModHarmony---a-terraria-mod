using System;
using System.Collections.Generic;
using System.Reflection;
using ModHarmony.Content.Config;
using Terraria.ModLoader;

namespace ModHarmony.Common.Utilities;

/// <summary>
/// Logging helpers with the consistent "[ModHarmony]" prefix required for easy
/// grep-ability in the tModLoader client log.
///
/// The logger is resolved from <see cref="Mod.Logger"/> at load time and invoked
/// through reflection with cached MethodInfos. This deliberately avoids naming
/// the logger's interface type (log4net's ILog), which is not exposed to the
/// mod compiler in every tModLoader build, so ModHarmony compiles on both
/// stable and preview without depending on log4net.
/// </summary>
public static class Log
{
	private static object _logger;
	private static readonly Dictionary<string, MethodInfo> Methods = new();
	private static LogLevelSetting _level = LogLevelSetting.Info;

	public static void Init(Mod mod) => _logger = mod?.Logger;

	public static void Reset()
	{
		_logger = null;
		Methods.Clear();
	}

	public static void SetLevel(LogLevelSetting level) => _level = level;

	public static void Trace(string message)
	{
		if (_level <= LogLevelSetting.Trace)
			LogAt("Debug", message);
	}

	public static void Debug(string message)
	{
		if (_level <= LogLevelSetting.Debug)
			LogAt("Debug", message);
	}

	public static void Info(string message) => LogAt("Info", message);

	public static void Warn(string message) => LogAt("Warn", message);

	public static void Error(string message) => LogAt("Error", message);

	public static void Error(string message, Exception e) => LogAt("Error", message, e);

	public static void ConflictEvent(string message) => LogAt("Info", message);

	/// <summary>Invokes the underlying logger method (Debug/Info/Warn/Error) via cached reflection. Never throws.</summary>
	private static void LogAt(string level, string message, Exception ex = null)
	{
		var logger = _logger;
		if (logger == null)
			return;

		try {
			string key = ex != null ? level + ":ex" : level;
			if (!Methods.TryGetValue(key, out var method)) {
				var type = logger.GetType();
				method = ex != null
					? type.GetMethod(level, new[] { typeof(object), typeof(Exception) })
					: type.GetMethod(level, new[] { typeof(object) });
				Methods[key] = method;
			}
			if (method == null)
				return;

			var text = "[ModHarmony] " + message;
			if (ex != null)
				method.Invoke(logger, new object[] { text, ex });
			else
				method.Invoke(logger, new object[] { text });
		}
		catch {
			// Logging must never break the game.
		}
	}
}
