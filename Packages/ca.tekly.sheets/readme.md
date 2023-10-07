## About
- This tool will download a Google Spreadsheet and turn it into JSON or other data formats
- The downloading and processing happens inside of your Unity project
- You need to give the tool readonly access to your spreadsheets
- You must set up your sheet in a certain way for it to be interpreted into JSON

## Setting up your sheet
- The tool uses the headings of each column to determine the layout of your JSON. You can sort of think of it as paths.
- The headings are used to generate the names of the fields
- Having a value in the first column indicates when a new object starts
- `.` or `:` indicate a sub property
- `|` indicators an array
- Arrays in nested objects are not supported
    - You can have an array of objects but those objects cannot contain arrays
- Sheet names that start with `//` are ignored
- Column names that start with `//` are ignored
- See the table and JSON below for an example

### Example
| Id    | User.Name | User.Email      | Permissions\|Resource | Permissions\|Role | Permissions\|Metadata.DateGiven | Permissions\|Metadata.Author |
|-------|-----------|-----------------|-----------------------|-------------------|---------------------------------|------------------------------|
| jeff  | JeffB     | jeffb@email.com | Databases             | Admin             | 10/01/2023                      | SYSTEM ADMIN                 |
|       |           |                 | AppManagement         | Viewer            | 10/10/2023                      | SYSTEM ADMIN                 |
| mark  | MarkG     | mark@email.com  | AppManagement         | Admin             | 6/27/2023                       | SYSTEM ADMIN                 |
| laura | LauraS    | laura@email.com | Databases             | Admin             | 10/23/2022                      | SYSTEM ADMIN                 |                   |


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
				"Role": "Admin",
				"Metadata": {
					"DateGiven": "10/01/2023",
					"Author": "SYSTEM ADMIN"
				}
			},
			{
				"Resource": "AppManagement",
				"Role": "Viewer",
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
				"Role": "Admin",
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
				"Role": "Admin",
				"Metadata": {
					"DateGiven": "10/23/2022",
					"Author": "SYSTEM ADMIN"
				}
			}
		]
	}
]
```

## How to point the tool to your sheets
- Create a `GoogleSheetObject` in your Assets directory. Right click on a folder `Tekly/Sheets/Google Sheet`
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

