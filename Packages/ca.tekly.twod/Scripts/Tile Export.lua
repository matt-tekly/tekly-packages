dofile("./Extensions.lua")

local spr = app.activeSprite

if not spr then return print('No active sprite') end
if not spr.filename then return print('The file needs to saved') end 

local rootName = findConfig(spr.filename)

app.command.ExportSpriteSheet{
	ui=false,
	type=SpriteSheetType.HORIZONTAL,
	textureFilename=rootName .. '.png',
	dataFilename=rootName .. '.json',
	dataFormat=SpriteSheetDataFormat.JSON_ARRAY,
	filenameFormat="{title}_{tag}_{frame00}",
}

local fileName = file_name(rootName)

local zipFile = fileName .. '.tiles'
local pngFile = fileName .. '.png'
local jsonFile = fileName .. '.json'

-- Delete the old zip file
local commandDelete = 'cd ' .. file_path(rootName) .. ' && rm ' .. zipFile
os.execute(commandDelete)

-- Compress the png and json data into a zip
local command = 'cd ' .. file_path(rootName) .. ' && zip -m ' .. zipFile .. ' ' .. pngFile .. ' ' .. jsonFile
os.execute(command)