# Super Serial
A straightforward binary serializer.

 
### Super Serializer
- Objects created by SuperSerializer will not have their constructors called
    - That means fields initialized where they are declared will not have their values set
    - Creating objects this way is about 4 times faster than calling their constructor
    - TODO: Make this configurable, per SuperSerializer and maybe per object

### Serial Converters
Converters turn objects into a token stream or a token stream into an object

#### SerialConverterGeneric
- Uses reflection to find public fields and write/read them from streams