# Tekly Stagecraft

An actor runtime for Unity: pooled GameObject actors with one explicit lifecycle, deferred despawn, generation
handles, ordered opt-in ticking and batch systems. Built for crowds of short-lived objects (enemies, bullets,
pickups, effects). Design and the reasons behind it: `Documentation~/DESIGN.md`.

## Concepts

- `Stage` owns every actor. You drive it: `Tick` from Update, `FixedTick` from FixedUpdate (`StageRunner` does
  this for you) or from a netcode tick.
- `Actor` is a GameObject made of `ActorBehaviour` components, pooled per prefab. Only the stage changes its state.
- `ActorId` / `ActorRef<T>` are how you hold an actor. A ref reads null the moment the actor is despawned.
- `Despawn` is deferred: the actor is dead now, cleanup happens at the next sync point (start and end of every tick).
- `StageConfig` is the one place tick order is set. Only types that override `Tick`/`FixedTick` are scheduled.
- `ActorSystem<T>` gets every live `T` in one call. Hot paths go here.
- Physics is an adapter: `Tekly.Stagecraft.PhysicsCore2D` for the low-level 2D physics (`ActorBody2D`), a stub for 3D.

## The calls game code uses

```csharp
// 1. Build the stage once; order of registration is tick order
var config = new StageConfig()
    .Register<Hero>()
    .RegisterSystem<Enemy, EnemyMovementSystem>()   // created through the injector, [Inject] works
    .Register<Projectile>()
    .RegisterSystem<Pickup, PickupAttractionSystem>();

var stage = new Stage(config, container);
new PhysicsCore2DAdapter(stage);                      // optional: stage steps PhysicsWorld.defaultWorld
StageRunner.Create(stage);                            // ticks it from Update / FixedUpdate

// 2. Spawn: pose + optional typed args, delivered before OnSpawned
var enemy = stage.Spawn(enemyPrefab, new Pose(position, rotation), new EnemyArgs { Health = 10 });

// 3. Keep it: a ref, never a raw component
ActorRef<Enemy> target = enemy;
if (target.TryGet(out var e)) { e.TakeDamage(5); }    // false once it despawns

// 4. Despawn from anywhere; it's deferred and idempotent
enemy.Despawn();

// 5. Query, don't keep your own list
foreach (var pickup in stage.All<Pickup>()) { ... }
```

A behaviour:

```csharp
public struct EnemyArgs { public int Health; }

public class Enemy : ActorBehaviour, ISpawnArgs<EnemyArgs>, IContactListener2D
{
    [Inject] private Combat m_combat;                 // injected once per pooled instance, via the prefab's HierarchyInjector

    public void Initialize(in EnemyArgs args) { m_health = args.Health; }
    public override void OnCreated() { }              // once per pooled instance; cache components here, not in Awake
    public override void OnSpawned() { }              // alive, resolvable, not yet in the tick lists
    public override void FixedTick(float dt) { }      // gameplay (only scheduled because it's overridden)
    public override void OnDespawned() { }            // at the sync point after Despawn

    public void OnContactBegin(in Contact2D c)        // delivered after the physics step, despawning is safe
    {
        if (c.OtherBody != null) { c.OtherBody.Despawn(); }
    }
    public void OnContactEnd(in Contact2D c) { }
}
```

A system:

```csharp
public class EnemyMovementSystem : ActorSystem<Enemy>
{
    public override void FixedTick(ActorList<Enemy> enemies, float dt)
    {
        foreach (var enemy in enemies) { ... }        // skips anything despawned since the last sync
    }
}
```

## Netcode

Call `stage.FixedTickBegin(dt)` from the tick, step physics yourself (adapter in `PhysicsStepping.External`),
then `stage.FixedTickEnd()`. `stage.Acquire` / `Activate` / `Despawn` back an external object pool. Describe spawns
as `(ActorPrefabId, Pose, args struct)` through `stage.Prefabs`. See "Netcode seams" in the design note.

## Sample

`Assets/Samples/Stagecraft/stagecraft_scn.unity`: ~3000 actors spawning and despawning continuously, one type
ticked per object (`Wanderer`), one by a system (`Drifter`), blasts despawning through trigger events, a profiler
marker per tick group and a DevBoard page ("Sample/Stagecraft"). The actor prefabs next to the scene show the
inspector side: an `Actor`, an `ActorBody2D` with its shapes, and the behaviour. Each shape carries its contact
filter, so the blast's shape sits in its own category and excludes it from its contacts: two blasts never pair in
the physics step, instead of being filtered out in the listener. `Blast` takes a shared
`SampleStats` service with `[Inject]`, registered once on the container the stage was built with; its prefab
carries the `HierarchyInjector` that makes that happen.

## Injection

An actor is injected only if its root has a `HierarchyInjector` (from `ca.tekly.injectors`). The stage drives
it once per pooled instance, before `OnCreated`, with the stage's container as parent, so the injector's usual
rules apply: every `[Inject]` MonoBehaviour it found at edit time is injected, `IInjectionProvider`s on the root
provide into the per-actor container, and components can be registered from the inspector. Leave its
"inject on Awake / OnEnable" flags off; the stage logs an error if they are set. A prefab without an injector is
never injected.
