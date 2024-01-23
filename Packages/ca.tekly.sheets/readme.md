# About

This tool will turn a spreadsheet into JSON or ScriptableObjects.

The source of your spreadsheet can either be Google Sheets or Excel.

Setup steps for both sources are in their own sections below.

You must set up your sheet in a certain way for it to be interpreted into JSON

# Setting up your sheet
- The tool uses the headings of each column to determine the layout of your JSON. You can sort of think of it as paths.
- The headings are used to generate the names of the fields
- `.` or `:` indicate a sub property
- `|` indicates an array
- A header ending in `[]` indicates it is a comma separated list of values and will be turned into an array
- Arrays in nested objects are not supported
    - You can have an array of objects but those objects cannot contain arrays
    - You can have `[]` properties inside nested arrays of objects
- You can use fixed indices as part of paths
    - `Building.Costs.0.Item` `Building.Costs.0.Amount` `Building.Costs.1.Item` `Building.Costs.1.Amount`
    - This would make `Costs` an array where you're optionally setting the first two entries
    - Empty cells will not add an empty entry in the array
- **Having a value in the first column of a row indicates that a new object starts**
  - If your data has an identifier or key it should be in the first column
- Sheet names that start with `//` or `__` or `$`are ignored
- Column names that start with `//` or `__` or `$`are ignored
- Rows that start with `//` or `__` or `$` are ignored

See the class, table, and JSON below for an example

## Example

```csharp
public class UserPermissions
{
    public string Id;
    public UserDetails User;
    public Permission[] Permissions;
}

public class UserDetails
{
    public string Name;
    public string Email;
}

public class Permission
{
    public string Resource;
    public string[] Roles;
    public PermissionMetaData MetaData;
}

public class PermissionMetaData
{
    public string DateGiven;
    public string Author;
}
```

| Id    | User.Name | User.Email      | Permissions\|Resource | Permissions\|Roles[] | Permissions\|Metadata.DateGiven | Permissions\|Metadata.Author |
|-------|-----------|-----------------|-----------------------|----------------------|---------------------------------|------------------------------|
| jeff  | JeffB     | jeffb@email.com | Databases             | Admin                | 10/01/2023                      | SYSTEM ADMIN                 |
|       |           |                 | AppManagement         | Commenter,Viewer     | 10/10/2023                      | SYSTEM ADMIN                 |
| mark  | MarkG     | mark@email.com  | AppManagement         | Admin                | 6/27/2023                       | SYSTEM ADMIN                 |
| laura | LauraS    | laura@email.com | Databases             | Admin                | 10/23/2022                      | SYSTEM ADMIN                 |


```json
[
	{
		"Id": "jeff",
		"User": {
			"Name": "JeffB",
			"Email": "jeffb@email.com"
		},
		"Permissions": [
			{
				"Resource": "Databases",
				"Roles": ["Admin"],
				"Metadata": {
					"DateGiven": "10/01/2023",
					"Author": "SYSTEM ADMIN"
				}
			},
			{
				"Resource": "AppManagement",
				"Roles": ["Commenter", "Viewer"],
				"Metadata": {
					"DateGiven": "10/10/2023",
					"Author": "SYSTEM ADMIN"
				}
			}
		]
	},
	{
		"Id": "mark",
		"User": {
			"Name": "MarkG",
			"Email": "mark@email.com"
		},
		"Permissions": [
			{
				"Resource": "AppManagement",
				"Roles": ["Admin"],
				"Metadata": {
					"DateGiven": "6/27/2023",
					"Author": "SYSTEM ADMIN"
				}
			}
		]
	},
	{
		"Id": "laura",
		"User": {
			"Name": "LauraS",
			"Email": "laura@email.com"
		},
		"Permissions": [
			{
				"Resource": "Databases",
				"Role": ["Admin"],
				"Metadata": {
					"DateGiven": "10/23/2022",
					"Author": "SYSTEM ADMIN"
				}
			}
		]
	}
]
```

## Unity Supported Types
There are some built in convenient conversion of common types.

`Vector2`, `Vector3`, and `Quaternion` can be written in one cell as comma separated values or as one cell per field

The following are both valid ways to write a Vector3

| Position | Pos.x | Pos.y | Pos.z |
|----------|-------|-------|-------|
| 1,2,3    | 1     | 2     | 3     |

# Excel
- Place your spreadsheet in the Assets directory some where
- Create a `Json Processor Excel` asset from the Create menu in the project
- Click on your spreadsheet and assign the processor you just created to it
- The content of the spreadsheet will be imported into your project as text assets represent JSON
- You can create your own processor by deriving from `ExcelSheetProcessor`
- Check out `IdleSheetProcessor` in the Tekly Unity project for an example on how to turn your spreadsheet into ScriptableObjects

# Google Sheets

## How to point the tool to your sheets
- Create a `GoogleSheetObject` in your Assets directory: Right click on a folder in your project `Tekly/Sheets/Google Sheet`
- Enter in the ID of your sheet
  - You can get it from the URL of the sheet. The part between `/d/` and `/edit`
  - https://docs.google.com/spreadsheets/d/15yeM4l2SNquxrFlujtlX40YKsb2cl14h0aMl7sdWTkw/edit#gid=0
  - The id is `15yeM4l2SNquxrFlujtlX40YKsb2cl14h0aMl7sdWTkw`
- Create a `GoogleSheetProcessor` and assign to your GoogleSheetObject
  - There is one built in processor `JsonSheetProcessor` that allows your to convert the Sheet to JSON files
  - `AssetSheetProcessor` exists as a helper to create ScriptableObjects from your Sheet. You must subclass this. Check out `IdleSheetProcessor` in the Tekly Unity project for an example
- Assign the Credentials file from the access section below
  - This allows you to use different credentials for different sheets

## How to download the sheets - Sheet Fetcher Window
- The Sheet Fetcher Window looks for all of your `GoogleSheetObjects`
- It will present you with options to download one or all your `GoogleSheetObjects`
- Open it from `Tekly/Sheets/Fetcher`

## Giving access to your spreadsheets to the tool
1. Create a Google Project in the Google APIs console
    1. Enable the Google Sheets API for your project
1. Create a Service Account
    1. https://cloud.google.com/iam/docs/service-accounts-create
1. The service account just needs to be readonly for everything
1. Download the credentials file and put it some where in your Unity project
   1. Choose the JSON option
   2. The `GoogleSheetObjects` need to reference this in order to download sheets
1. Within Google Drive share your sheets or sheets directory with the service accounts email

# Customization

## Custom Converters
Sheets parses a Spreadsheet into `Dynamic` objects, which are structured like a JSON blob.

`DynamicConverter` is what turns a `Dynamic` object into the correct type. You can derive from `DynamicConverter` and
parse Types how you'd like.

Your `DynamicConverter` must:

1. Implement `CanConvert` to indicate that it is the converter for that type
2. Implement `Convert` to actually convert the `Dynamic` into an instance of the type 

Here is an example of a `DynamicConverter` that converts a string into `UnityEngine.Object` reference.
This allows you to just put the name of the asset in the sheet and it will be hooked up automatically in your data.

```csharp
public class DynamicConverterUnityAsset : DynamicConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(Object).IsAssignableFrom(objectType);
    }

    public override object Convert(DynamicSerializer serializer, Type type, object dyn, object existing)
    {
        var assetName = dyn.ToString();
        
        if (existing is Object obj && obj != null && obj.name == assetName) {
            return existing;
        }
        
        var asset = AssetDatabaseExt.FindAndLoadFirst(assetName, type);

        if (asset == null) {
            Debug.LogError($"Failed to find asset: [{assetName}]");
        }

        return asset;
    }
}
```

To add your own `DynamicConverter`

```csharp
// Put something similar in a class in an Editor folder
[InitializeOnLoadMethod]
private static void RegisterCustomConverters()
{
	DynamicExt.RegisterConverter(new MyCustomDynamicConverter());
}
```

