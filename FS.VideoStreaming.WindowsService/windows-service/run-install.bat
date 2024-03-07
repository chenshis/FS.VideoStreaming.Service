set serviceDisplayName=FS.VideoStreaming.WindowsService
set serviceName=FS.VideoStreaming.WindowsService.exe
set servicArgs= --windows-service
set batPath=%~dp0%
set exePath=%batPath:windows-service\=%
sc create %serviceDisplayName% binPath= "%exePath%%serviceName%%servicArgs%" start= auto
sc description %serviceDisplayName% "|nickname|remark"
sc start %serviceDisplayName%
pause