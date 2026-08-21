using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ModHarmony.Common.Utilities;

namespace ModHarmony.Common.Detection;

/// <summary>
/// Best-effort reader for installed .tmod files' embedded metadata ("Info"
/// entry). tModLoader keeps the BuildProperties wire format internal, so we
/// mirror it here (see BuildProperties.ReadFromStream). The .tmod container is
/// a zip archive, so we read it with the standard library — no tModLoader
/// internals needed. Any parse failure is caught and the mod is simply left
/// with less metadata — never a crash.
/// </summary>
public static class ModMetadataReader
{
	/// <summary>Reads metadata for a .tmod file at the given path. Returns null on any failure.</summary>
	public static InstalledModInfo Read(string filePath)
	{
		try {
			var info = new InstalledModInfo {
				FileName = Path.GetFileName(filePath),
				Name = Path.GetFileNameWithoutExtension(filePath)
			};

			using var archive = ZipFile.OpenRead(filePath);
			var infoEntry = archive.GetEntry("Info");
			if (infoEntry == null) {
				info.ParseFailed = true;
				return info;
			}

			using var stream = infoEntry.Open();
			using var reader = new BinaryReader(stream);
			Parse(info, reader);
			return info;
		}
		catch (Exception e) {
			Log.Debug($"Could not read metadata from {Path.GetFileName(filePath)}: {e.Message}");
			return new InstalledModInfo { FileName = Path.GetFileName(filePath), ParseFailed = true };
		}
	}

	private static void Parse(InstalledModInfo info, BinaryReader reader)
	{
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
					break;
				case "eacPath":
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
					// Unknown tag: we cannot know the type of its value, so stop
					// parsing rather than desync the stream.
					Log.Debug($"Unknown Info tag '{tag}' in {info.FileName}; metadata parse stopped.");
					return;
			}
		}
	}

	private static IEnumerable<string> ReadList(BinaryReader reader)
	{
		var list = new List<string>();
		for (string item = reader.ReadString(); item.Length > 0; item = reader.ReadString())
			list.Add(item);
		return list;
	}
}
