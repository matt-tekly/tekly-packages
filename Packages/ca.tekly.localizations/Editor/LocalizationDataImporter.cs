using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tekly.Localizations
{
	/// <summary>
	/// Imports a .jsonloc file containing a flat JSON object of id to format pairs into a LocalizationData asset.
	/// {
	///     "$menu.play": "Play",
	///     "$score.label": "Score: {score}"
	/// }
	/// </summary>
	[ScriptedImporter(2, "jsonloc")]
	public class LocalizationDataImporter : ScriptedImporter
	{
		private static readonly Regex s_keyRegex = new Regex(@"\{\s*(\$[A-Za-z0-9_.]+)\s*\}",
			RegexOptions.Compiled | RegexOptions.CultureInvariant);

		public override void OnImportAsset(AssetImportContext ctx)
		{
			var data = ScriptableObject.CreateInstance<LocalizationData>();

			try {
				data.Strings = Read(ctx.assetPath);
				ResolveReferences(data.Strings, ctx);
			} catch (Exception ex) {
				ctx.LogImportError($"Failed to parse localization file [{ctx.assetPath}]: {ex.Message}");
				data.Strings = Array.Empty<LocalizationStringData>();
			}

			ctx.AddObjectToAsset("LocalizationData", data);
			ctx.SetMainObject(data);
		}

		private static LocalizationStringData[] Read(string path)
		{
			var strings = new List<LocalizationStringData>();
			var seen = new HashSet<string>();

			using var stream = new StreamReader(path);
			using var reader = new JsonTextReader(stream);

			if (!reader.Read() || reader.TokenType != JsonToken.StartObject) {
				throw new JsonReaderException("Root must be a JSON object");
			}

			while (reader.Read()) {
				if (reader.TokenType == JsonToken.EndObject) {
					break;
				}

				if (reader.TokenType != JsonToken.PropertyName) {
					throw new JsonReaderException($"Unexpected token {reader.TokenType} at line {reader.LineNumber}");
				}

				var key = (string)reader.Value;
				var line = reader.LineNumber;

				if (!reader.Read() || reader.TokenType != JsonToken.String) {
					throw new JsonReaderException($"Value for [{key}] must be a string (line {line})");
				}

				if (!seen.Add(key)) {
					throw new JsonReaderException($"Duplicate key [{key}] (line {line})");
				}

				strings.Add(new LocalizationStringData {
					Id = key,
					Format = (string)reader.Value
				});
			}

			return strings.ToArray();
		}

		/// <summary>
		/// Replaces {$id} references with the format of the string whose Id is "$id".
		/// References are resolved recursively; missing keys and cycles are left as-is and reported.
		/// </summary>
		private static void ResolveReferences(LocalizationStringData[] strings, AssetImportContext ctx)
		{
			var raw = new Dictionary<string, string>();
			foreach (var stringData in strings) {
				raw[stringData.Id] = stringData.Format;
			}

			var resolved = new Dictionary<string, string>();
			var resolving = new HashSet<string>();

			string Resolve(string id)
			{
				if (resolved.TryGetValue(id, out var cached)) {
					return cached;
				}

				if (!resolving.Add(id)) {
					ctx.LogImportWarning($"Circular localization reference involving [{id}]");
					return raw[id];
				}

				var result = s_keyRegex.Replace(raw[id], m => {
					var key = m.Groups[1].Value;

					if (!raw.ContainsKey(key)) {
						ctx.LogImportWarning($"[{id}] references missing key [{key}]");
						return m.Value;
					}

					return Resolve(key);
				});

				resolving.Remove(id);
				resolved[id] = result;
				return result;
			}

			foreach (var stringData in strings) {
				stringData.Format = Resolve(stringData.Id);
			}
		}
	}
}
