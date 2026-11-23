#if UNITY_EDITOR
using UnityEngine;

namespace Tekly.Thunk
{
	/// <summary>
	/// Editor only. A pooled AudioSource is a copy of its template made when the pool grew, so a change made to the
	/// template afterwards would not be heard. This copies the template's 3D sound settings onto a source again
	/// each time it is reused, so they can be tuned while the game runs. Filters and other components are not
	/// refreshed.
	/// </summary>
	internal static class ThunkSpatialTemplate
	{
		public static void Copy(AudioSource template, AudioSource audio)
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

			// The curves are set last, because setting one of the values above replaces its curve with a flat one
			audio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, template.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
			audio.SetCustomCurve(AudioSourceCurveType.SpatialBlend, template.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
			audio.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, template.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
			audio.SetCustomCurve(AudioSourceCurveType.Spread, template.GetCustomCurve(AudioSourceCurveType.Spread));
		}
	}
}
#endif
