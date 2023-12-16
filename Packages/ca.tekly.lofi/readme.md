A basic Audio System

- Clips
  - SFX or Music
  - Can play a random clip from a list of audio clips
  - Random pitch between range
  - Random volume between range
  - Can set a minimum time between plays so there weren't bo too many of a sound playing at once
- Banks
  - Loads LofiClips from Addressables based on a label
  - Each bank is reference counted
- Tracks
  - Music is played on a track
  - You can cross fade between music on a track
- Emitter
  - Clips are played by Emitters


# Usage

## Banks
AudioClips must be loaded by loading a bank. Banks are just Addressable LofiClips that are grouped together with a label.

Load a bank by calling

```csharp
Lofi.Instance.LoadBank("bank.label");
```

You should unload the bank when you're done with it. Bank loads are reference counted internally.

## One Shots
You can play a one shot sound effect by calling

```csharp 
Lofi.Instance.PlayOneShot("clip_name")
```

The clip must be from a bank that is already loaded

## Music
Music is played on a track. A track is just a name you pass in when playing the music.

```csharp
// Immediately play this clip on the given track 
Lofi.Instance.PlayOnTrack("music_clip_name", "track_name");

// Cross fade the "new_music_clip" over 0.5f seconds
Lofi.Instance.CrossFadeOnTrack("new_music_clip", "track_name", 0.5f);
```
