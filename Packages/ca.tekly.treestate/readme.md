## Tree States

Tree States is a hierarchical state machine package.

Hierarchical state machines are state machines whose states themselves can be other state machines.

### Intended Usage
* Tree States are used to model the state of the Application rather than of an individual entity
* It should be used as an orchestration layer
  * You will want to share data/state and logic across TreeStates and the best way to do that is by not having a TreeState own that data
  * You should keep most logic and data in services that can be shared across states

### Big Picture
* TreeStateMachine is derived from TreeState and inherits all its behaviour
* Each TreeState has its own internal ActivityMode and has many lifecycle events similar to a MonoBehaviours events
* TreeStates can have TreeStateActivities attached to them in the same way GameObjects and MonoBehaviours work
  * TreeStates/TreeStateMachines are the GameObjects and TreeStateActivities are the MonoBehaviours in this analogy
  * To add custom behaviour to a TreeState rather than deriving from TreeState you derive from TreeStateActivity
  * A TreeState can have any amount of TreeStateActivities
  * They get all the same events a TreeState does
* Transitioning between states is an asynchronous sequence
  * The sequence of which states will load and unload is determined when the transition is requested
  * Each state can define its transitions which are a trigger name to TreeState pair
  * The transition trigger name is bubbled up from the active leaf state until it finds a state with that trigger name
* Tree State Machines are authored in the GameObject hierarchy but don't use regular MonoBehaviour events
  * TreeState, TreeStateMachine, TreeStateActivity are all MonoBehaviours so the authoring experience is familiar


### Setup
You can right click in the Hierarchy and use the `Tekly/Tree State Machine` menu to create a TreeStateMachine with a few states

If you were to do it manually:

- Create a GameObject and add a TreeStateManager
  - TreeStateManager actually updates everything
- Add a TreeStateMachine to that GameObject and assign it to TreeStateManager's StateMachine
- Create a child GameObject and add a TreeState
- Assign that TreeState to the TreeStateMachine's DefaultState

### TreeState ActivityModes

* Each TreeState/TreeStateActivity has its own modes, which is a kind of mini state machine
* A TreeState is considered done loading or unloading when all of its TreeStateActivities are done loading or unloading
* An individual TreeState can be configured to load its Activities in Parallel or in Sequence

| ActivityMode    | Description                                                                                                                                   |
|-----------------|-----------------------------------------------------------------------------------------------------------------------------------------------|
| Inactive        | Inactive and doing nothing                                                                                                                    |
| Loading         | Loading                                                                                                                                       |
| ReadyToActivate | Done loading but the transition isn't finished. Once all TreeStates in the transition are ReadyToActive all states in the transition Activate |
| Active          | Active                                                                                                                                        |
| Unloading       | Unloading                                                                                                                                     |

### Transitions

Each state can define named transitions between other states. Ideally a state only defines transitions between its siblings or children.

Transitioning between states is done as a sequence of unloading the current states and loading the target states

In your activity you can call `TreeState.HandleTransition("trigger_name")` to start a transition. 

This is the order of operations that occurs from there

1. Find the closest TreeState to the active state with a "trigger_name" transition
2. Set that transition's state as the pending state to transition to on next update
3. Determine what states need to be unloaded and loaded
   * This is done by finding the first common ancestor between the active state and the target state
   * If the target state is a TreeStateMachine the TreeStateMachine's DefaultState will be added to the transition
   * This happens until the target state is a TreeState
4. All states between the current state and common ancestor are unloaded
   * This is done in reverse to how they were initially loaded
5. All states between the common ancestor and target state are then loaded

### TreeStateActivities

Derive from TreeStateActivity and add your activity to a TreeState in the Inspector

TreeStateActivity has several functions you can overload

```csharp
// Called just before LoadingStarted. The major purpose of this is for Activities to inject their dependencies into themselves. 
// You can derive from InjectableActivity to have this happen automatically.
protected virtual void PreLoad() 

protected virtual void LoadingStarted()
protected virtual bool IsDoneLoading() // Return true when your activity is done loading. This defaults to returning true immediately
protected virtual void LoadingUpdate()

protected virtual void ReadyToActivateUpdate()

protected virtual void ActiveStarted()
protected virtual void ActiveUpdate()

protected virtual void UnloadingStarted()
protected virtual bool IsDoneUnloading() // Return true when your activity is done unloading. This defaults to returning true immediately
protected virtual void UnloadingUpdate()

protected virtual void InactiveStarted()
protected virtual void PostInactive()
```

### Injection
* Tree State integrates with the Tekly dependency injection library `Tekly Injectors`
* Adding an `InjectorContainerState` to your TreeState creates a new `InjectorContainer` scoped to that state
  * It will create the InjectorContainer state on PreLoad
  * If will search for the closest parent InjectorContainerState and use that as the parent of the InjectorContainer
  * This allows for access of the parent's content without polluting it
  * To populate the container use `ScriptableBindings` or your TreeStateActivities can implement `IInjectionProvider`
  * `IInjectionProvider` is generally simpler
  * The container will be cleared when the InjectorContainerState is deactivated
* Derive from `InjectableActivity` instead of `TreeStateActivity` to have your activity be injected before LoadingStarted is called.
* `InjectorContainerState` can use a `InjectorContainerRef` to be used as a parent and/or the target InjectorContainer to populate
