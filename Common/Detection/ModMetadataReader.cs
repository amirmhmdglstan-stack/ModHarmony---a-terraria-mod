using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModHarmony.Common.Utilities;
using Terraria.ModLoader.Core;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Best-effort reader for installed .tmod files' embedded metadata ("Info"
/// stream). tModLoader keeps this format internal, so we mirror the wire format
/// (see BuildProperties.ReadFromStream). Any parse failure is caught and the mod
/// is simply left with less metadata — never a crash.
/// </summary>
public static class ModMetadataReader
{
	/// <summary>Reads metadata for a .tmod file at the given path. Returns null on any failure.</summary>
	public static InstalledModInfo Read(string filePath)
	{
		try {
			var file = new TmodFile(filePath);
			using (file.Open()) {
				if (!file.HasFile("Info"))
					return new InstalledModInfo { FileName = Path.GetFileName(filePath), Name = file.Name, ParseFailed = true };

				using var stream = file.GetStream("Info");
				if (stream == null)
					return new InstalledModInfo { FileName = Path.GetFileName(filePath), Name = file.Name, ParseFailed = true };

				using var reader = new BinaryReader(stream);
				var info = Parse(filePath, reader);
				info.Name = file.Name;
				return info;
			}
		}
		catch (Exception e) {
			Log.Debug($"Could not read metadata from {Path.GetFileName(filePath)}: {e.Message}");
			return new InstalledModInfo { FileName = Path.GetFileName(filePath), ParseFailed = true };
		}
	}

	private static InstalledModInfo Parse(string filePath, BinaryReader reader)
	{
		var info = new InstalledModInfo {
			FileName = Path.GetFileName(filePath)
		};

		for (string tag = reader.ReadString(); tag.Length > 0; tag = reader.ReadString()) {
			switch (tag) {
				case "dllReferences":
					ReadList(reader);
					break;
				case "modReferences":
					info.ModReferences.AddRange(ReadList(reader));
					break;
				case "weakReferences":
					info.WeakReferences.AddRange(ReadList(reader));
					break;
				case "sortAfter":
					info.SortAfter.AddRange(ReadList(reader));
					break;
				case "sortBefore":
					info.SortBefore.AddRange(ReadList(reader));
					break;
				case "author":
					info.Author = reader.ReadString();
					break;
				case "version":
					info.Version = reader.ReadString();
					break;
				case "displayName":
					info.DisplayName = reader.ReadString();
					break;
				case "homepage":
					info.Homepage = reader.ReadString();
					break;
				case "description":
					info.Description = reader.ReadString();
					break;
				case "noCompile":
				case "!playableOnPreview":
					break;
				case "translationMod":
					info.IsTranslationMod = true;
					break;
				case "!hideCode":
				case "!hideResources":
				case "includeSource":
				case "eacPath":
					if (tag == "eacPath")
						reader.ReadString();
					break;
				case "side":
					reader.ReadByte();
					break;
				case "buildVersion":
					reader.ReadString();
					break;
				case "modSource":
					reader.ReadString();
					break;
				default:
					// Unknown tag: skip a value if one exists. We cannot know its type,
					// so stop parsing rather than desync the stream.
					Log.Debug($"Unknown Info tag '{tag}' in {Path.GetFileName(filePath)}; metadata parse stopped.");
					return info;
			}
		}

		info.Name = Path.GetFileNameWithoutExtension(filePath);
		return info;
	}

	private static IEnumerable<string> ReadList(BinaryReader reader)
	{
		var list = new List<string>();
		for (string item = reader.ReadString(); item.Length > 0; item = reader.ReadString())
			list.Add(item);
		return list;
	}
}
