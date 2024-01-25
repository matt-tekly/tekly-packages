# About
Simulant is a relatively simple ECS.

**Goals:**
- A data oriented approach to simulating a game
- Simple implementation and simple to use
- Fast and simple serialization
- Still able to use GameObjects
- Can integrate Jobs where necessary
- Easy extensibility to core concepts

**Non Goals:**
- This is not a replacement for Unity's ECS. It will never compete with Unity on a performance level
  - A nice way to develop games is more important than performance
- This is primarily for procedural based games and will not convert scenes to Entities

## Extensions
See the Tekly Simulant Extensions and Tekly Simulant Templates packages for more built in behaviour.

## Implementation

Main Concepts

### Types 
- **World**: Manages Entities, DataPools, and Queries
- **DataPool**: A pool of components attached to Entities
  - Components must be structs and ideally blittable
- **Query**: Given a list of components an entity includes/excludes this will keep track of all entities matching that pattern
  - Identical includes/excludes result in the exact same Query object
  - You can iterate Querys
- **Entity**: Is just an integer ID

Querys, DataPools and World all work with tightly packed data arrays that have a corresponding sparse array that maps an entity to an index in the tightly packed array

## Usage

```csharp
// Create a world. You can create your own WorldConfig to customize initial entity and component capacity
var world = new World(WorldConfig.Default);

// Create an entity
var entity = world.Create();

// Delete an Entity
world.Delete(entity);

// Add a component: Version 1
world.Add(entity, new TransformData { Position = new Vector3(1, 2, 3) });

// Add a component: Version 2
ref var data = ref world.Add<TransformData>(entity);
data.Position = new Vector3(1, 2, 3);

// Add a component: Version 3 - the other versions are just slower version of this
var transformPool = world.GetPool<TransformData>();
ref var data = ref transformPool.Add(entity);
data.Position = new Vector3(1, 2, 3);

// Remove a component: Version 1
world.Delete<TransformData>(entity);

// Remove a component: Version 2 - the other versions are just slower versions of this
var transformPool = world.GetPool<TransformData>();
transformPool.Delete(entity);
```

### Queries

There are multiple ways you can iterate over a query.

Below are different examples of implementing the same Velocity system that modifies a transform.

```csharp
// This is the most performant version because it caches references to the query and pools
var query = world.Query<TransformData, VelocityData>();
var transforms = world.GetPool<TransformData>();
var velocities = world.GetPool<VelocityData>();

foreach (var entity in query) {
	ref var transformData = ref transforms.Get(entity);
	ref var velocityData = ref velocities.Get(entity);

	transformData.Position += velocityData.Velocity * Time.deltaTime;
}
```

```csharp
// This example doesn't cache the pools or query, but is very concise
// It doesn't have much overhead if the number of components you're iterating is large
world.ForEach((ref TransformData transformData, ref VelocityData velocityData) => {
    transformData.Position += velocityData.Velocity * Time.deltaTime;
});

// You can also get the entity if you need it
world.ForEach((int entity, ref TransformData transformData, ref VelocityData velocityData) => {
    transformData.Position += velocityData.Velocity * Time.deltaTime;
});
```

### Data Interfaces
There are a few interfaces you can optionally implement on your data components.

```csharp
public struct MyData : IInit, IRecycle
{
	public List<GameObject> SomeObjects;

	// Will be called when the data is added to an entity
	public void Init()
	{
		SomeObjects = new List<GameObject>();
	}

	// Will be called when the data is removed from an entity
	public void Recycle()
	{
		SomeObjects.Clear();
	}
}

// This data will be set to "default" when it is removed from an entity
public struct RecycleToDefault : IAutoRecycle { }
```

## Serialization
- The world can be serialized with Super Serial
- It supports blittable and regular structs
  - Regular structs are significantly slower and generate garbage unless you implement `ISuperSerialize`
- Once a blittable type is serialized it only supports having fields added - not removed or reordered
- If you find you have too many unused fields in your type you could create a new type with only the data you need and deprecate the old type.
  - Post deserialization you would copy the data from one DataPool to the other and remove the old DataPool

```csharp
private void Write(World world, string file)
{
	using var fileStream = File.OpenWrite(file);
	using var outputStream = new TokenOutputStream(fileStream);

	var serializer = new SuperSerializer();
	serializer.Write(outputStream, world);
	outputStream.Flush();
}

public World Read(string file)
{
	using var fs = File.OpenRead(file);
	using var inputStream = new TokenInputStream(fs, true);
	var world = new World(WorldConfig.Default);

	m_serializer.Read(inputStream, world);

	return world;
}
```

### Todo

- There are a bunch of constants for capacity that need to be configurable
- Should queries be able to be disposed/disconnected?
- Check for leaked Entities
  - A leaked entity is an entity that exists with no components
- Test an integration with Data Models
