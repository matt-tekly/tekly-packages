# Stagecraft — design decisions

What the README doesn't say: the rules the runtime relies on, and why the non-obvious choices went the way they
did. If you change something that breaks one of the invariants, the tests under `Tests/EditMode` are written to
catch it.

## Invariants

1. **An actor has one state, and only `Stage`/`ActorPool` change it.** `Pooled → Acquired → Active → Despawning
   → Pooled`; `Destroyed` from anywhere. `IsAlive` means `State == Active` and nothing else. There is no separate
   "in pool" or "registered" flag to drift out of step; the pool pushes an instance exactly when it sets `Pooled`.
2. **Only `Sync` touches the tick lists.** Spawning pushes to a pending-add queue, despawning to a pending-remove
   queue; `Sync` drains both at the start and end of every `Tick`/`FixedTick`. So no loop over a list ever sees
   it change, and code can spawn/despawn from anywhere (ticks, physics events, `OnDespawned`) without guards.
   The `IsScheduled` bit on `Actor` is set and cleared only by `Sync`.
3. **`Despawn` is immediate for state, deferred for cleanup.** After the call: not alive, id unresolvable, refs
   null, skipped by every loop. `OnDespawned`, deactivation and pooling happen at the next sync. Calling it twice,
   or on a stale handle, is a no-op by state alone.
4. **Handles, not references, cross frames.** `ActorId = (slot, generation)`; freeing a slot bumps its generation.
   `ActorRef<T>` resolves through `behaviour.Actor.Id == Id && State == Active`, so it needs no Unity null check
   and goes null on `Despawn`, not on reuse. A raw reference handed out by the stage is valid until the next sync.
5. **Every behaviour is in exactly one tick group.** Its own registration, or the nearest registered base type,
   else the fallback group (error logged once per type). Hence ticked at most once per tick.
6. **Nothing per spawn is reflective or allocating.** Injection and tick-override detection happen once per
   pooled instance / once per type. Spawn, despawn, sync and ticks allocate nothing after warm-up.
7. **The stage never steps time on its own.** `Tick(dt)`/`FixedTick(dt)` are called by a driver (`StageRunner`,
   or a netcode tick); there is no accumulator, and `FixedTickBegin/End` exist so someone else can own the
   physics step in between.
8. **Physics callbacks only record.** Adapters buffer the engine's events and the stage delivers them in
   `FixedTickEnd`, before the closing sync. Fixed step order: logic → step → deliver → sync. Bodies carry the
   owning `ActorId` as user data, so an event about a dead or reused actor resolves to nothing.
9. **Pooled instances are asleep.** Instantiated under an inactive parent, deactivated, then reparented: no
   `Awake`/`OnEnable` until the first activation. `Actor.EnsureInitialized` rebuilds the behaviour array if
   `OnValidate` never ran.

## State × operation

| State \ Op | `Activate` | `Despawn` | `TryGet` / refs | `Sync` does | `Object.Destroy` |
|---|---|---|---|---|---|
| Pooled | error | no-op (warned once) | null | – | → Destroyed, pool forgets |
| Acquired | → Active: pose, id, args, `OnSpawned`, queue add | → Pooled now, no callbacks | null (no id yet) | – | → Destroyed, pool forgets |
| Active | no-op | → Despawning, queue remove | value | add to lists, `OnAdded` | → Destroyed, queued (no `OnDespawned`) |
| Despawning | no-op | no-op | null | `OnRemoved` → `OnDespawned` → `Despawned` event → deactivate → free id → pool | → Destroyed |
| Destroyed | error | no-op | null | remove from lists, free id, pool forgets | – |

`Clear` = despawn every Active + release every Acquired + `Sync`, repeated until the registry is empty (≤16 passes),
so it catches actors spawned this frame and by `OnDespawned`. From inside a tick it defers to that tick's closing
sync. `Dispose` = `Clear` + destroy pools; no-op while the app is quitting.

## Callback timing

| | When |
|---|---|
| `ISpawnArgs.Initialize` | inside `Spawn`/`Activate`, after `SetActive(true)` (`Awake` has run), before any `OnSpawned` |
| `OnSpawned`, `Stage.Spawned` | inside `Spawn`/`Activate`. Alive and resolvable from here |
| `ActorSystem.OnAdded` | the next sync (start of the next tick), i.e. before that tick's loops |
| `Tick`/`FixedTick`/system ticks | between the two syncs of a stage tick, groups in registration order, per-object before the group's system |
| `ActorSystem.OnRemoved` | the sync after `Despawn`, before `OnDespawned` |
| `OnDespawned`, `Stage.Despawned` | the sync after `Despawn`; the actor still has its id and transform |

Anything spawned from `OnDespawned` is scheduled in the same sync's next pass and ticked by the tick that sync
opens. The 16-pass bound turns an infinite spawn↔despawn loop into one error and leftovers for the next sync.

## Decisions and why

- **`OnSpawned` runs immediately; list insertion is what's deferred.** A spawner nearly always wants to talk to
  what it just spawned. Deferring the callback would force an `ActorRef` round-trip for no gain; deferring the
  insertion is what protects running loops.
- **Spawn, then `Tick` → one tick.** The opening sync schedules it and the same tick runs it. "Joins at a sync
  point" is about loop safety, not about skipping a frame.
- **`SpawnReason` instead of a second spawn callback.** A restore (save game, replicated spawn) goes
  `Acquire` → set state → `Activate(…, SpawnReason.Restored)`, and `OnSpawned` still runs so setup that must
  happen either way isn't duplicated; behaviours read `SpawnReason` to skip spawn-only effects. An `OnRestored`
  callback would be forgotten by half the behaviours that needed it.
- **No named phases.** Registration order is the phase order; a phase would be a label on a range of groups.
  Profiler markers are per group, which gives the same visibility. Add phases only if two systems need to agree
  on a name across assemblies.
- **No `LateTick`.** Ordering against other stage code is registration order. Ordering against Unity's
  `LateUpdate` (post-animation transforms, after Cinemachine) is the one real case; when it shows up, add
  `LateTick` mirroring `Tick` and let the driver (or `CinemachineCore.CameraUpdatedEvent`) call it.
- **Tick doesn't consult `enabled`.** It's a native call per behaviour per tick. A behaviour that shouldn't tick
  should despawn or hold state; disabling the component only stops Unity messages.
- **`All<Boss>()` excludes bosses from `All<Enemy>()`** when both are registered: one group per behaviour keeps
  "ticked once" trivially true. Register only what you need to order or query.
- **Order within a group is not stable.** Swap-remove. If two actors of one type must tick in a fixed relative
  order, that's a system.
- **Per-object vs system is about what becomes possible, not dispatch cost.** A virtual call and a state check
  per behaviour is noise next to the engine calls the behaviour makes. Systems earn their place by having the
  whole list in hand: batched engine calls instead of one per actor, and a place to put a Jobs path (copy the
  dense list to a `NativeArray`, job, write back) that per-object ticks have nowhere to go.
- **Physics is an adapter per backend, with a contract rather than an abstraction.** The 2D and 3D APIs differ
  too much to wrap. The contract: body created in `OnSpawned`, destroyed in `OnDespawned` (the sync point),
  user data = `ActorId`, callbacks record, delivery in `FixedTickEnd`. Core exposes only `IStagePhysics`.
  What the low-level 2D API actually does (simulation modes, event spans, callback timing) is documented at
  the top of `PhysicsCore2DAdapter.cs`, next to the code that depends on it.
- **DevBoard is a separate optional assembly** compiled only when `ca.tekly.devboard` is present
  (`versionDefines`). The core never references a debug UI.
- **Test behaviours live in `Tests/Shared`, an all-platform assembly.** Unity refuses `AddComponent` for a
  MonoBehaviour in an editor-only assembly, and both the edit-mode and play-mode tests need the same prefabs.

## Netcode seams (nothing networked here)

- *Time*: a netcode tick calls `FixedTickBegin(dt)` → steps physics (adapter in `PhysicsStepping.External`) →
  `FixedTickEnd()`. `Tick` stays on `Update` for presentation. Fish-Net doesn't step the low-level 2D world, so
  a tick hook has to; `External` mode exists for that.
- *Prefab ids*: `PrefabRegistry` maps `ActorPrefabId` ↔ prefab so a spawn is `(prefabId, pose, args)` bytes.
- *Args*: `ISpawnArgs<TArgs> where TArgs : struct`; no object references in them (by rule, not by type system).
- *Identity*: `ActorId` is local to one machine and never serialized as identity; a network id lives in a behaviour.
- *External creator*: `Acquire` / `Activate` / `Despawn` are public and go through the same state table, so a
  netcode object pool can be backed by the stage's pools.
- *Persistence (not built)*: a save is a walk of live actors at a sync point writing `(prefab id, pose, per-
  behaviour state)`; a load is `Acquire` → load state → `Activate(Restored)`, then a second pass to resolve
  actor references, since `ActorId` is never persisted. Needs a per-behaviour state hook, loadable `StageTime`,
  and a save-local index for refs; `SpawnReason` is the only part that touches the core API, so it's in.
