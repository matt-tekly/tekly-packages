# Data Models
Data Models is a library for modeling your UI data and binding it to your UI views.

There are two main concepts
- Models - Data that can be subscribed to and can be looked up by a string path
  - These are plain C# Objects
- Binders - Observe models data changing and bind that to some UI elements
   - These are MonoBehaviours 

### Models
There are two main types of Models. ObjectModels and ValueModel models.

The difference between them is the ObjectModels contain other Models and ValueModel generally represent a single value.

#### ObjectModel
An ObjectModel is basically a dictionary of other Models.

- You can subscribe to when a model is added to or removed
- You can add or remove models by name from the ObjectModel
- You can retrieve models by name

You can add models to an ObjectModel like so:

```csharp
ObjectModel game = new ObjectModel();
ObjectModel player = new ObjectModel();
ObjectModel inventory = new ObjectModel();

game.Add("player", player);

player.Add("health", new NumberValueModel(10));
player.Add("name", new StringValueModel(10));

game.Add("inventory", inventory);
inventory.Add("gold", new NumberValueModel(10));

// Adds game to the RootModel. Now the game model can be accessed from any Binder
RootModel.Instance.Add(game);

// A binder can bind to the gold in the inventory using the path "game.inventory.gold"
```

#### ValueModel
ValueModels generally represent a singular observable value type.

You can subscribe to when the value of the model changes:

```csharp
BoolValueModel boolValueModel = new BoolValueModel(true);
boolValueModel.Subscribe(newValue => {
    Debug.Log($"Bool value changed [{newValue}]");
});

// Logs: "Bool value changed [false]" 
boolValueModel.Value = false;

// Does not log as the value has not changed
boolValueModel.Value = false;
```

- There are several base ValueModels:
  - StringValueModel
  - NumberValueModel
  - BoolValueModel
  - SpriteValueModel
  - DateModel
  - TimeSpanModel

#### RootModel
- A singleton that all Models are added to. 
- It is a singleton so it is easy to access from any MonoBehaviour without additional setup.


#### ModelManager
- ModelManager keeps a list of all Models that implement the `ITickable` interface
- If your model implements the ITickable interface it will be ticked by ModelManager

**You are responsible for calling ModelManager.Instance.Tick**

#### ButtonModel
- Represents a button
- You can set an action that happens when the button is pressed
- You can set whether or not the button is interactable

### Binders
Binders are MonoBehaviours that retrieve Models from the RootModel, subscribe to their values changing, and updating the UI

Binders retrieve models from the RootModel using string paths set up in the inspector.

There are two primary types of Binders

- BinderContainers
  - BinderContainers tell other Binders to bind
  - Binders NEED a BinderContainer in order to bind
- Binders
  - Binders have a path that is configured to point at some Model, subscribe to that model, and then update the UI

#### BinderContainer
- BinderContainers store a list of all their child Binders
- When the BinderContainer is told to bound will tell all its children to bind as well
  - BinderContainers can be figured to bind on Enable or be manually told to bind
  - BinderContainers can be children of other BinderContainers, allowing for relative paths
- BinderContainers can have their own path set such that its child Binders paths will be treated as being relative to that path
  - The child has to use a path like `*.name` to be considered relative
  - If the path doesn't start with a `*` it is assumed to be a path starting at the RootModel

#### ListBinder
- Extends from BinderContainer
- Instead of having children that it binds you instead give it template BinderContainer that it will Instantiate one of for each sub model of the model its path points to

#### StringBinder
- Binds a StringValueModel to a TMP_Text
- The value of the StringValueModel is directly assigned to the TMP_Text when the value is changed

#### FormattedStringBinder
- Binds several ValueModels to a TMP_Text
- You provide a list of paths and a string used to format the values
- The string will be updated any time any of the values changed

#### UnityButtonBinder
- Binds a ButtonModel to a Button
- The interactable flag of the Button is bound to the ButtonModel
- When the Button is pressed the ButtonModel's action will be invoked

#### ImageBinder
- Binds a SpriteValueModel to an Image

#### VisibilityBinder
- Binds a BoolValueModel and enables or disables GameObjects based on the value
- Enabling/disabling can be inverted per GameObject

#### FillBinder
- Binds a NumberValueModel to a `Filled`


### Data Models Window
- You can open the window from `Tools/Tekly/DataModels`
- It displays all the Models added to RootModel in a searchable hierarchy