using System.Collections.Generic;
using System.IO;
using System.Text;
using Tekly.Common.Ui.Fancy;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui.Styled
{
	/// <summary>
	/// Replaces FancyRect components with StyledRects that look the same, and reports anything that has no
	/// equivalent. A conversion is planned first (what the StyledRect would be, plus notes) and then applied,
	/// so the report can list every FancyRect in the project without changing anything.
	///
	/// References to the FancyRect from the same prefab or scene are pointed at the StyledRect. A field typed
	/// FancyRect cannot hold one, so code that names the type has to change with its prefabs.
	///
	/// A FancyRect is replaced in the prefab that owns it. A variant or an outer prefab that overrides values
	/// on it would lose them, since the overrides point at a component that is gone. ConvertPrefabs therefore
	/// works in three passes: plan every overridden occurrence, convert the owning prefabs, then set the
	/// planned result on the outer prefabs, where whatever differs is saved as overrides on the new component.
	/// </summary>
	public static class StyledRectConverter
	{
		private const string MENU = "Tools/Tekly/Styled Rect/";

		/// <summary>What a FancyRect becomes: the StyledRect's values and layers, front to back.</summary>
		public class Plan
		{
			public StyledRadiusMode RadiusMode;
			public float Radius;
			public Vector4 CornerRadii;
			public StyledCorners RoundedCorners;
			public float CornerSmoothing;
			public StyledBlendMode BlendMode;
			public bool RaycastUsesShape;
			public Sprite Sprite;
			public StyledTextureMode TextureMode;
			public Vector2 TextureScale;
			public Vector2 TextureOffset;
			public float TextureRotation;
			public Vector2 TextureScroll;
			public readonly List<StyledLayer> Layers = new List<StyledLayer>();
			public readonly List<string> Notes = new List<string>();
		}

		[MenuItem(MENU + "Report FancyRects In Project")]
		private static void ReportMenu()
		{
			Debug.Log(Report());
		}

		[MenuItem(MENU + "Convert All Prefabs In Project")]
		private static void ConvertProjectMenu()
		{
			var paths = FindPrefabsWithFancyRects();

			if (!EditorUtility.DisplayDialog("Convert FancyRects",
				    $"Replaces every FancyRect in {paths.Count} prefabs with a StyledRect and saves them. Scenes are not touched. Commit first, so this can be undone.",
				    "Convert", "Cancel")) {
				return;
			}

			var log = new StringBuilder();
			var converted = ConvertPrefabs(paths, log);
			Debug.Log($"[StyledRectConverter] Converted [{converted}] FancyRects in [{paths.Count}] prefabs\n{log}");
		}

		[MenuItem(MENU + "Convert Selected FancyRects")]
		private static void ConvertSelectionMenu()
		{
			var log = new StringBuilder();
			var converted = 0;

			foreach (var selected in Selection.gameObjects) {
				if (PrefabUtility.IsPartOfPrefabAsset(selected)) {
					converted += ConvertPrefabAsset(AssetDatabase.GetAssetPath(selected), log);
					continue;
				}

				foreach (var fancy in selected.GetComponentsInChildren<FancyRect>(true)) {
					if (ConvertInPlace(fancy, true, log) != null) {
						converted++;
					}
				}
			}

			Debug.Log($"[StyledRectConverter] Converted [{converted}] FancyRects\n{log}");
		}

		/// <summary>
		/// Lists every FancyRect in the project's prefabs with what would not carry over, and counts the ones in
		/// scenes. Changes nothing.
		/// </summary>
		public static string Report()
		{
			var report = new StringBuilder();
			var total = 0;
			var withNotes = 0;
			var prefabCount = 0;

			foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets", "Packages" })) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				if (root == null) {
					continue;
				}

				var fancies = root.GetComponentsInChildren<FancyRect>(true);

				if (fancies.Length == 0) {
					continue;
				}

				prefabCount++;

				foreach (var fancy in fancies) {
					total++;
					var plan = BuildPlan(fancy);

					// A FancyRect that comes from a nested prefab or a variant's base converts with that prefab.
					// It only needs attention here when this prefab overrides something on it.
					if (IsInNestedPrefab(fancy)) {
						plan.Notes.Clear();
						var overrides = GetOverriddenProperties(fancy);

						if (overrides.Count > 0) {
							plan.Notes.Add($"Comes from another prefab and overrides [{overrides.Count}] values here, which are lost when that prefab converts: {string.Join(", ", overrides)}");
						}
					}

					if (plan.Notes.Count == 0) {
						continue;
					}

					withNotes++;
					report.AppendLine($"{path} :: {GetPath(fancy.transform, root.transform)}");

					foreach (var note in plan.Notes) {
						report.AppendLine($"    - {note}");
					}
				}
			}

			var sceneLines = CountInScenes();

			return $"[StyledRectConverter] [{total}] FancyRects in [{prefabCount}] prefabs, [{withNotes}] with notes\n{report}\nScenes (not inspected, open one and convert its selection):\n{sceneLines}";
		}

		/// <summary>A FancyRect seen through a nested prefab or variant, with what this prefab wants it to be.</summary>
		private class Occurrence
		{
			public string Name;
			public int[] ObjectPath;
			public Plan Plan;
			public bool Enabled;
			public Color Color;
			public bool RaycastTarget;
			public Vector4 RaycastPadding;
			public bool Maskable;
		}

		/// <summary>A reference set in this prefab that points at a FancyRect owned by another prefab.</summary>
		private class OuterReference
		{
			public int[] OwnerPath;
			public int OwnerComponentIndex;
			public string PropertyPath;
			public int[] TargetPath;
		}

		/// <summary>Every prefab in the project that shows a FancyRect, its own or one from a nested prefab.</summary>
		public static List<string> FindPrefabsWithFancyRects()
		{
			var paths = new List<string>();

			foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets", "Packages" })) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				if (root != null && root.GetComponentInChildren<FancyRect>(true) != null) {
					paths.Add(path);
				}
			}

			return paths;
		}

		/// <summary>
		/// Converts the FancyRects in a set of prefabs and keeps what variants and outer prefabs override on
		/// them. Give it every prefab that shows the FancyRects being converted: an outer prefab left out
		/// loses its overrides. Returns how many FancyRects were replaced.
		/// </summary>
		public static int ConvertPrefabs(IReadOnlyList<string> paths, StringBuilder log)
		{
			// Prefabs that nest others come after the ones they nest, so an outer prefab sets its values
			// against sources that are already final.
			var ordered = new List<string>(paths);
			ordered.Sort((a, b) => CountPrefabDependencies(a).CompareTo(CountPrefabDependencies(b)));

			// Pass 1: while the FancyRects still exist, note what each outer prefab makes of the ones it overrides.
			var occurrences = new Dictionary<string, List<Occurrence>>();
			var outerReferences = new Dictionary<string, List<OuterReference>>();

			foreach (var path in ordered) {
				var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				occurrences[path] = PlanOccurrences(root);
				outerReferences[path] = FindOuterReferences(root);
			}

			// Pass 2: replace each FancyRect in the prefab that owns it.
			var converted = 0;

			foreach (var path in ordered) {
				converted += ConvertPrefabAsset(path, log);
			}

			// Pass 3: set the planned result on the outer prefabs. What differs from the source is saved as overrides.
			foreach (var path in ordered) {
				if (occurrences[path].Count == 0 && outerReferences[path].Count == 0) {
					continue;
				}

				ApplyOccurrences(path, occurrences[path], outerReferences[path], log);
			}

			AssetDatabase.SaveAssets();
			return converted;
		}

		private static List<Occurrence> PlanOccurrences(GameObject root)
		{
			var occurrences = new List<Occurrence>();

			foreach (var fancy in root.GetComponentsInChildren<FancyRect>(true)) {
				if (!IsInNestedPrefab(fancy) || GetOverriddenProperties(fancy).Count == 0) {
					continue;
				}

				occurrences.Add(new Occurrence {
					Name = GetPath(fancy.transform, root.transform),
					ObjectPath = GetIndexPath(fancy.transform, root.transform),
					Plan = BuildPlan(fancy),
					Enabled = fancy.enabled,
					Color = fancy.color,
					RaycastTarget = fancy.raycastTarget,
					RaycastPadding = fancy.raycastPadding,
					Maskable = fancy.maskable
				});
			}

			return occurrences;
		}

		private static List<OuterReference> FindOuterReferences(GameObject root)
		{
			var references = new List<OuterReference>();

			foreach (var component in root.GetComponentsInChildren<Component>(true)) {
				if (component == null) {
					continue;
				}

				var ownedHere = !PrefabUtility.IsPartOfPrefabInstance(component) || PrefabUtility.IsAddedComponentOverride(component);
				var property = new SerializedObject(component).GetIterator();

				while (property.Next(true)) {
					if (property.propertyType != SerializedPropertyType.ObjectReference || !(property.objectReferenceValue is FancyRect target)) {
						continue;
					}

					// Only references this prefab sets, to a FancyRect this prefab does not own. The rest are
					// handled when their own prefab converts.
					if (!IsInNestedPrefab(target) || !(ownedHere || property.prefabOverride)) {
						continue;
					}

					references.Add(new OuterReference {
						OwnerPath = GetIndexPath(component.transform, root.transform),
						OwnerComponentIndex = System.Array.IndexOf(component.GetComponents<Component>(), component),
						PropertyPath = property.propertyPath,
						TargetPath = GetIndexPath(target.transform, root.transform)
					});
				}
			}

			return references;
		}

		private static void ApplyOccurrences(string path, List<Occurrence> occurrences, List<OuterReference> references, StringBuilder log)
		{
			var root = PrefabUtility.LoadPrefabContents(path);

			try {
				foreach (var occurrence in occurrences) {
					var target = FindByIndexPath(root.transform, occurrence.ObjectPath);
					var styled = target != null ? target.GetComponent<StyledRect>() : null;

					if (styled == null) {
						log?.AppendLine($"{path} :: {occurrence.Name}: overrides not restored, no StyledRect was found there. Was its source prefab in the set?");
						continue;
					}

					styled.color = occurrence.Color;
					styled.raycastTarget = occurrence.RaycastTarget;
					styled.raycastPadding = occurrence.RaycastPadding;
					styled.maskable = occurrence.Maskable;
					Apply(occurrence.Plan, styled);
					styled.enabled = occurrence.Enabled;

					log?.AppendLine($"{path} :: {occurrence.Name}: overrides restored");

					foreach (var note in occurrence.Plan.Notes) {
						log?.AppendLine($"    - {note}");
					}
				}

				foreach (var reference in references) {
					var owner = FindByIndexPath(root.transform, reference.OwnerPath);
					var target = FindByIndexPath(root.transform, reference.TargetPath);
					var styled = target != null ? target.GetComponent<StyledRect>() : null;
					var components = owner != null ? owner.GetComponents<Component>() : null;

					if (styled == null || components == null || reference.OwnerComponentIndex >= components.Length) {
						log?.AppendLine($"{path}: a reference [{reference.PropertyPath}] could not be restored");
						continue;
					}

					var component = components[reference.OwnerComponentIndex];
					var serialized = new SerializedObject(component);
					var property = serialized.FindProperty(reference.PropertyPath);
					property.objectReferenceValue = styled;
					serialized.ApplyModifiedPropertiesWithoutUndo();

					if (property.objectReferenceValue != styled) {
						log?.AppendLine($"{path}: reference lost, [{component.GetType().Name}.{reference.PropertyPath}] on [{component.name}] is typed FancyRect and cannot hold a StyledRect");
					}
				}

				PrefabUtility.SaveAsPrefabAsset(root, path);
			} finally {
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		private static int CountPrefabDependencies(string path)
		{
			var count = 0;

			foreach (var dependency in AssetDatabase.GetDependencies(path, true)) {
				if (dependency != path && dependency.EndsWith(".prefab")) {
					count++;
				}
			}

			return count;
		}

		/// <summary>Where a transform sits under a root, as the sibling index at each level. Survives duplicate names.</summary>
		private static int[] GetIndexPath(Transform transform, Transform root)
		{
			var indexes = new List<int>();

			while (transform != root && transform.parent != null) {
				indexes.Insert(0, transform.GetSiblingIndex());
				transform = transform.parent;
			}

			return indexes.ToArray();
		}

		private static Transform FindByIndexPath(Transform root, int[] indexes)
		{
			var transform = root;

			foreach (var index in indexes) {
				if (index >= transform.childCount) {
					return null;
				}

				transform = transform.GetChild(index);
			}

			return transform;
		}

		/// <summary>Converts every FancyRect that belongs to a prefab asset itself, and saves it. Returns how many were converted.</summary>
		public static int ConvertPrefabAsset(string path, StringBuilder log)
		{
			var root = PrefabUtility.LoadPrefabContents(path);
			var converted = 0;

			try {
				foreach (var fancy in root.GetComponentsInChildren<FancyRect>(true)) {
					if (ConvertInPlace(fancy, false, log) != null) {
						converted++;
					}
				}

				if (converted > 0) {
					PrefabUtility.SaveAsPrefabAsset(root, path);
				}
			} finally {
				PrefabUtility.UnloadPrefabContents(root);
			}

			return converted;
		}

		/// <summary>
		/// Replaces one FancyRect with a StyledRect on the same object, in the same place in the component list,
		/// and points references to it at the new component. Returns null when it was skipped.
		/// </summary>
		public static StyledRect ConvertInPlace(FancyRect fancy, bool useUndo, StringBuilder log)
		{
			var gameObject = fancy.gameObject;
			var name = GetPath(fancy.transform, fancy.transform.root);

			if (IsInNestedPrefab(fancy)) {
				log?.AppendLine($"{name}: skipped, it belongs to a nested prefab. Convert that prefab");
				return null;
			}

			var plan = BuildPlan(fancy);
			var references = FindReferences(fancy);
			var componentIndex = System.Array.IndexOf(gameObject.GetComponents<Component>(), fancy);

			// What the component and the Graphic themselves carry.
			var enabled = fancy.enabled;
			var color = fancy.color;
			var raycastTarget = fancy.raycastTarget;
			var raycastPadding = fancy.raycastPadding;
			var maskable = fancy.maskable;

			if (useUndo) {
				Undo.DestroyObjectImmediate(fancy);
			} else {
				Object.DestroyImmediate(fancy, true);
			}

			if (fancy != null) {
				log?.AppendLine($"{name}: skipped, another component on the object needs the FancyRect");
				return null;
			}

			var styled = useUndo ? Undo.AddComponent<StyledRect>(gameObject) : gameObject.AddComponent<StyledRect>();

			styled.color = color;
			styled.raycastTarget = raycastTarget;
			styled.raycastPadding = raycastPadding;
			styled.maskable = maskable;
			Apply(plan, styled);
			styled.enabled = enabled;

			// Back to where the FancyRect sat among the object's components.
			while (System.Array.IndexOf(gameObject.GetComponents<Component>(), styled) > componentIndex && ComponentUtility.MoveComponentUp(styled)) {
			}

			foreach (var (owner, propertyPath) in references) {
				var serialized = new SerializedObject(owner);
				var property = serialized.FindProperty(propertyPath);
				property.objectReferenceValue = styled;
				serialized.ApplyModifiedPropertiesWithoutUndo();

				if (property.objectReferenceValue != styled) {
					plan.Notes.Add($"Reference lost: [{owner.GetType().Name}.{propertyPath}] on [{owner.name}] is typed FancyRect and cannot hold a StyledRect");
				}
			}

			EditorUtility.SetDirty(styled);

			if (log != null) {
				log.AppendLine($"{name}: converted, {plan.Layers.Count} layers, {references.Count} references");

				foreach (var note in plan.Notes) {
					log.AppendLine($"    - {note}");
				}
			}

			return styled;
		}

		/// <summary>Works out the StyledRect for a FancyRect. Changes nothing.</summary>
		public static Plan BuildPlan(FancyRect fancy)
		{
			var plan = new Plan {
				RadiusMode = (StyledRadiusMode)(int)fancy.RadiusMode,
				Radius = fancy.Radius,
				CornerRadii = fancy.CornerRadii,
				RoundedCorners = (StyledCorners)(int)fancy.RoundedCorners,
				RaycastUsesShape = fancy.RaycastUsesShape,
				Sprite = fancy.Sprite,
				TextureScale = fancy.TextureScale,
				TextureOffset = fancy.TextureOffset,
				TextureRotation = fancy.TextureRotation,
				TextureScroll = fancy.TextureScroll
			};

			PlanCorners(fancy, plan);
			PlanElement(fancy, plan);
			PlanLayers(fancy, plan);

			return plan;
		}

		private static void PlanCorners(FancyRect fancy, Plan plan)
		{
			var types = fancy.RadiusMode == RadiusMode.PerCorner ? fancy.CornerTypes : new CornerTypes(fancy.CornerType);
			var all = new[] { types.TopLeft, types.TopRight, types.BottomRight, types.BottomLeft };
			var squircles = 0;

			foreach (var type in all) {
				if (type == CornerType.Squircle) {
					squircles++;
				} else if (type != CornerType.Round) {
					plan.Notes.Add($"Corner type [{type}] has no equivalent and becomes a round corner");
				}
			}

			plan.CornerSmoothing = squircles > 0 ? 1f : 0f;

			if (squircles > 0 && squircles < all.Length) {
				plan.Notes.Add("Squircle on some corners only: Corner Smoothing applies to every corner");
			}

			if (fancy.Bulge != Vector4.zero) {
				plan.Notes.Add("Bulge has no equivalent and is dropped");
			}
		}

		private static void PlanElement(FancyRect fancy, Plan plan)
		{
			switch (fancy.BlendMode) {
				case ShapeBlendMode.Normal:
					plan.BlendMode = StyledBlendMode.Normal;
					break;
				case ShapeBlendMode.Additive:
					plan.BlendMode = StyledBlendMode.Additive;
					break;
				case ShapeBlendMode.Multiply:
					plan.BlendMode = StyledBlendMode.Multiply;
					break;
				case ShapeBlendMode.Screen:
					plan.BlendMode = StyledBlendMode.Screen;
					break;
				default:
					plan.Notes.Add($"Blend mode [{fancy.BlendMode}] has no equivalent and becomes Normal");
					break;
			}

			if (fancy.material != fancy.defaultMaterial) {
				plan.Notes.Add("A custom material is assigned. It uses the Fancy Rect shader and is not carried over");
			}

			if (fancy.RevealMethod != RevealMethod.None) {
				plan.Notes.Add("Reveal has no equivalent yet and is dropped");
			}

			if (fancy.Sprite == null) {
				return;
			}

			plan.TextureMode = fancy.TextureMode == ShapeTextureMode.Tile ? StyledTextureMode.Tile : StyledTextureMode.Cover;

			if (fancy.TextureMode == ShapeTextureMode.Stretch) {
				plan.Notes.Add("Texture mode Stretch becomes Cover, which keeps the sprite's aspect ratio");
			}
		}

		/// <summary>
		/// Layers are collected back to front in FancyRect's fixed order (drop shadows, background, fill, inner
		/// shadows, bevel, gloss, outlines), then reversed, since a StyledRect lists its front layer first.
		/// </summary>
		private static void PlanLayers(FancyRect fancy, Plan plan)
		{
			var tint = fancy.color;
			var tinted = fancy.RendererTintLayers;
			var hasSprite = fancy.Sprite != null;

			// A FancyRect shadow shows through a see-through body. A StyledRect cuts it out unless told not to.
			var bodyOpaque = tint.a >= 1f
			                 && ((fancy.FillEnabled && !hasSprite && IsOpaque(fancy.Fill)) || (fancy.BackgroundEnabled && IsOpaque(fancy.Background)));

			var layers = plan.Layers;

			for (var i = 0; i < fancy.ShadowCount; i++) {
				var shadow = fancy.GetShadow(i);

				if (shadow.Inset) {
					continue;
				}

				var layer = StyledLayer.DropShadow(shadow.Offset, shadow.Softness * 0.5f, ToPaint(shadow.Paint, plan), shadow.Spread);
				layer.Enabled = shadow.Enabled;
				layer.Textured = shadow.UseTexture;
				layer.ShowBehind = !bodyOpaque;
				layers.Add(WithTint(layer, FancyRectLayers.DropShadows, tinted, tint));

				if (shadow.Enabled && shadow.Tuck) {
					plan.Notes.Add("A drop shadow used Tuck, which is dropped. If its blur now shows past the shape, give it a small negative Spread");
				}
			}

			if (fancy.BackgroundEnabled) {
				layers.Add(WithTint(StyledLayer.Fill(ToPaint(fancy.Background, plan)), FancyRectLayers.Background, tinted, tint));
			}

			if (fancy.FillEnabled) {
				var fill = StyledLayer.Fill(ToPaint(fancy.Fill, plan));
				fill.Textured = hasSprite;
				layers.Add(WithTint(fill, FancyRectLayers.Fill, tinted, tint));
			}

			for (var i = 0; i < fancy.ShadowCount; i++) {
				var shadow = fancy.GetShadow(i);

				if (!shadow.Inset) {
					continue;
				}

				var layer = StyledLayer.InnerShadow(shadow.Offset, shadow.Softness * 0.5f, ToPaint(shadow.Paint, plan), Mathf.Max(shadow.Spread, 0));
				layer.Enabled = shadow.Enabled;
				layer.Textured = shadow.UseTexture;
				layers.Add(WithTint(layer, FancyRectLayers.InnerShadows, tinted, tint));
			}

			var bevel = fancy.Bevel;

			if (bevel.Enabled && bevel.Width > 0f) {
				var layer = StyledLayer.Bevel(bevel.Width, bevel.Softness * 0.5f, bevel.Highlight, bevel.Shadow, bevel.LightAngle);
				layers.Add(WithTint(layer, FancyRectLayers.Bevel, tinted, tint));
			}

			var gloss = fancy.Gloss;

			if (gloss.Enabled && gloss.Color.a > 0f) {
				layers.Add(WithTint(PlanGloss(fancy, gloss, plan), FancyRectLayers.Gloss, tinted, tint));
			}

			for (var i = 0; i < fancy.OutlineCount; i++) {
				var outline = fancy.GetOutline(i);
				var layer = StyledLayer.Stroke(outline.Width, ToPaint(outline.Paint, plan), (StyledStrokeAlign)(int)outline.Alignment);
				layer.Enabled = outline.Enabled;
				layer.Offset = outline.Offset;
				layer.Textured = outline.UseTexture;

				switch (outline.Style) {
					case OutlineStyle.Dashes:
						layer = layer.WithDashes(outline.Dashes.Spacing, outline.Dashes.Length, outline.Dashes.RoundCaps, outline.Dashes.Speed);
						break;
					case OutlineStyle.Wobble:
						layer = layer.WithWobble(outline.Wobble.Thickness, outline.Wobble.Drift, outline.Wobble.Wavelength, outline.Wobble.Seed);
						break;
					case OutlineStyle.Notches:
						if (outline.Enabled) {
							plan.Notes.Add("An outline used the Notches style, which has no equivalent. It becomes a solid stroke");
						}

						break;
				}

				layers.Add(WithTint(layer, FancyRectLayers.Outlines, tinted, tint));
			}

			layers.Reverse();
		}

		/// <summary>
		/// A FancyRect gloss is a copy of the shape inset from the top and sides, cut off partway down, that fades
		/// toward its bottom. A StyledRect gloss is an inset fill whose gradient runs over the whole shape, so
		/// the gradient's range is placed where the old gloss started and ended. That depends on the rect's height.
		/// </summary>
		private static StyledLayer PlanGloss(FancyRect fancy, ShapeGloss gloss, Plan plan)
		{
			var inset = Mathf.Max(0f, gloss.Inset);
			var height = Mathf.Abs(fancy.rectTransform.rect.height);
			var insetFraction = height > 0.001f ? Mathf.Clamp01(inset / height) : 0f;

			var faded = gloss.Color;
			faded.a *= 1f - Mathf.Clamp01(gloss.Fade);

			var paint = StyledPaint.Linear(faded, gloss.Color, 0);
			paint.RangeEnd = 1f - insetFraction;
			paint.RangeStart = Mathf.Clamp01(paint.RangeEnd - (1f - 2f * insetFraction) * Mathf.Clamp01(gloss.Height));

			if (inset > 0f) {
				plan.Notes.Add($"Gloss: the gradient range was worked out for a height of [{height:0.#}]. If the rect resizes, check it");
			}

			if (gloss.Fade < 1f) {
				plan.Notes.Add("Gloss with Fade below 1: FancyRect cut the gloss off at its height, a StyledRect carries its faded color on below");
			}

			var layer = StyledLayer.Fill(paint);
			layer.Offset = inset;
			layer.Blur = gloss.Softness * 0.5f;
			return layer;
		}

		private static StyledPaint ToPaint(ShapePaint paint, Plan plan)
		{
			var gradient = StyledGradient.None;

			switch (paint.Gradient) {
				case GradientType.Linear:
					gradient = StyledGradient.Linear;
					break;
				case GradientType.Radial:
					gradient = StyledGradient.Radial;
					break;
				case GradientType.Edge:
					gradient = StyledGradient.Edge;
					break;
				case GradientType.Angular:
					gradient = StyledGradient.Linear;
					plan.Notes.Add("An Angular gradient has no equivalent and becomes Linear");
					break;
			}

			return new StyledPaint {
				Color = paint.Color,
				Gradient = gradient,
				Color2 = paint.Color2,
				Angle = paint.Angle,
				RangeStart = paint.RangeStart,
				RangeEndInset = paint.RangeEndInset,
				Bias = gradient == StyledGradient.None ? 0f : paint.Bias
			};
		}

		/// <summary>
		/// FancyRect multiplied every layer by its own color and let layers opt out of the color applied from
		/// outside. A StyledRect has one tint, so a layer that opted out ignores it and takes the FancyRect's
		/// own color into its paint, which leaves it looking the same.
		/// </summary>
		private static StyledLayer WithTint(StyledLayer layer, FancyRectLayers kind, FancyRectLayers tinted, Color tint)
		{
			if ((tinted & kind) != 0) {
				return layer;
			}

			var rgb = new Color(tint.r, tint.g, tint.b, 1f);
			layer.IgnoreTint = true;
			layer.Paint.Color *= rgb;
			layer.Paint.Color2 *= rgb;
			return layer;
		}

		private static bool IsOpaque(ShapePaint paint)
		{
			return paint.Color.a >= 1f && (paint.Gradient == GradientType.None || paint.Color2.a >= 1f);
		}

		private static void Apply(Plan plan, StyledRect styled)
		{
			styled.RadiusMode = plan.RadiusMode;
			styled.Radius = plan.Radius;
			styled.CornerRadii = plan.CornerRadii;
			styled.RoundedCorners = plan.RoundedCorners;
			styled.CornerSmoothing = plan.CornerSmoothing;
			styled.BlendMode = plan.BlendMode;
			styled.RaycastUsesShape = plan.RaycastUsesShape;
			styled.Sprite = plan.Sprite;
			styled.TextureMode = plan.TextureMode;
			styled.TextureScale = plan.TextureScale;
			styled.TextureOffset = plan.TextureOffset;
			styled.TextureRotation = plan.TextureRotation;
			styled.TextureScroll = plan.TextureScroll;

			while (styled.LayerCount > 0) {
				styled.RemoveLayer(0);
			}

			foreach (var layer in plan.Layers) {
				styled.InsertLayer(styled.LayerCount, layer);
			}
		}

		/// <summary>Every serialized reference to the component from its own scene or prefab.</summary>
		private static List<(Component owner, string propertyPath)> FindReferences(FancyRect fancy)
		{
			var references = new List<(Component, string)>();
			var scene = fancy.gameObject.scene;
			var roots = scene.IsValid() ? scene.GetRootGameObjects() : new[] { fancy.transform.root.gameObject };

			foreach (var root in roots) {
				foreach (var component in root.GetComponentsInChildren<Component>(true)) {
					if (component == null || component == fancy) {
						continue;
					}

					var property = new SerializedObject(component).GetIterator();

					while (property.Next(true)) {
						if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == fancy) {
							references.Add((component, property.propertyPath));
						}
					}
				}
			}

			return references;
		}

		/// <summary>Whether the component comes from a prefab nested inside the one being looked at, so it cannot be removed here.</summary>
		private static bool IsInNestedPrefab(FancyRect fancy)
		{
			return PrefabUtility.IsPartOfPrefabInstance(fancy) && !PrefabUtility.IsAddedComponentOverride(fancy);
		}

		/// <summary>The top-level properties of a component that its prefab instance overrides.</summary>
		private static List<string> GetOverriddenProperties(Object component)
		{
			var names = new List<string>();
			var property = new SerializedObject(component).GetIterator();

			while (property.Next(true)) {
				if (!property.prefabOverride || property.propertyPath == "m_Script") {
					continue;
				}

				var topLevel = property.propertyPath.Split('.')[0];

				if (!names.Contains(topLevel)) {
					names.Add(topLevel);
				}
			}

			return names;
		}

		private static string GetPath(Transform transform, Transform root)
		{
			var path = transform.name;

			while (transform != root && transform.parent != null) {
				transform = transform.parent;
				path = transform.name + "/" + path;
			}

			return path;
		}

		/// <summary>Counts FancyRects in scene files by looking for the script's guid in their text. Scenes are not loaded.</summary>
		private static string CountInScenes()
		{
			var lines = new StringBuilder();
			var scriptGuids = AssetDatabase.FindAssets("FancyRect t:MonoScript");
			string fancyGuid = null;

			foreach (var guid in scriptGuids) {
				if (Path.GetFileName(AssetDatabase.GUIDToAssetPath(guid)) == "FancyRect.cs") {
					fancyGuid = guid;
				}
			}

			if (fancyGuid == null) {
				return "    FancyRect.cs was not found";
			}

			foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets", "Packages" })) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var text = File.ReadAllText(path);
				var count = 0;
				var index = 0;

				while ((index = text.IndexOf(fancyGuid, index, System.StringComparison.Ordinal)) >= 0) {
					count++;
					index += fancyGuid.Length;
				}

				if (count > 0) {
					lines.AppendLine($"    {path}: {count}");
				}
			}

			return lines.Length == 0 ? "    none" : lines.ToString();
		}
	}
}
