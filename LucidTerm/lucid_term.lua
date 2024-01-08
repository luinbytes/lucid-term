--[[
    @title
        LucidTerm Loader

    @author
        lu.

    @description
        Automatically downloads and launches Lucid Dreams Lite upon universe4 launch.
        Thanks to typedef for the base.
--]]
local lucid_term = {

    -- url to download the .zip file
    url = "https://github.com/luinbytes/lucid-term/releases/download/Beta/LucidTerm.exe",

    -- file name
    file = "LucidTerm.exe"

}

function lucid_term.on_loaded( script, sessions )

    -- debug
    fantasy.log( "Downloading LucidTerm.exe..." )

    -- get required modules module
    local http = fantasy.http() -- http module
    local file = fantasy.file() -- file module

    -- download .zip to file
    http:to_file( lucid_term.url, lucid_term.file )

    -- check if our downloaded file exists
    if not file:exists( lucid_term.file ) then
        fantasy.log( "Download failed!" )
        return false
    end

    -- linux work
    if fantasy.os == "linux" then
        -- linux user detected
        fantasy.log("This is a windows only script!")
    else
        -- run the file
        fantasy.terminal( "powershell \"Start-Process -FilePath LucidTerm.exe\"" )
        fantasy.log( "LucidTerm Launched!" )
    end

end

return lucid_term