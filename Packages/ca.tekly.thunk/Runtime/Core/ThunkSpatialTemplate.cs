using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// Copies how a sound is heard in the world from a template AudioSource onto the AudioSource a voice plays
	/// through. Only the template's spatial and effect bypass settings are used. What it plays, its volume, pitch,
	/// loop, priority, mute and output group belong to the sound and are left alone.
	/// </summary>
	internal static class ThunkSpatialTemplate
	{
		/// <summary>
		/// Sets the source up from the template, or as not positioned if the template is null.
		/// </summary>
		public static void Apply(ThunkSource source, AudioSource template)
		{
			// Unity null check, so a destroyed template counts as none
			if (template == null) {
				if (!ReferenceEquals(source.Template, null)) {
					Clear(source.Audio);
					source.Template = null;
				}

				return;
			}

#if !UNITY_EDITOR
			// Reading the template's curves allocates, so a source that is already set up from it is left as it is.
			// In the editor it is always copied, so a template can be tuned while the game runs.
			if (ReferenceEquals(source.Template, template)) {
				return;
			}
#endif

			Copy(template, source.Audio);
			source.Template = template;
		}

		private static void Copy(AudioSource template, AudioSource audio)
		{
			audio.bypassEffects = template.bypassEffects;
			audio.bypassListenerEffects = template.bypassListenerEffects;
			audio.bypassReverbZones = template.bypassReverbZones;

			audio.panStereo = template.panStereo;
			audio.spatialBlend = template.spatialBlend;
			audio.reverbZoneMix = template.reverbZoneMix;
			audio.dopplerLevel = template.dopplerLevel;
			audio.spread = template.spread;
			audio.velocityUpdateMode = template.velocityUpdateMode;

			audio.rolloffMode = template.rolloffMode;
			audio.minDistance = template.minDistance;
			audio.maxDistance = template.maxDistance;

			audio.spatialize = template.spatialize;
			audio.spatializePostEffects = template.spatializePostEffects;

			// After the values above, as setting one of those replaces its curve with a flat one
			audio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, template.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
			audio.SetCustomCurve(AudioSourceCurveType.SpatialBlend, template.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
			audio.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, template.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
			audio.SetCustomCurve(AudioSourceCurveType.Spread, template.GetCustomCurve(AudioSourceCurveType.Spread));
		}

		/// <summary>
		/// Puts back what a template may have changed that a sound with no position would still be affected by.
		/// </summary>
		private static void Clear(AudioSource audio)
		{
			audio.bypassEffects = false;
			audio.bypassListenerEffects = false;
			audio.bypassReverbZones = false;

			audio.panStereo = 0f;
			audio.spatialBlend = 0f;
			audio.reverbZoneMix = 1f;
			audio.dopplerLevel = 1f;

			audio.spatialize = false;
			audio.spatializePostEffects = false;
		}
	}
}
