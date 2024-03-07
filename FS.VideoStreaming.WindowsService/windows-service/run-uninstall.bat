set serviceDisplayName=FS.VideoStreaming.WindowsService
sc stop %serviceDisplayName%
sc delete %serviceDisplayName%
pause