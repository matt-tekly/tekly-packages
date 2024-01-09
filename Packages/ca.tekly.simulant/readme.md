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
- This is primarily procedural based games and will not convert scenes to Entities

## Implementation

Main Concepts

### Types 
- **World**: Manages Entities, DataPool, and Queries
- **DataPool**: A pool of components attached to Entities
  - Components must be structs and ideally blittable
- **Query**: Given a list of components an entity includes/excludes this will keep track of all entities matching that pattern
  - Identical includes/excludes result in the exact same Query object
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

Queries

```csharp
// Simple "system" that moves an entity based on its velocity
var query = world.Query().Include<TransformData, VelocityData>().Build();

foreach (var entity in query) {
	ref var transformData = ref world.Get<TransformData>(entity);
	ref var velocityData = ref world.Get<VelocityData>(entity);

	transformData.Position += velocityData.Velocity * Time.deltaTime;
}
```

## Serialization
- The world can be serialized with Super Serial
- It supports blittable and regular structs
  - Regular structs are significantly slower and generate garbage unless you implement `ISuperSerialize`
- Once a blittable type is serialized it only supports having fields added - not removed
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

- Systems
  - There is no systems structure. Should Simulant provide this or leave it up to the user?
- Serialization should support ignoring certain components
- Make a query object that can destructure into the components you want to modify
- There are a bunch of constants for capacity that need to be configurable
- Should queries be able to be disposed/disconnected?
- Check for leaked Entities
  - A leaked entity is an entity that exists with no components
- Destroying the entity when it has no more components can seem a little weird
  - Not sure this is the right approach
- Test an integration with Data Models