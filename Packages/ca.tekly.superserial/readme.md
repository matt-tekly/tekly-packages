# Super Serial
A straightforward binary serializer for save data.

```csharp
var serializer = new SuperSerializer();
serializer.Write(path, saveData);
var loaded = serializer.Read<SaveData>(path);
```

## What it handles
- Public fields, private fields marked `[SerializeField]` or `[SerialName]`. `[NonSerialized]` fields are skipped.
- All primitives, enums, nullables, `string`, `DateTime`, `TimeSpan`, `decimal`, `Guid`, `byte[]`
- `List<T>`, `T[]`, `Dictionary<K, V>`, nested objects and structs, nulls anywhere
- Vector2/3/4, Quaternion, Vector2Int/3Int, Color, Color32, Rect, Bounds
- Polymorphic fields. A value whose runtime type differs from its field's type carries a type name hash.
  Declared field and element types register themselves as converters are built, but a concrete subclass that
  only ever appears as a value must be registered on the reading side with `serializer.RegisterType<T>()`
  before loading. Loading an unregistered type throws rather than guessing.
- Types that implement `ISuperSerialize` and write themselves with the raw stream API.

## Format
Little endian throughout. Every value is self describing, so a reader can skip anything it doesn't recognise.

### Stream
| Bytes | Content |
|---|---|
| 4 | Magic `TKSS` |
| 1 | Format version, currently 2. Newer than this build, or older than `MIN_VERSION`, is rejected. |
| 1 | Flags, reserved |
| ... | One value |

### Value, field, object
| Thing | Layout |
|---|---|
| Value | `[WireType:u8][payload]` |
| Field | `[name hash:u32][Value]` |
| Object payload | Fields, one after another, ended by a name hash of `0` |

The name hash is XXHash32 of the field name, or of the `[SerialName]` if the field has one. Hash `0` is
reserved for the end marker and a field that hashes to it is a startup error.

### Wire types
| WireType | Byte | Payload | Used for |
|---|---|---|---|
| Null | 0 | none | null references, null nullables |
| Varint | 1 | LEB128 varint, see below | all integers, bool, char, enums |
| Fixed32 | 2 | 4 bytes | float |
| Fixed64 | 3 | 8 bytes | double, DateTime, TimeSpan |
| Bytes | 4 | `[length:varint][bytes]` | string (UTF-8), byte[], decimal, Guid, Vector2/3/4, Quaternion, Color, Rect, Bounds and other fixed size structs |
| Object | 5 | fields ended by hash 0 | classes and structs handled by the reflection converter |
| TypedObject | 6 | `[type hash:u32][Value]` | a value whose runtime type differs from its declared type |
| List | 7 | `[count:varint]` then count Values | List<T>, T[] |
| Map | 8 | `[count:varint]` then count key Value, value Value pairs | Dictionary<K,V> |
| Block | 9 | `[length:u32][bytes]` | ISuperSerialize values nested inside another value |
| Raw | 10 | untagged bytes, no length | an ISuperSerialize value at the top level of a stream only |

The type hash in TypedObject is XXHash32 of the type's full name, or its `[SerialName]`.

Block is a plain length prefix so a reader that understands less of the payload than the writer wrote still
lands on the next field, and a reader that reads past the block throws. Raw can't be skipped, which is why it's
only allowed at the top level.

### Integers
Integers are written as unsigned LEB128 varints: 7 bits per byte, low bits first, with the top bit set on every
byte except the last. Signed values are zigzag mapped first so small negative numbers stay small.

| Value | Varint bytes |
|---|---|
| 0 to 127 | 1 |
| 128 to 16,383 | 2 |
| 16,384 to 2,097,151 | 3 |
| any int | at most 5 |
| any long | at most 10 |

| Signed | Zigzag |
|---|---|
| 0 | 0 |
| -1 | 1 |
| 1 | 2 |
| -2 | 3 |
| 2 | 4 |

Zigzag is `(n << 1) ^ (n >> 63)` on write and `(u >> 1) ^ -(u & 1)` on read. `WriteVarInt`/`ReadVarInt` apply
it; `WriteVarUInt`/`ReadVarUInt` don't. Because every integer width shares one wire type, an `int` field can
become a `long` or an enum and old data still loads.

### What the format lets you change
- Add, remove or reorder fields. A missing field keeps its default.
- Change a field's type. The old value is skipped with a warning instead of corrupting what follows.
- Rename a field without losing data by pinning its name: `[SerialName("hp")] public int Health;`. The same
  attribute on a class pins its type name for polymorphic fields.
- Append more data to an `ISuperSerialize` type's Write. Older readers skip the extra bytes.

## Object construction
Objects are created uninitialized by default, so constructors and field initializers don't run. It's several
times faster, but a field that isn't in the data stays at its zero value. Set
`serializer.Settings.Factory.CallConstructors = true` to run parameterless constructors instead.

## Blittable arrays
`TokenOutputStream.WriteBlittable` writes an array of blittable structs as one block of memory, along with the
struct's field layout. If the struct changes, `ReadBlittable` matches fields by name and size and zeroes the
rest, so adding or reordering fields in a component is safe. Changing a field's type or size drops that field's
data.

## Custom converters
```csharp
serializer.RegisterConverter(new SerialConverterDelegate<MyType>(WireType.Varint,
	(stream, value) => stream.WriteVarInt(value.Id),
	stream => MyType.FromId((int) stream.ReadVarInt())));
```
Or subclass `SerialConverter<T>`. A converter writes only its payload and declares which `WireType` it uses.
