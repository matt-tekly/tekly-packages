using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tekly.TwoD.Common;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tekly.TwoD.Cells
{
	public static class CellAnimationImporter
	{
		public static void Import(Sprite[] sprites, AsepriteData data, AssetImportContext ctx, PixelDataSettings pixelDataSettings)
		{
			var spriteMap = sprites.ToDictionary(x => x.name);
			
			var frameEvents = GatherEvents(data);

			var fileName = Path.GetFileNameWithoutExtension(ctx.assetPath);

			var cellSprite = ScriptableObject.CreateInstance<CellSprite>();
			cellSprite.name = fileName;

			var animations = new List<CellAnimation>();

			foreach (var tag in data.meta.frameTags) {
				var spriteAnimation = ScriptableObject.CreateInstance<CellAnimation>();
				spriteAnimation.name = tag.name;
				animations.Add(spriteAnimation);
				ctx.AddObjectToAsset(tag.name, spriteAnimation);

				var frameIndices = new List<int>();
				
				switch (tag.direction) {
					case "forward": {
						for (var i = tag.from; i <= tag.to; i++) {
							frameIndices.Add(i);
						}
						
						break;
					}
					case "pingpong": {
						for (var i = tag.from; i <= tag.to; i++) {
							frameIndices.Add(i);
						}
						
						for (var i = tag.to - 1; i > tag.from; i--) {
							frameIndices.Add(i);
						}
						break;
					}
				}
				
				spriteAnimation.Frames = CreateAnimationFrames(frameIndices, data.frames, spriteMap, frameEvents);
				
			}

			if (spriteMap.TryGetValue($"{fileName}_icon_00", out var iconSprite)) {
				cellSprite.Icon = iconSprite;
				
				var texture = TextureExtensions.GetSpriteTexture(iconSprite);
				if (pixelDataSettings.Generate) {
					cellSprite.PixelData = new SpritePixelData(texture, pixelDataSettings);	
				}
				
				ctx.AddObjectToAsset("main", cellSprite, texture);
			} else {
				var texture = TextureExtensions.GetSpriteTexture(sprites[0]);
				ctx.AddObjectToAsset("main", cellSprite, texture);
				cellSprite.Icon = sprites[0];
				
				if (pixelDataSettings.Generate) {
					cellSprite.PixelData = new SpritePixelData(texture, pixelDataSettings);	
				}
			}

			ctx.SetMainObject(cellSprite);
			
			cellSprite.Animations = animations.ToArray();
		}

		private static SpriteFrame[] CreateAnimationFrames(List<int> indices, AseFrame[] aseFrames,
			Dictionary<string, Sprite> spriteMap, Dictionary<int, string> frameEvents)
		{
			var frames = new SpriteFrame[indices.Count];
			
			var index = 0;
			foreach (var i in indices) {
				var frame = aseFrames[i];

				frames[index] = new SpriteFrame {
					Duration = frame.duration / 1000f,
					Sprite = spriteMap[frame.filename]
				};

				if (frameEvents.TryGetValue(i, out var evt)) {
					ProcessEvt(evt, ref frames[index]);
				}

				index++;
			}
			
			return frames;
		}

		private static Dictionary<int, string> GatherEvents(AsepriteData data)
		{
			try {
				return data.meta.layers
					.Where(x => x.cels != null)
					.SelectMany(x => x.cels)
					.ToDictionary(x => x.frame, x => x.data);
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
			
			return new Dictionary<int, string>();
		}

		private static void ProcessEvt(string evt, ref SpriteFrame spriteFrame)
		{
			if (evt.StartsWith("sfx:")) {
				spriteFrame.Event = new AudioEvt {
					Clip = evt.Substring(4)
				};
			} else {
				spriteFrame.Event = new TextEvt {
					Text = evt
				};
			}
		}
	}
}