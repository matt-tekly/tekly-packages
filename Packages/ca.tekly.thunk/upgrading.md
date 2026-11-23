# Upgrading from the old Thunk

Thunk was rewritten. This file takes a project that used the old version to the new one. It is written to be followed step by step, by a person or by a coding agent. To hand it to an agent, update the package and then give it this:

```
Read Packages/ca.tekly.thunk/upgrading.md and migrate this project from the old Thunk.
```

A project is on the old Thunk if its code uses the namespaces `Tekly.Thunk.Core`, `Tekly.Thunk.Music` or `Tekly.Thunk.Utils`, or a class called `Thunk`.

## Before you start

1. **The project must use text serialization.** Check that `ProjectSettings/EditorSettings.asset` has `m_SerializationMode: 2`. If it does not, stop and ask for it to be changed (Edit > Project Settings > Editor > Asset Serialization > Force Text), as the asset steps edit YAML.
2. **Start from a clean git working tree**, with the package update as the only change.
3. **Find the last commit before the package was updated.** This file calls it `OLD`. Every old value is read from there with `git show OLD:<path>`, never from the working copy.

The reason for the third point: the old data is still in the asset files after the update, but Unity removes a field it no longer knows about the first time it saves that asset. A sound that has been saved under the new package has lost its clips on disk. `OLD` always has them.

Work in the order below. Do steps 1 to 3 with scripts that you write and run once, not by editing files one at a time. Print what each script changed.

## Step 1: Sound assets

**Find them.** Every `.asset` file that contains this line is an old sound:

```
m_Script: {fileID: 11500000, guid: 0f6bf0dc227644e592d3028e5976fd3d, type: 3}
```

**What an old one looks like**, from the `m_uniqueId` line down:

```yaml
  m_uniqueId: 10685183595126848427
  RandomMode: 0
  Clips:
  - {fileID: 8300000, guid: f281445048afb354e9a3e4d2f06ba9dd, type: 3}
  - {fileID: 8300000, guid: 27ccf7285c382c24bac53b24de5dce7e, type: 3}
  MixerGroup: {fileID: -8309867485381092383, guid: 3c48cc700afde4c7b82d128591b8a8d0, type: 2}
  Volume:
    Min: 1
    Max: 1
  Pitch:
    Min: 1
    Max: 1
  RandomizeStartTime: 0
  Loop: 0
  MinimumTimeBetweenPlays: 0.2
  InstanceCapacity: 2
  CapacityBehaviour: 2
```

**What it has to become:**

```yaml
  m_uniqueId: 10685183595126848427
  m_mixerGroup: {fileID: -8309867485381092383, guid: 3c48cc700afde4c7b82d128591b8a8d0, type: 2}
  m_volume:
    Min: 1
    Max: 1
  m_pitch:
    Min: 1
    Max: 1
  m_clock: 0
  m_maxInstances: 2
  m_stealing: 0
  m_cooldown: 0.2
  m_clips:
    Clips:
    - {fileID: 8300000, guid: f281445048afb354e9a3e4d2f06ba9dd, type: 3}
    - {fileID: 8300000, guid: 27ccf7285c382c24bac53b24de5dce7e, type: 3}
    Mode: 0
  m_loop: 0
  m_randomizeStartTime: 0
```

Fields that are not listed (fades, priority, spatial template) can be left out. Unity fills them in with their defaults.

**Field by field.** Take every value from `git show OLD:<path>`.

| Old field | New field | Notes |
|---|---|---|
| `Clips` | `m_clips.Clips` | The same list, nested one level deeper. An empty list is `Clips: []`. |
| `RandomMode` | `m_clips.Mode` | Same number. |
| `MixerGroup` | `m_mixerGroup` | |
| `Volume` | `m_volume` | Keep `Min` and `Max`. |
| `Pitch` | `m_pitch` | Keep `Min` and `Max`. |
| `Loop` | `m_loop` | |
| `RandomizeStartTime` | `m_randomizeStartTime` | |
| `MinimumTimeBetweenPlays` | `m_cooldown` | |
| `InstanceCapacity` and `CapacityBehaviour` | `m_maxInstances` and `m_stealing` | See the next table. |
| (none) | `m_clock` | `0` is Game Time, `1` is Real Time. See below. |

Remove every old field name from the file once its value has been moved. Do not leave `InstanceCapacity` in place: Unity would read it as `m_maxInstances` and undo the mapping below.

**Capacity.** The old Capacity Behaviour does not line up with the new Stealing values, so map the pair:

| Old `InstanceCapacity` | Old `CapacityBehaviour` | New `m_maxInstances` | New `m_stealing` |
|---|---|---|---|
| 0 or less | anything | 0 | 0 |
| above 0 | 0 (Unbound) | 0 | 0 |
| above 0 | 1 (Replace Oldest) | the capacity | 1 (Oldest) |
| above 0 | 2 (Ignore New) | the capacity | 0 (None) |

**Clock.** The old Thunk had no such setting, so work it out from how each sound is used. Set `m_clock: 1` (Real Time) on a sound if any of these is true, and `m_clock: 0` (Game Time) otherwise:

- Code plays it through `Thunk.Instance.UiSounds`.
- It is played on a music track (a `ThunkTrackPlayer` refers to it, or code passes it to a track).
- It is clearly an interface sound by its name or folder (click, hover, menu, ui).

If you cannot tell for a sound, leave it on Game Time and add it to the list of questions at the end.

**Check.** For every sound asset, the number of clips after the change equals the number in `OLD`. Print any that differ and fix them before going on.

## Step 2: Music tracks

The old Thunk named its tracks with strings. A track is now a `ThunkTrack` asset.

**Find every old track name.** Collect the distinct values from:

- `m_trackId:` lines in scenes and prefabs.
- String arguments to `GetTrack(...)` in code.
- `Id:` values under `m_tracks:` on `ThunkTrackInitializer` components, and `ThunkTrackSettings` built in code for `ConfigureTrack(...)`.

**Make one asset for each name.** Put them beside the project's music. Each needs two files. The asset, for example `track_music.asset`:

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 6419e6c05d7b4e71a39c2f94148677a8, type: 3}
  m_Name: track_music
  m_EditorClassIdentifier: Tekly.Thunk::Tekly.Thunk.ThunkTrack
  m_uniqueId: 0
  m_crossfade: 1
  m_resumeHeld: 1
  m_mixerGroup: {fileID: 0}
  m_realTime: 1
  m_duckLevel: 0.3
  m_duckFadeDown: 0.2
  m_duckFadeBack: 0.8
```

And its meta file, `track_music.asset.meta`, with a new random 32 character hex guid:

```yaml
fileFormatVersion: 2
guid: <new guid>
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

`m_Name` must match the file name. Leave `m_uniqueId` at 0, Unity sets it when it imports the asset.

**Point the track players at them.** A track player is a component with this script:

```
m_Script: {fileID: 11500000, guid: 843440db22cf4bf1ace0153a8fd0f120, type: 3}
```

In each one, replace the `m_trackId: <name>` line with a reference to the asset made for that name:

```yaml
  m_track: {fileID: 11400000, guid: <that track asset's guid>, type: 2}
```

Leave its other fields alone. `m_clip`, `m_push`, `m_fadeInDuration` and `m_autoPlay` are read into their new names by Unity.

**Remove the initializers.** `ThunkTrackInitializer` no longer exists, so its components show as missing scripts. They have this script:

```
m_Script: {fileID: 11500000, guid: 1dd692fed648e8a42aa2f9a294bd19f9, type: 3}
```

Remove each one from its scene or prefab: delete the component's `--- !u!114 &<id>` block, and delete the `- component: {fileID: <id>}` line that refers to it in its GameObject's `m_Component` list. If that leaves a GameObject with nothing but a Transform, leave the GameObject and mention it at the end.

## Step 3: Emitters

An emitter is a component with this script:

```
m_Script: {fileID: 11500000, guid: fc33239e18de4848b4a93bfd34a8c438, type: 3}
```

- **`m_volume`, `m_pitch`** are unchanged.
- **`m_ignoreListenerPause`** is read into the new Real Time option by Unity. Nothing to do.
- **`m_unscaledDeltaTime`** is gone. If an emitter had it set to 1 and `m_ignoreListenerPause` set to 0 in `OLD`, add `m_realTime: 1` to it and mention it at the end.
- **`m_audioSourceTemplate`** is gone and cannot be converted by editing files. See below.

**Templates.** The old template was an AudioSource the emitter used directly, usually one in the scene. The new one is `m_spatialTemplate`, and it has to be an AudioSource in a prefab of its own, because every sound is played through a copy of that prefab's whole GameObject.

For every emitter that had `m_audioSourceTemplate` set in `OLD`, find the AudioSource it pointed at and note its 3D settings (spatial blend, rolloff mode, min and max distance, doppler). Group the emitters whose templates have the same settings. Then either:

- If you can drive the Unity editor, make one prefab for each group holding only an AudioSource with those settings, and assign it to `m_spatialTemplate` on those emitters.
- If you cannot, list the groups and their settings at the end for the person to make the prefabs.

An emitter with no template plays its sounds with no position, so do not skip this.

`ThunkPlayer` components need nothing. `m_clip` is read into its new name by Unity.

## Step 4: Code

Fix compile errors with this table. Search for each pattern on the left across the project.

**Namespaces**

| Before | Now |
|---|---|
| `using Tekly.Thunk.Core;`, `using Tekly.Thunk.Music;`, `using Tekly.Thunk.Utils;` | `using Tekly.Thunk;` (once) |

**Mixer helpers**

| Before | Now |
|---|---|
| `Thunk.SetVolume`, `Thunk.GetVolume`, `Thunk.ToDecibel`, `Thunk.ToLinear` | The same method on `ThunkMixer`, with the same arguments |

**Playing**

| Before | Now |
|---|---|
| `Thunk.Instance.OneShot.Play(clip)` | `clip.Play()` |
| `Thunk.Instance.UiSounds.Play(clip)` | `clip.Play()`, and the asset's Clock set to Real Time (step 1) |
| `int id = emitter.Play(clip);` | `ThunkHandle handle = emitter.Play(clip);` |
| A field or variable of type `int` holding an id | The same, of type `ThunkHandle` |
| `Thunk.INVALID_ID` | `ThunkHandle.None` |
| `id != Thunk.INVALID_ID` used to mean "is it playing" | `handle.IsPlaying` |
| `emitter.Stop(id)` | `handle.Stop()` |
| `emitter.SetPaused(id, true)` and `emitter.SetPaused(id, false)` | `handle.Pause()` and `handle.Resume()` |
| `emitter.SetVolume(id, volume)` | `handle.SetVolume(volume)` |
| `emitter.TryGetInstance(id, out var instance)` | `handle.IsPlaying`. There is no instance object to read. |
| `emitter.Play(clip, pitch: p, volume: v)` | `var handle = emitter.Play(clip);` then `handle.SetPitch(p);` and `handle.SetVolume(v);` |
| `emitter.Play(clip, delay: d)` | No direct replacement. Ask. |
| `emitter.Play(clip, startTime: t)` | No replacement. Ask. |

A handle does not need its emitter, and every call on a handle whose sound has ended does nothing, so guards like `if (id != Thunk.INVALID_ID)` around a stop can be removed.

`handle.SetPitch` and `handle.SetVolume` multiply the value the sound picked from its own range. If the old code passed a pitch or volume meant to replace the sound's own, say so at the end.

**Emitters**

| Before | Now |
|---|---|
| `emitter.IgnoreListenerPause`, `emitter.UseUnscaledDeltaTime` | `emitter.RealTime` |
| `emitter.Instances`, `emitter.InstanceDisposed` | Removed. Keep the handles you need and poll `handle.IsPlaying`. |
| `emitter.GetAudioSource()`, `emitter.UpdatePitchAndVolume()` | Removed. AudioSources are pooled by the package and not handed out. Ask. |

**Sound assets from code**

| Before | Now |
|---|---|
| `clip.Clips` (an `AudioClip[]`) | `clip.Clips.Clips` |
| `clip.RandomMode` | `clip.Clips.Mode` |
| `clip.InstanceCapacity` | `clip.MaxInstances` |
| `clip.MinimumTimeBetweenPlays` | `clip.Cooldown` |
| `clip.MixerGroup`, `clip.Volume`, `clip.Pitch`, `clip.Loop`, `clip.RandomizeStartTime` | Same names |
| `clip.State`, `ThunkClipState`, `ThunkClipInstance`, `ThunkAudioSource`, `ThunkClipStateManager` | Removed. Ask what the code needed them for. |

These are all read-only now. Code that assigned to a sound's fields at runtime has no replacement. Ask.

**Music**

| Before | Now |
|---|---|
| `Thunk.Instance.TrackManager.GetTrack("music")` | A `[SerializeField] private ThunkTrack m_music;` field, assigned the asset made in step 2 |
| `TrackManager.ConfigureTrack(settings)` | Removed. The settings are on the track asset. |
| `track.Play(clip)` | `track.Play(clip, 0f)`. Without the `0f` the track's own crossfade time is used. |
| `track.PlayCrossfade(clip, time)` | `track.Play(clip, time)` |
| `track.PushCrossfade(clip, time)` | `var handle = track.Interrupt(clip, time);` and keep the handle |
| `track.PopCrossFade(time)`, `track.PopCrossFade(id, time)` | `handle.Stop(time)` on the handle `Interrupt` returned |
| `track.Stop(id, fade)` | `handle.Stop(fade)` |
| `track.Emitter` | Removed. Ask. |

Adding a serialized `ThunkTrack` field means something has to assign it. Assign it in the scene or prefab YAML where the component lives, using the track asset's guid as in step 2, or list it at the end.

`ThunkClip` still exists, so fields of that type keep working. `ThunkSound` is its base type and also accepts the new chain and timeline sounds. Changing a field to `ThunkSound` is optional.

## Step 5: Check

1. The project compiles with no errors.
2. No scene or prefab has a component with a missing script that came from Thunk.
3. Every sound asset has the same number of clips as it had in `OLD`.
4. Every `ThunkTrackPlayer` has a track assigned.
5. Open the project in Unity, let it import, and read the console. There should be no errors or serialization warnings from the changed assets.

## Tell the person

Finish with a short report. Besides the questions collected above, tell them about these changes in behaviour, as nothing will flag them:

- **Sounds follow the time scale.** A sound on the Game Time clock slows down in slow motion and pauses while `Time.timeScale` is 0. Before, sounds ignored the time scale. A game that does not want slow motion to bend its audio can set `ThunkSettings.PitchFollowsTimeScale = false` when it starts. The pausing stays either way.
- **A sound outlives its emitter.** A one-shot now finishes after its emitter is disabled or destroyed, where before it was cut off. A looping sound is released.
- **A template is a prefab.** Every sound played through it is a copy of the whole prefab.
- **Sounds whose clock was a guess**, so they can be checked by ear.

The readme describes how each of these works.
