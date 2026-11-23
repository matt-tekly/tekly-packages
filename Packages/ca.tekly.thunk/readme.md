# Thunk

A small audio package for Unity.

## Pieces

| Piece | What it is |
|---|---|
| `ThunkSound` | Base asset for anything playable. Holds mixer group, volume and pitch ranges, fades, priority, limits and an optional spatial template. |
| `ThunkClip` | A sound that plays one AudioClip picked from a set, once or looping. |
| `ThunkChain` | A sound with a start, a loop and an end. |
| `ThunkTimeline` | A sound made of several clips played at set times. |
| `ThunkEmitter` | A component that plays sounds at its own position. The sounds follow it and outlive it. |
| `ThunkHandle` | A struct that refers to one playing voice. Goes stale when the voice ends, after which its calls do nothing. |
| `ThunkTrack` | An asset for one stream of music. |
| `ThunkPlayer`, `ThunkTrackPlayer` | Components that play a sound, or play a sound on a track, without code. |
| `ThunkMixer` | Helpers for driving exposed AudioMixer volumes with 0 to 1 values. |

## Playing sounds

```csharp
[SerializeField] private ThunkSound m_click;
[SerializeField] private ThunkEmitter m_emitter;

m_click.Play();              // no position
m_emitter.Play(m_click);     // at the emitter, following it
m_click.PlayAt(position);    // at a point in the world
```

All three return a `ThunkHandle`. Ignore it to play a one-shot. Keep it to control the voice:

```csharp
var handle = m_engine.Play();

handle.SetVolume(0.5f);
handle.Pause(0.2f);
handle.Resume(0.2f);
handle.Release();     // ends naturally, using the sound's fade out
handle.Stop(0.5f);    // fades out and stops
handle.Stop();        // stops now
```

## Clocks

Every sound runs on one of two clocks, set by Clock on the sound.

| | Game Time (the default) | Real Time |
|---|---|---|
| `Time.timeScale` | The sound plays at that speed, so slow motion slows it and lowers its pitch | Ignored |
| Time scale of 0 | Paused, and carries on from where it was | Ignored |
| `AudioListener.pause` | Paused | Ignored |
| Fades | Game seconds | Real seconds |
| Timeline delays | Game seconds | Real seconds |

Use Game Time for anything in the world and Real Time for UI and music. Pausing the game by setting the time scale to 0 then pauses the world's sounds and leaves the menus and music alone, with nothing else to call.

Chains stay seamless at any speed, and a timeline's entries keep their spacing: at half speed each clip takes twice as long and so does each gap.

An emitter or a track can put everything played through it on Real Time, whatever the sound says:

- **`ThunkEmitter`** has a Real Time option. A pause menu that plays gameplay sounds (a weapon preview, a reward jingle) plays them through an emitter with it ticked, so those sounds don't need a second copy.
- **`ThunkTrack`** has the same option and it is on for a new track, as music rarely wants to bend with the game. Untick it for music that should.

For a game that wants its sounds to pause with the game but not bend in slow motion, turn the speed part off for everything:

```csharp
ThunkSettings.PitchFollowsTimeScale = false;   // once, when the game starts
```

Game Time sounds then always play at normal speed, and still pause while the time scale is 0 or the listener is paused. The setting lasts for the play session.

Which clock a voice is on is decided when the sound starts. A sound on Game Time that is played while the game clock is stopped starts paused, and plays once the clock runs again.

Slow motion done with a pitch parameter on an Audio Mixer group is not seen by Thunk. A chain routed through such a group loses its clean join, so let the clock do it.

## Limits

Max instances, stealing and cooldown are set on the sound and apply to the sound as a whole, wherever it is played from. Copies of a sound loaded from different bundles share them.

A play that is refused (cooldown, max instances, no clips) returns `ThunkHandle.None`. The reason is logged at Debug level on the `ThunkRuntime` logger.

## Music

```csharp
[SerializeField] private ThunkTrack m_music;

m_music.Play(m_townTheme);                 // crossfades from the current main music
var boss = m_music.Interrupt(m_bossTheme); // holds the town theme
boss.Stop(1f);                             // the town theme comes back
m_music.Stop();
```

- `Play` sets the track's main music. Playing the sound that is already the main music does nothing.
- `Interrupt` plays over the top and holds what was playing. When the interrupting voice ends or is stopped, what was held returns. The track's Resume Held setting chooses between carrying on from where it was and starting again.
- `Play` during an interruption changes what will be there when the interruption ends.

## Ducking

A track can be turned down while something else needs to be heard. The level and fade times are on the track asset.

```csharp
m_music.PlayOver(m_victoryJingle);   // music is ducked for as long as the jingle is heard

var duck = m_music.Duck();           // music stays ducked until released
duck.Release();
```

With several requests active the lowest level wins, they do not multiply together. A sound played with `PlayOver` is not part of the track: it uses its own mixer group and `Stop` on the track leaves it alone.

This only turns down music on the track. For one group of sounds ducking another by signal level, use the Audio Mixer's Duck Volume effect.

## Spatial sound

Sounds play through AudioSources that Thunk pools centrally, under one `[Thunk]` object. How a sound is heard in the world comes from a template: an AudioSource in a prefab. Thunk keeps a pool of copies of that prefab and plays the sound through one of them, so the template's 3D Sound Settings apply, and so do any filters (`AudioLowPassFilter` and the like) or spatializer components that sit next to its AudioSource.

The clip, volume, pitch, loop, priority and output group always come from the sound, whatever the template has set.

```csharp
m_emitter.Play(m_footstep);       // follows the emitter
m_explosion.PlayAt(hit.point);    // stays at a point, no emitter needed
m_click.Play();                   // not positioned
```

A sound played with `sound.Play()` is never positioned. `sound.PlayAt(position)` uses the Spatial Template on the sound. A sound played from an emitter uses the first of these that exists:

1. The Spatial Template on the sound.
2. The Spatial Template on the emitter.

If there is no template the sound is not positioned.

Things to know about templates:

- The whole GameObject of the template is copied, so keep it to an AudioSource and its filters. A template should be a prefab of its own, not an AudioSource on a character or on the emitter.
- There is one pool for each template, so templates are meant to be shared. Point every large sound at `spatial_large.prefab` and they share one pool and are retuned in one place.
- In the editor a template's 3D Sound Settings are copied onto a pooled source again each time it is reused, so they can be tuned while the game runs. Changes to filters are only picked up by copies made afterwards.

### When an emitter goes away

A sound is not cut off when its emitter is disabled or destroyed. A sound that ends on its own (a clip that does not loop, a timeline) plays out where the emitter last was. A sound that would play forever (a looping clip, a chain) is released, so it fades out or plays its end. This is what lets an enemy play its death sound and be destroyed on the same frame.

## Chains

A `ThunkChain` is a sound in three parts: a start that plays once, a loop that follows it with no gap, and an end that plays when the voice is released. The start and end are optional. Each part is its own set of clips.

```csharp
var engine = m_engineChain.Play();   // start, then loop
engine.Release();                    // loop fades out under the end
engine.Stop(0.5f);                   // or: fade the whole thing out, end included
```

- `Release` is the natural ending. Without an end, it fades out using the sound's fade out time.
- `Stop` always ends the whole voice, whichever part is playing.
- A chain is one voice. It counts once against its limits, and one pitch is picked for all three parts.

The loop is scheduled against the audio clock from the length of the start clip, so the join is exact. Two things make it approximate: changing the pitch during the start, and pausing during the start. Both reschedule the loop from where the start has got to. Clips in a format that pads its audio (MP3) will have a gap whatever is done, so use Vorbis or PCM for chain clips. Doppler on a moving emitter also bends the pitch without Thunk knowing.

A track can play a chain, which is how music with an intro is done.

## Timelines

A `ThunkTimeline` is a sound made of several clips played at set times after it starts, such as a chest opening: a creak, a latch at 0.2 seconds and a sparkle at 0.5. Entries can overlap. It plays once and does not loop.

Each entry has a delay, its own set of clips, a volume offset and a pitch offset in semitones.

- A timeline is one voice with one handle. It counts once against its limits.
- Stopping it stops the entries that are playing and cancels the ones that have not started.
- Every entry holds an AudioSource for as long as the timeline plays, so a timeline with five entries costs five voices from Unity's budget. It is meant for composite sounds, not for anything fired many times a second.

For sounds that need to line up with an animation, fire them from the animation (Tweenline markers, for example) instead.

## Upgrading from the old Thunk

Thunk was rewritten, and a project on the old version needs its code, sound assets and scenes changed. [upgrading.md](upgrading.md) has the whole procedure, written so that a coding agent can carry it out:

```
Read Packages/ca.tekly.thunk/upgrading.md and migrate this project from the old Thunk.
```

In short:

- The namespaces under `Tekly.Thunk` are now the single namespace `Tekly.Thunk`, and the `Thunk` class is gone. Its mixer helpers are on `ThunkMixer`.
- `int` ids are now `ThunkHandle`s, and a sound is stopped, paused and faded through its handle.
- Tracks named by a string are now `ThunkTrack` assets.
- Old sound assets load, but their clip lists have to be moved. Do this from git history before saving them in Unity, as saving drops the old data.
- Sounds follow the time scale unless they are on the Real Time clock.
