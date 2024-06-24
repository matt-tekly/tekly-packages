local CONFIG = ".aseconfig"

function dump(o)
    if type(o) == 'table' then
       local s = '{ '
       for k,v in pairs(o) do
          if type(k) ~= 'number' then k = '"'..k..'"' end
          s = s .. '['..k..'] = ' .. dump(v) .. ','
       end
       return s .. '} '
    else
       return tostring(o)
    end
 end

function file_exists(name)
	local f = io.open(name, "r")
	return f ~= nil and io.close(f)
end

function file_path(str)    return str:match("(.*[/\\])")
end

function read_file(path)
    local file = io.open(path, "rb")
    if not file then return nil end
    local content = file:read "*a"
    file:close()
    return content
end

function generateRootFilename(spriteFileName, rootDir, config)
    local rootUnity = rootDir .. config
    local localSpriteFile = spriteFileName:sub(#rootDir + 1):gsub(".aseprite", "")

    return rootUnity .. '/' .. localSpriteFile
end

function findConfig(filename)
    local path = file_path(filename);

    while (path) do
        local configPath = path .. CONFIG;
        local configExists = file_exists(configPath)

        if configExists then
            local config = read_file(configPath)
            return generateRootFilename(filename, path, config)
        end

        path = file_path(path:sub(1, -2))
    end

    print("Failed to find settings")
end

function file_name(url)
    return url:match("^.+/(.+)$")
end

