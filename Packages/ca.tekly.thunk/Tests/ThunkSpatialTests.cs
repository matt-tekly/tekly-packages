using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Thunk.Tests
{
	public class ThunkSpatialTests : ThunkTestBase
	{
		private const float TOLERANCE = 0.0001f;

		[Test]
		public void NullEmitter_IsNotPositioned_EvenWithASoundTemplate()
		{
			var sound = CreateSound();
			SetSoundTemplate(sound, CreateTemplate());

			var voice = GetVoice(Runtime.Play(sound, null));

			Assert.IsTrue(voice.SpatialTemplate == null);
			Assert.AreEqual(0f, voice.Sources[0].Audio.spatialBlend, TOLERANCE);
		}

		[Test]
		public void NoTemplateAnywhere_IsNotPositioned()
		{
			var emitter = CreateEmitter();

			var voice = GetVoice(Runtime.Play(CreateSound(), emitter));

			Assert.IsTrue(voice.SpatialTemplate == null);
			Assert.AreEqual(0f, voice.Sources[0].Audio.spatialBlend, TOLERANCE);
		}

		[Test]
		public void EmitterTemplate_IsCopiedOntoThePlayingSource()
		{
			var template = CreateTemplate(0.6f, 2f, 30f, AudioRolloffMode.Linear);
			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			var voice = GetVoice(Runtime.Play(CreateSound(), emitter));
			var audio = voice.Sources[0].Audio;

			Assert.AreSame(template, voice.SpatialTemplate);
			Assert.AreEqual(0.6f, audio.spatialBlend, TOLERANCE);
			Assert.AreEqual(2f, audio.minDistance, TOLERANCE);
			Assert.AreEqual(30f, audio.maxDistance, TOLERANCE);
			Assert.AreEqual(AudioRolloffMode.Linear, audio.rolloffMode);
		}

		[Test]
		public void SoundTemplate_WinsOverTheEmittersTemplate()
		{
			var soundTemplate = CreateTemplate(1f, 5f, 60f);
			var emitterTemplate = CreateTemplate(0.2f, 1f, 10f);
			var sound = CreateSound();
			var emitter = CreateEmitter();

			SetSoundTemplate(sound, soundTemplate);
			emitter.SpatialTemplate = emitterTemplate;

			var voice = GetVoice(Runtime.Play(sound, emitter));

			Assert.AreSame(soundTemplate, voice.SpatialTemplate);
			Assert.AreEqual(5f, voice.Sources[0].Audio.minDistance, TOLERANCE);
			Assert.AreEqual(60f, voice.Sources[0].Audio.maxDistance, TOLERANCE);
		}

		[Test]
		public void OtherSpatialProperties_AreCopied()
		{
			var template = CreateTemplate();
			template.dopplerLevel = 3f;
			template.spread = 90f;
			template.reverbZoneMix = 0.7f;
			template.panStereo = 0.5f;
			template.bypassEffects = true;
			template.bypassListenerEffects = true;
			template.bypassReverbZones = true;
			template.velocityUpdateMode = AudioVelocityUpdateMode.Fixed;

			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			var audio = GetVoice(Runtime.Play(CreateSound(), emitter)).Sources[0].Audio;

			Assert.AreEqual(3f, audio.dopplerLevel, TOLERANCE);
			Assert.AreEqual(90f, audio.spread, TOLERANCE);
			Assert.AreEqual(0.7f, audio.reverbZoneMix, TOLERANCE);
			Assert.AreEqual(0.5f, audio.panStereo, TOLERANCE);
			Assert.IsTrue(audio.bypassEffects);
			Assert.IsTrue(audio.bypassListenerEffects);
			Assert.IsTrue(audio.bypassReverbZones);
			Assert.AreEqual(AudioVelocityUpdateMode.Fixed, audio.velocityUpdateMode);
		}

		[Test]
		public void CustomRolloffCurve_IsCopied()
		{
			var template = CreateTemplate(1f, 1f, 50f, AudioRolloffMode.Custom);
			var curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.25f));
			template.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);

			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			var audio = GetVoice(Runtime.Play(CreateSound(), emitter)).Sources[0].Audio;
			var copied = audio.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
			var expected = template.GetCustomCurve(AudioSourceCurveType.CustomRolloff);

			Assert.AreEqual(AudioRolloffMode.Custom, audio.rolloffMode);

			for (var step = 0; step <= 4; step++) {
				var time = step * 0.25f;
				Assert.AreEqual(expected.Evaluate(time), copied.Evaluate(time), 0.01f, "at " + time);
			}
		}

		[Test]
		public void PropertiesTheSoundOwns_AreNotTakenFromTheTemplate()
		{
			var template = CreateTemplate();
			template.clip = CreateClips(1)[0];
			template.loop = true;
			template.volume = 0.1f;
			template.mute = true;
			template.priority = 3;

			var sound = CreateSound(loop: false);
			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			var audio = GetVoice(Runtime.Play(sound, emitter)).Sources[0].Audio;

			Assert.AreSame(sound.Clips.Clips[0], audio.clip);
			Assert.IsFalse(audio.loop);
			Assert.IsFalse(audio.mute);
			Assert.AreEqual(128, audio.priority);
			Assert.AreEqual(1f, audio.volume, TOLERANCE);
		}

		[Test]
		public void ReusedSourceWithoutATemplate_IsReset()
		{
			var template = CreateTemplate();
			template.bypassEffects = true;
			template.spatialBlend = 1f;
			template.panStereo = 0.5f;
			template.dopplerLevel = 3f;

			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			var first = Runtime.Play(CreateSound(), emitter);
			var audio = GetVoice(first).Sources[0].Audio;

			Runtime.StopVoice(GetVoice(first), 0f);
			emitter.SpatialTemplate = null;

			var second = GetVoice(Runtime.Play(CreateSound(), emitter));
			var source = second.Sources[0];

			Assert.AreSame(audio, source.Audio);
			Assert.AreEqual(0f, audio.spatialBlend, TOLERANCE);
			Assert.IsFalse(audio.bypassEffects);
			Assert.AreEqual(0f, audio.panStereo, TOLERANCE);
			Assert.AreEqual(1f, audio.dopplerLevel, TOLERANCE);
			Assert.IsTrue(source.Template == null);
		}

		[Test]
		public void DestroyedTemplate_CountsAsNone()
		{
			var template = CreateTemplate();
			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			Object.DestroyImmediate(template.gameObject);

			var voice = GetVoice(Runtime.Play(CreateSound(), emitter));

			Assert.AreEqual(0f, voice.Sources[0].Audio.spatialBlend, TOLERANCE);
		}

		[Test]
		public void InTheEditor_TheTemplateIsCopiedAgainOnEveryPlay()
		{
			var template = CreateTemplate(1f, 1f, 50f);
			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			var first = Runtime.Play(CreateSound(), emitter);
			Runtime.StopVoice(GetVoice(first), 0f);

			template.maxDistance = 80f;

			var audio = GetVoice(Runtime.Play(CreateSound(), emitter)).Sources[0].Audio;

			Assert.AreEqual(80f, audio.maxDistance, TOLERANCE);
		}
	}
}
