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

			Assert.IsFalse(voice.Positioned);
			Assert.IsFalse(voice.Pool.HasTemplate);
			Assert.AreEqual(0f, voice.Sources[0].Audio.spatialBlend, TOLERANCE);
		}

		[Test]
		public void NoTemplateAnywhere_IsNotPositioned()
		{
			var emitter = CreateEmitter();

			var voice = GetVoice(Runtime.Play(CreateSound(), emitter));

			Assert.IsFalse(voice.Positioned);
			Assert.IsTrue(voice.Follow == null);
			Assert.AreEqual(0f, voice.Sources[0].Audio.spatialBlend, TOLERANCE);
		}

		[Test]
		public void EmitterTemplate_ThePlayingSourceIsACopyOfIt()
		{
			var template = CreateTemplate(0.6f, 2f, 30f, AudioRolloffMode.Linear);
			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			var voice = GetVoice(Runtime.Play(CreateSound(), emitter));
			var audio = voice.Sources[0].Audio;

			Assert.AreSame(template, voice.Pool.Template);
			Assert.AreNotSame(template, audio);
			Assert.AreNotSame(template.gameObject, audio.gameObject);
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

			Assert.AreSame(soundTemplate, voice.Pool.Template);
			Assert.AreEqual(5f, voice.Sources[0].Audio.minDistance, TOLERANCE);
			Assert.AreEqual(60f, voice.Sources[0].Audio.maxDistance, TOLERANCE);
		}

		[Test]
		public void CustomRolloffCurve_ComesFromTheTemplate()
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
		public void FiltersOnTheTemplate_ComeAlongWithIt()
		{
			var template = CreateTemplate();
			template.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 500f;

			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			var audio = GetVoice(Runtime.Play(CreateSound(), emitter)).Sources[0].Audio;
			var filter = audio.GetComponent<AudioLowPassFilter>();

			Assert.IsNotNull(filter);
			Assert.AreEqual(500f, filter.cutoffFrequency, 0.5f);
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
			Assert.IsFalse(audio.playOnAwake);
			Assert.AreEqual(128, audio.priority);
			Assert.AreEqual(1f, audio.volume, TOLERANCE);
		}

		[Test]
		public void InactiveOrDisabledTemplate_StillGivesAWorkingSource()
		{
			var template = CreateTemplate();
			template.enabled = false;
			template.gameObject.SetActive(false);

			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			var audio = GetVoice(Runtime.Play(CreateSound(), emitter)).Sources[0].Audio;

			Assert.IsTrue(audio.enabled);
			Assert.IsTrue(audio.gameObject.activeInHierarchy);
		}

		[Test]
		public void Sources_AreReusedByPlaysThroughTheSameTemplate()
		{
			var template = CreateTemplate();
			var first = CreateEmitter();
			var second = CreateEmitter();

			first.SpatialTemplate = template;
			second.SpatialTemplate = template;

			var voice = GetVoice(Runtime.Play(CreateSound(), first));
			var audio = voice.Sources[0].Audio;

			Runtime.StopVoice(voice, 0f);

			Assert.AreSame(audio, GetVoice(Runtime.Play(CreateSound(), second)).Sources[0].Audio);
		}

		[Test]
		public void Sources_AreNotSharedBetweenTemplates()
		{
			var emitter = CreateEmitter();
			emitter.SpatialTemplate = CreateTemplate();

			var voice = GetVoice(Runtime.Play(CreateSound(), emitter));
			var audio = voice.Sources[0].Audio;

			Runtime.StopVoice(voice, 0f);

			emitter.SpatialTemplate = CreateTemplate();
			var other = GetVoice(Runtime.Play(CreateSound(), emitter));

			emitter.SpatialTemplate = null;
			var plain = GetVoice(Runtime.Play(CreateSound(), emitter));

			Assert.AreNotSame(audio, other.Sources[0].Audio);
			Assert.AreNotSame(audio, plain.Sources[0].Audio);
			Assert.AreEqual(0f, plain.Sources[0].Audio.spatialBlend, TOLERANCE);
		}

		[Test]
		public void DestroyedTemplate_CountsAsNone()
		{
			var template = CreateTemplate();
			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			Object.DestroyImmediate(template.gameObject);

			var voice = GetVoice(Runtime.Play(CreateSound(), emitter));

			Assert.IsFalse(voice.Positioned);
			Assert.AreEqual(0f, voice.Sources[0].Audio.spatialBlend, TOLERANCE);
		}

		[Test]
		public void TemplateDestroyedWhilePlaying_ItsSourceIsDestroyedWhenTheVoiceEnds()
		{
			var template = CreateTemplate();
			var emitter = CreateEmitter();
			emitter.SpatialTemplate = template;

			var voice = GetVoice(Runtime.Play(CreateSound(), emitter));
			var audio = voice.Sources[0].Audio;

			Object.DestroyImmediate(template.gameObject);
			Runtime.StopVoice(voice, 0f);

			Assert.IsTrue(audio == null);
		}

		[Test]
		public void InTheEditor_AReusedSourcePicksUpChangesToTheTemplate()
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

		[Test]
		public void Source_StartsAtTheEmitter_AndFollowsIt()
		{
			var emitter = CreateEmitter();
			emitter.SpatialTemplate = CreateTemplate();
			emitter.transform.position = new Vector3(1f, 2f, 3f);

			var voice = GetVoice(Runtime.Play(CreateSound(), emitter));
			var source = voice.Sources[0];

			Assert.AreEqual(new Vector3(1f, 2f, 3f), source.Transform.position);

			emitter.transform.position = new Vector3(4f, 5f, 6f);
			Runtime.Follow();

			Assert.AreEqual(new Vector3(4f, 5f, 6f), source.Transform.position);
		}

		[Test]
		public void PlayAt_PlacesTheSourceThroughTheSoundsTemplate()
		{
			var template = CreateTemplate();
			var sound = CreateSound();
			SetSoundTemplate(sound, template);

			var voice = GetVoice(Runtime.PlayAt(sound, new Vector3(7f, 8f, 9f)));

			Assert.IsTrue(voice.Positioned);
			Assert.AreSame(template, voice.Pool.Template);
			Assert.IsTrue(voice.Follow == null);
			Assert.AreEqual(new Vector3(7f, 8f, 9f), voice.Sources[0].Transform.position);
		}

		[Test]
		public void PlayAt_WithNoTemplate_IsNotPositioned()
		{
			var voice = GetVoice(Runtime.PlayAt(CreateSound(), new Vector3(7f, 8f, 9f)));

			Assert.IsFalse(voice.Positioned);
			Assert.AreEqual(0f, voice.Sources[0].Audio.spatialBlend, TOLERANCE);
		}

		[Test]
		public void EmitterGone_ASoundThatEndsOnItsOwn_PlaysOutWhereTheEmitterWas()
		{
			var emitter = CreateEmitter();
			emitter.SpatialTemplate = CreateTemplate();
			emitter.Volume = 0.5f;
			emitter.transform.position = new Vector3(1f, 2f, 3f);

			var handle = Runtime.Play(CreateSound(loop: false), emitter);
			var voice = GetVoice(handle);

			Runtime.EmitterGone(emitter);
			Object.DestroyImmediate(emitter.gameObject);

			Runtime.Follow();
			voice.Apply();

			Assert.IsTrue(Resolves(handle));
			Assert.IsTrue(voice.Emitter == null);
			Assert.AreEqual(new Vector3(1f, 2f, 3f), voice.Sources[0].Transform.position);
			Assert.AreEqual(0.5f, voice.Sources[0].Audio.volume, TOLERANCE);
		}

		[Test]
		public void EmitterGone_ALoopingSound_IsReleased()
		{
			var emitter = CreateEmitter();
			var chain = CreateChain(endClips: 1);

			var looping = Runtime.Play(CreateSound(loop: true), emitter);
			var chained = Runtime.Play(chain, emitter);

			Runtime.EmitterGone(emitter);

			// A clip with no fade out ends immediately. A chain moves on to its end clip.
			Assert.IsFalse(Resolves(looping));
			Assert.IsTrue(Resolves(chained));
			Assert.IsTrue(GetVoice(chained).Released);
		}

		[Test]
		public void FollowedTransformDestroyedWithoutNotice_StopsBeingFollowed()
		{
			var emitter = CreateEmitter();
			emitter.SpatialTemplate = CreateTemplate();

			var voice = GetVoice(Runtime.Play(CreateSound(), emitter));

			Object.DestroyImmediate(emitter.gameObject);

			Assert.DoesNotThrow(() => Runtime.Follow());
			Assert.IsTrue(ReferenceEquals(voice.Follow, null));
		}
	}
}
