# Thunk

A small audio package for Unity.

## Pieces

| Piece | What it is |
|---|---|
| `ThunkSound` | Base asset for anything playable. Holds mixer group, volume and pitch ranges, fades, priority, limits and an optional spatial template. |
| `ThunkClip` | A sound that plays one AudioClip picked from a set, once or looping. |
| `ThunkChain` | A sound with a start, a loop and an end. |
| `ThunkEmitter` | A component that plays sounds at its own position. |
| `ThunkHandle` | A struct that refers to one playing voice. Goes stale when the voice ends, after which its calls do nothing. |
| `ThunkTrack` | An asset for one stream of music. |
| `ThunkPlayer`, `ThunkTrackPlayer` | Components that play a sound, or play a sound on a track, without code. |
| `ThunkMixer` | Helpers for driving exposed AudioMixer volumes with 0 to 1 values. |

## Playing sounds

```csharp
[SerializeField] private ThunkSound m_click;
[SerializeField] private ThunkEmitter m_emitter;

m_click.Play();              // no position
m_emitter.Play(m_click);     // at the emitter
```

Both return a `ThunkHandle`. Ignore it to play a one-shot. Keep it to control the voice:

```csharp
var handle = m_engine.Play();

handle.SetVolume(0.5f);
handle.Pause(0.2f);
handle.Resume(0.2f);
handle.Release();     // ends naturally, using the sound's fade out
handle.Stop(0.5f);    // fades out and stops
handle.Stop();        // stops now
```

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

How a sound is heard in the world comes from a template AudioSource. Thunk copies its 3D Sound Settings (spatial blend, doppler, spread, rolloff, distances and their curves), stereo pan, reverb zone mix and effect bypass flags onto the AudioSource the sound plays through. Everything else on the template is ignored: the clip, volume, pitch, loop, priority, mute and output group come from the sound.

A sound played with `sound.Play()` is never positioned. A sound played from an emitter uses the first of these that exists:

1. The Spatial Template on the sound.
2. The Spatial Template on the emitter.

If neither exists the sound is not positioned.

A template is usually an AudioSource in a prefab, which makes it a shared preset: point every large sound at `spatial_large.prefab` and retune them all in one place. An emitter can also point at a disabled AudioSource on its own GameObject for a one-off.

In the editor the template is copied every time a sound plays, so it can be tuned while the game runs. In a build it is copied once per AudioSource.

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

## Not built yet

Timeline sounds.
