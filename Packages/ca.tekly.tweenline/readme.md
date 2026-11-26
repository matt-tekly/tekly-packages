# Tweenline

Timeline based tweening for Unity.

## The pieces

From the outside in:

| Piece | What it is |
|---|---|
| `Tweenline` | The component you add to a GameObject and play from code. Holds the animations. |
| `TweenAnimation` | One named timeline inside a Tweenline, like "Show" or "Hide". Holds tracks and markers. |
| `TweenTrack` | One row on the timeline. Drives one property of one target. |
| Clip | One block of time on a track. Says what the property does during that block. |
| `TweenChannel` | The property itself: how to read it, write it and combine values for it. |

### Tweenline component

The `Tweenline` component plays its animations by name:

```csharp
m_tweenline.Play("Show");
m_tweenline.PlayReverse("Show");
await m_tweenline.PlayAsync("Hide");
```

Only one animation plays at a time, so playing "Hide" in the middle of "Show" takes over from wherever "Show" left things. The component also remembers each property's rest value (its origin) the first time any animation touches it, so animations that share a property all agree on where it rests. Animations are edited, scrubbed and previewed in the Tweenline window (Window > Tekly > Tweenline).

### Animation

An animation is a timeline with a length, a wrap mode (Once, Loop or PingPong) and a list of tracks. It can also have markers, which fire a UnityEvent or the `MarkerFired` event at a set time, for things like a sound on impact.

### Track

A track drives one property of one target for the whole animation, like the alpha of a CanvasGroup or the scale of a Transform. The target can be the Tweenline's own GameObject or any object under it. The track type decides which property it drives, how values blend and what kind of clips it has. Add Track lists the types that fit the selected object first.

Most tracks are **base** tracks, which set their property. Some, like Shake and Punch, are **layered**: they offset or multiply whatever a base track set instead of replacing it, so a shake can run on top of a move.

### Clip

A clip is one block on a track, which you can drag and resize in the timeline. It has a start and a duration and says what happens to the property during that time. For most tracks that's a tween from one value to another with an ease. Other clip types are a shake, a bounce or an instant switch.

A clip's From can be the previous clip's end, the value when the animation started, the rest value or a value of its own, so clips chain into one another without retyping numbers. Its values can also be absolute or relative to the rest value, so "move up 50" works wherever the object sits.

### Channel

A channel is the property a track writes, like local position or CanvasGroup alpha. It's what lets several tracks meet on one property: each track writes into the channel, and the channel combines a base value with any layered offsets and multipliers and writes the result once. You only deal with channels when writing a custom track.

### Example

A "Show" animation on a popup could have:

| Track | Target | Channel | Clips |
|---|---|---|---|
| Canvas Group Alpha | Popup | alpha | 0s to 0.3s: 0 to 1 |
| Scale | Popup | localScale | 0s to 0.2s: 0.8 to 1.1, then 0.2s to 0.35s: 1.1 to 1 |
| Shake | Icon | anchoredPosition | 0.35s to 0.6s: a small shake, layered on the icon's position |
| Active | Close button | active | 0.35s: switch on |

## Pooling

Tweenline doesn't need pooling of its own. An animation allocates what it needs the first time it plays, and later plays reuse all of it.

That first play is the cost worth avoiding, so pool short-lived objects that carry a Tweenline, like list items, reward icons, damage numbers and toasts. A new instance pays the first-play cost again every time it's created.

A Tweenline remembers each property's rest value (its origin) the first time an animation touches it, and disabling it doesn't forget them. When a pooled object comes back somewhere new, like under a different parent or at a different spot, clips using Relative To Origin or From: Origin would still use the old rest values. Recapture them after placing the object:

```csharp
var item = m_pool.Get();
item.transform.SetParent(m_content, false);
item.Tweenline.CaptureOrigins();
item.Tweenline.Play("Show");
```

## Custom tracks

A game can add its own tracks without changing the package. The Add Track menu lists every concrete `TweenTrack` subclass with a parameterless constructor in any assembly, and tracks are stored with `[SerializeReference]`, so a game's track saves and loads like a built-in one. The menu name is the class name without "Track", so `AudioVolumeTrack` shows as "Audio Volume".

There are three ways to build one, from least to most work.

### From/to tweens: ClipTrack

For a property that tweens from one value to another, write a channel and a track.

The channel reads and writes the property. Subclass `FloatChannel`, `Vector2Channel`, `Vector3Channel`, `BoolChannel` or `TweenChannel<T>` for other value types:

```csharp
using System.Collections.Generic;
using Tekly.Tweenlines;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class AudioVolumeChannel : FloatChannel
{
    // Tracks must share one instance, so keep it somewhere static
    public static readonly AudioVolumeChannel Instance = new AudioVolumeChannel();

    public override string Name => "volume";
    public override float Read(Object target) => ((AudioSource) target).volume;
    public override void Write(Object target, float value) => ((AudioSource) target).volume = value;

#if UNITY_EDITOR
    // The serialized fields a write changes, so the editor preview can put them back
    public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
    {
        properties.Add(new DrivenProperty(target, "m_Volume"));
    }
#endif
}
```

The mixer finds a channel by its target and channel instance. Two tracks driving the same property must use the same instance, or they get separate origins and won't layer onto each other.

The track subclasses `ClipTrack<TTarget, TValue>` and says how to blend values and what a new clip looks like:

```csharp
using System;
using Tekly.Tweenlines;
using UnityEngine;

[Serializable]
public class AudioVolumeTrack : ClipTrack<AudioSource, float>
{
    protected override TweenChannel<float> Channel => AudioVolumeChannel.Instance;

    protected override float Lerp(float a, float b, float t) => Mathf.LerpUnclamped(a, b, t);

    // Used for clips in Relative To Origin space and clips that end on a target
    protected override float Add(float origin, float offset) => origin + offset;

    protected override TweenClip<float> CreateClip(float start)
    {
        return new TweenClip<float> {
            Start = start,
            Duration = 0.5f,
            FromMode = ClipFrom.Previous,
            Space = ClipSpace.Absolute,
            From = 0,
            To = 1
        };
    }
}
```

That is the whole track. It gets the standard clip inspector (From mode, Space, Ease), origin capture, scrubbing and the editor preview. Override `Bind` if the target isn't simply the first `TTarget` on the GameObject.

### On and off switches: StepTrack

For a bool that switches at points in time, subclass `StepTrack<TTarget>` and return a `TweenChannel<bool>` from `Channel`, the way `ActiveTrack` and `EnabledTrack` do. The timeline draws its clips as points.

### Anything else: TweenTrack

For a track that isn't a from/to tween, like the built-in Shake, Bounce and Punch, subclass `TweenTrack` and define its own clip. This one pulses a target's scale on top of whatever a base track does to it:

```csharp
using System;
using System.Collections.Generic;
using Tekly.Tweenlines;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// One pulse on the timeline: the scale swings bigger and smaller a few times, then settles. Clips are
/// structs, so a track's clip list is a plain serialized List that the timeline window can edit.
/// </summary>
[Serializable]
public struct PulseClip : IClip
{
    // The timeline window drags and resizes clips through these two fields, found by name
    public float Start;
    public float Duration;

    // Any other fields show up in the clip inspector, along with their tooltips and attributes
    [Tooltip("How far the scale swings, as a fraction of its size")]
    [Min(0)] public float Amount;

    [Tooltip("Pulses a second")]
    [Min(0.01f)] public float Frequency;

    public float End => Start + Duration;

    // IClip lets the ClipOrder helpers find clips by time without knowing the clip type
    float IClip.Start => Start;
}

/// <summary>
/// Pulses a Transform's scale. It's layered (ILayeredTrack), so it multiplies whatever scale a base track
/// sets, or the scale the target had when the animation started, instead of replacing it. Between clips it
/// leaves the scale alone.
/// </summary>
[Serializable]
public class PulseTrack : TweenTrack, ILayeredTrack
{
    // What the timeline window shows and binds the track to
    public override Object TargetObject => m_target;
    public override Type TargetType => typeof(Transform);
    public override int ClipCount => m_clips.Count;

    // Where the track's last clip ends, so the animation knows how long it is
    public override float End {
        get {
            var end = 0f;
            for (var i = 0; i < m_clips.Count; i++) {
                end = Mathf.Max(end, m_clips[i].End);
            }

            return end;
        }
    }

    // The timeline window finds these two fields by name
    [SerializeField] private Transform m_target;
    [SerializeField] private List<PulseClip> m_clips = new List<PulseClip>();

    // Where the track writes during a play. Taken from the mixer in Prepare, never serialized
    [NonSerialized] private ChannelSlot<Vector3> m_slot;

    // Called when the track is added to an object in the editor. Every GameObject has a Transform, so it
    // always binds
    public override bool Bind(GameObject gameObject)
    {
        m_target = gameObject.transform;
        return true;
    }

    // Called when a clip is added in the editor. The values here are what a new clip starts with
    public override int AddClip(float start)
    {
        m_clips.Add(new PulseClip { Start = start, Duration = 0.6f, Amount = 0.1f, Frequency = 3 });
        return m_clips.Count - 1;
    }

    // Where the timeline window draws a clip
    public override void GetClipRange(int index, out float start, out float duration)
    {
        start = m_clips[index].Start;
        duration = m_clips[index].Duration;
    }

    // Stores the scale as a rest value the first time any animation on the Tweenline touches it, so
    // animations that share the scale agree on where it rests
    public override void CaptureOrigins(TweenOrigins origins)
    {
        if (m_target != null) {
            origins.GetOrCapture(m_target, TweenChannels.LocalScale);
        }
    }

    // Runs once when a play starts. Takes the slot for this target's scale, which is shared with every other
    // track driving the same scale in this animation. A disabled or empty track takes nothing, so it leaves
    // the scale free for other code
    public override void Prepare(TweenMixer mixer)
    {
        m_slot = null;

        if (m_enabled && m_target != null && m_clips.Count > 0) {
            m_slot = mixer.Get(m_target, TweenChannels.LocalScale);
        }
    }

    // Runs every frame while playing, and when scrubbing in the editor. It only depends on time, so the
    // animation can be scrubbed and played backwards
    public override void Evaluate(float time)
    {
        if (m_slot == null) {
            return;
        }

        // The clip covering this time, or -1 between clips, where a layered track does nothing
        var index = ClipOrder.FindCovering(m_clips, time);

        if (index < 0) {
            return;
        }

        var clip = m_clips[index];

        if (clip.Duration <= 0) {
            return;
        }

        // Seconds into the clip, and a fade from 1 at its start to 0 at its end so the pulse settles
        var local = time - clip.Start;
        var fade = 1 - local / clip.Duration;

        // A sine wave Frequency times a second, swinging up to Amount either side of the normal size
        var swing = Mathf.Sin(local * clip.Frequency * Mathf.PI * 2) * clip.Amount * fade;

        // Multiplies the scale instead of setting it. The mixer combines this with the base track's value and
        // any other layers, then writes the scale once
        m_slot.Multiply(Vector3.one * (1 + swing));
    }

#if UNITY_EDITOR
    // The serialized fields this track changes, so the editor preview can restore them when it stops
    public override void CollectDrivenProperties(List<DrivenProperty> properties)
    {
        if (m_target != null) {
            TweenChannels.LocalScale.CollectDrivenProperties(m_target, properties);
        }
    }
#endif
}
```

How the pieces fit:

- `Prepare` runs before playback and takes a `ChannelSlot` from the mixer for each channel the track drives. Take only the channels the clips use, so the rest stay free for layouts and other code.
- `Evaluate` writes into the slot. A base track calls `SetBase`. A layered track calls `Offset` or `Multiply`, and the mixer combines them all and writes each channel once.
- `ILayeredTrack` marks a track that offsets or multiplies instead of setting. Layered tracks are prepared after base tracks, so `mixer.HasSource` and `mixer.BaseAt` can tell them what the base track is doing.
- `ClipOrder` finds clips by time without allocating. `TweenRandom.Hash` with `mixer.PlaySeed` gives random values that stay the same when a play is scrubbed.
- `GetWarning` (editor only) shows a message on the track when it's set up in a way that won't work.

The timeline window reads clips through serialized properties, so a custom track follows these conventions:

- The target field is named `m_target` and the clip list `m_clips`.
- Each clip has public `Start` and `Duration` fields, so it can be dragged and resized.
- A clip without a `FromMode` field gets an inspector that lists all its fields, so tooltips and `[Min]` attributes show up there.
