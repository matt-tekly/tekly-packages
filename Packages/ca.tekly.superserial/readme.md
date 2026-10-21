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
  Declared field and element types register themselves as converters are built, and the generated registries
  register every concrete type that can sit in a `[SuperSerializable]` type's polymorphic fields. Anything
  else that only ever appears as a value must be registered on the reading side with
  `serializer.RegisterType<T>()` before loading. Loading an unregistered type throws rather than guessing.
- Types that implement `ISuperSerialize` and write themselves with the raw stream API.
- `[SuperSerializable]` partial types, whose `ISuperSerialize` is written by a source generator. No reflection,
  no boxing for the common field types, and you can read the code that runs. See [Generated serialization](#generated-serialization).

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
| Object | 5 | fields ended by hash 0 | classes and structs handled by the reflection converter. Generated types read it too, so a type can be annotated after its data was saved |
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

## Generated serialization
Mark a partial class or struct `[SuperSerializable]` and the source generator in `Analyzers/` writes its `Write`
and `Read` for it. The data layout is the same as the reflection converter's, so a type can be annotated after
data was saved and still load it.

```csharp
namespace Game
{
	[SuperSerializable]
	public partial class Player
	{
		public int Health = 100;
		public string Name;
		public Vector3 Position;
		public Rarity Rarity = Rarity.Common;
		public List<Item> Items;
		[SerializeField] private int m_secret;
	}
}
```

Field rules are the reflection converter's: public fields are in, private fields opt in with `[SerializeField]`
or `[SerialName]`, `[NonSerialized]` opts out. Fields of base classes are included when the derived type can
see them; a private base field is skipped with a warning. Readonly fields are skipped with a warning, since the
generated `Read` cannot assign them.

### What is inlined
`bool`, every integer type, `char`, `float`, `double`, `string`, `DateTime`, enums, `Vector2`, `Vector3`,
`Quaternion`, nullables of those, nested `ISuperSerialize` structs and nested sealed `[SuperSerializable]`
classes are written and read with direct stream calls. Everything else (`List<T>`, arrays, dictionaries,
non-sealed classes, `decimal`, `Guid`, `Color`, and so on) goes through `superSerializer.WriteValue` and
`ReadPayload`, which box but otherwise behave exactly as the reflection converter does.

### Defaults
At the top of the generated `Read`, every field is reset to its declared default before the data is applied.
That makes "the field isn't in the data" mean "the field keeps its initializer", for a fresh object and for an
existing object alike, without `ObjectFactory` or `CallConstructors`. An initializer is reproduced when it is a
constant, a literal, an enum member, a static readonly member or static property such as `Vector3.zero`, or
`default`. A field with no initializer resets to `default`. A field whose initializer is anything else (a
method call, `new List<int>()`) is left alone and keeps whatever construction gave it.

`[SuperSerializable(OmitDefaults = true)]` leaves out every value typed or string field that equals its declared
default, and `[SerialOptional]` does the same for one field. On read the field is reset first, so an omitted
field comes back as its default. The rules:
- Value types and strings compare against the declared default: `Health != 100`, `Rarity != Rarity.Common`,
  `!Position.Equals(Vector3.zero)`. A nullable with no initializer is omitted when it has no value.
- Strings treat `null` and `""` as the same empty value. If the initializer is `""`, a null string is omitted
  and reads back as `""`; if there is no initializer, an empty string reads back as `null`.
- A class or collection typed field is never omitted by `OmitDefaults`. With `[SerialOptional]` it is omitted
  only when it is null; an empty list is still written.
- An omit-defaults field with an initializer the generator can't reproduce is an error (`SUPERSERIAL007`).

### Construction
Each generated class gets `public static T CreateForSerialization() => new T();`, so the serializer never goes
through `ObjectFactory` for it and initializers always run. The generated registry hands that factory to the
serializer; without `RegisterAll` the converter finds it by name once per type. Structs are created with
`default` and then reset field by field. The class needs a public or internal parameterless constructor.

### The registry
Each assembly that contains `[SuperSerializable]` types gets one
`Tekly.SuperSerial.Generated.SuperSerialRegistry_<AssemblyName>`. Its `RegisterAll(serializer)`:
- registers the type name hash of every `[SuperSerializable]` type, every `[SerialName]` type, and every
  concrete type in the assembly that can be assigned to a class or interface typed field, list element, array
  element or dictionary key or value of a `[SuperSerializable]` type, so polymorphic values can be recreated;
- registers each generated class's `CreateForSerialization`;
- installs ref based `Write` and `Read` delegates for each generated struct in `SuperSerializeStructs<T>`, so
  Simulant's `DataPool<T>` serializes components in place with no boxing and no reflection. A hand written
  `ISuperSerialize` struct that nobody registers still works, through a boxing fallback.

You don't normally call `RegisterAll` yourself. Each registry announces itself to `SuperSerialRegistries` from
a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`, which runs before the first scene on Mono and IL2CPP,
and from an `[InitializeOnLoadMethod]` in the editor, which runs on every domain reload and so covers EditMode
tests and tooling. A new `SuperSerializer` applies every announced registry, and when it meets a type hash it
doesn't know it checks for registries that announced themselves since before giving up. Set
`Settings.AutoRegister = false` to opt out and call `RegisterAll` by hand; the tests do this to prove the
unregistered path still throws. The registry class carries `[Preserve]` so managed stripping keeps it, and the
types it names are kept because it references them. C# 9 module initializers would be the natural hook, but
Unity lists them as unsupported, so the generator doesn't rely on them.

`object` typed fields are left out of the polymorphic scan, since every type would match. Give the concrete
types that can end up in one `[SerialName]` (or `[SuperSerializable]`) and the registry picks them up. The
registry only names public and internal types; a private nested type is still generated but has to be
registered by hand.

### Generated output
This is the generated code for the `Player` above, verbatim, minus the header comment and usings.

```csharp
namespace Game
{
	partial class Player : ISuperSerialGenerated
	{
		/// <summary>
		/// Used by the serializer to create an instance, so field initializers run and no reflection is needed.
		/// </summary>
		public static Player CreateForSerialization() => new Player();

		public virtual void Write(TokenOutputStream output, SuperSerializer superSerializer)
		{
			output.WriteField(0xE799CB45u, Health); // Health
			output.WriteField(0xD922A55Bu, Name); // Name
			output.WriteField(0x427481C2u, Position); // Position
			output.WriteField(0xC8D6A28Eu, (long) Rarity); // Rarity
			output.WriteFieldHash(0xF1A321F9u); // Items
			superSerializer.WriteValue(output, Items);
			output.WriteField(0x7B36C920u, m_secret); // m_secret

			output.WriteEndObject();
		}

		public virtual void Read(TokenInputStream input, SuperSerializer superSerializer)
		{
			// A field that isn't in the data keeps its declared default.
			Health = 100;
			Name = null;
			Position = default;
			Rarity = Game.Rarity.Common;
			Items = null;
			m_secret = default;

			while (true) {
				var hash = input.ReadFieldHash();
				if (hash == 0) {
					break;
				}

				var wireType = input.ReadWireType();

				switch (hash) {
					case 0xE799CB45u: // Health
						if (wireType == WireType.Varint) {
							Health = checked((int) input.ReadVarInt());
						} else {
							Health = superSerializer.ReadPayload(input, wireType, Health);
						}
						break;
					case 0xD922A55Bu: // Name
						if (wireType == WireType.Bytes) {
							Name = input.ReadUtf8();
						} else {
							Name = superSerializer.ReadPayload(input, wireType, Name);
						}
						break;
					case 0x427481C2u: // Position
						if (wireType == WireType.Bytes) {
							if (input.ExpectLength(sizeof(float) * 3)) {
								Position = input.ReadVector3();
							}
						} else {
							Position = superSerializer.ReadPayload(input, wireType, Position);
						}
						break;
					case 0xC8D6A28Eu: // Rarity
						if (wireType == WireType.Varint) {
							Rarity = (Game.Rarity) input.ReadVarInt();
						} else {
							Rarity = superSerializer.ReadPayload(input, wireType, Rarity);
						}
						break;
					case 0xF1A321F9u: // Items
						Items = superSerializer.ReadPayload(input, wireType, Items);
						break;
					case 0x7B36C920u: // m_secret
						if (wireType == WireType.Varint) {
							m_secret = checked((int) input.ReadVarInt());
						} else {
							m_secret = superSerializer.ReadPayload(input, wireType, m_secret);
						}
						break;
					default:
						input.Skip(wireType);
						break;
				}
			}
		}
	}
}
```

The hashes are computed by the generator with the same XXHash32 the runtime uses, over the UTF-16 bytes of the
field name or `[SerialName]`. `ReadPayload` handles a wire type that doesn't match (the field's type changed:
skip with a warning), `Null`, `TypedObject` and `Block` framing. Unknown fields are skipped.

### Diagnostics
| Id | Error |
|---|---|
| SUPERSERIAL001 | The type isn't partial |
| SUPERSERIAL002 | A containing type isn't partial |
| SUPERSERIAL003 | A class has no public or internal parameterless constructor |
| SUPERSERIAL004 | Two fields hash to the same value |
| SUPERSERIAL005 | A field hashes to 0, the end of object marker |
| SUPERSERIAL006 | A field's type can't be serialized (pointer, delegate, multidimensional array, type parameter) |
| SUPERSERIAL007 | An omit-defaults field has an initializer the generator can't reproduce |
| SUPERSERIAL008 | The type is generic |
| SUPERSERIAL009 | Warning: a readonly field is skipped |
| SUPERSERIAL010 | Warning: a private base class field is skipped |

### Installing and rebuilding the generator
The generator is a Roslyn incremental generator in `Generator~/` (the trailing `~` keeps Unity from importing
it), built against Microsoft.CodeAnalysis 4.3, the Roslyn that Unity 6000.3 compiles with. The built DLL is
checked in at `Analyzers/Tekly.SuperSerial.Generator.dll` with the `RoslynAnalyzer` asset label and every platform
excluded.
Unity applies an analyzer that lives in a package to every assembly in the project, not just the package's own,
so nothing needs to be installed in a game that depends on this package. Verified on 6000.3.18: the DLL path
shows up in `CompilationPipeline.GetAssemblies()[*].compilerOptions.RoslynAnalyzerDllPaths` for
`Assembly-CSharp` and the Simulant assemblies.

After changing the generator, build it. Release is the default configuration and a post-build step copies the
DLL into `Analyzers/`:

```bash
dotnet build Packages/ca.tekly.superserial/Generator~
```

Then let Unity reimport. Unity's generated .csproj files pick the analyzer up as an `<Analyzer>` item, so the
IDE shows the generated files and diagnostics too.

## Object construction
`[SuperSerializable]` classes are created with their generated `CreateForSerialization`, so this section is about
everything else. Objects are created uninitialized by default, so constructors and field initializers don't run. It's several
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
