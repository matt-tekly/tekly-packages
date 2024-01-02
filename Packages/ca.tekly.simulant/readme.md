### Todo

- Make a way to look up an entity by an external ID
- Serialization
  - Maybe this is some sort of copy to other world thing
  - A pattern where graphics/prefabs components are removed but "GraphicsIsMissing" components are added
  - Should see if there is a blittable version
    - Some data could written to a blittable section and other could be Super Serial
  - The world needs to be serialized too
- Make a query object that can destructure into the components you want to modify
- There are a bunch of constants for capacity that need to be configurable
- Should queries be able to be disposed/disconnected?
- Check for leaked Entities
  - A leaked entity is an entity that exists with no components
- Destroying the entity when it has no more components can seem a little weird
  - Not sure this is the right approach
- Data Model tests

### Implementation
- Querys, DataPools and World all work with tightly packed data arrays that have a corresponding sparse array that maps an entity to an index in the tightly packed array
- Identical includes/excludes result in the exact same Query object


### Serialization
- World
  - Entities, Recycled?