Const ForReading = 1
Const ForWriting = 2
WScript.Echo "Installing NET Thingworx SDK Distribution."

' Read in comand line arguments
If WScript.Arguments.Count = 4 Then
    solutionDirectory = WScript.Arguments.Item(0)
    projectDirectory = WScript.Arguments.Item(1)
    targetDirectory = WScript.Arguments.Item(2)
    configurationName = WScript.Arguments.Item(3)
Else
    Wscript.Echo "Usage: the distribution.vbs script requires 4 arguments"
    Wscript.Quit(-1)
End If

' Arguments - uncomment to debug
'Wscript.Echo "--- Command Line Arguments ---"
'Wscript.Echo "solutionDirectory="&solutionDirectory
'Wscript.Echo "projectDirectory="&projectDirectory
Wscript.Echo "targetDirectory="&targetDirectory
'Wscript.Echo "gitBranch="&gitBranch
'Wscript.Echo "version="&version
'Wscript.Echo "artifactName="&artifactName

' Derived paths
dotNetDirectory = solutionDirectory &"DotNetSDK\bin\"+configurationName+"\"

' Calculated Fields - Uncomment to debug
Wscript.Echo "dotNetDirectory="&dotNetDirectory

' 32 and 64 Bit twApi.dll to SDK into the targetDirectory for this build
CreateFolderRecursive targetDirectory & "tw"
CreateFolderRecursive targetDirectory & "tw\x86"
CreateFolderRecursive targetDirectory & "tw\x64"
CopyFile dotNetDirectory&"tw\x86\twApi.dll",targetDirectory & "tw\x86\"
CopyFile dotNetDirectory&"tw\x64\twApi.dll",targetDirectory & "tw\x64\"
CopyFile dotNetDirectory&"thingworx-dotnet-common.dll",targetDirectory
CopyFile dotNetDirectory&"thingworx-dotnet-common.pdb",targetDirectory 
CopyFile dotNetDirectory&"thingworx-dotnet-common.xml",targetDirectory

Function CreateFolderRecursive(FullPath)
  Dim arr, dir, path
  Dim oFs

  Set oFs = WScript.CreateObject("Scripting.FileSystemObject")
  arr = split(FullPath, "\")
  path = ""
  For Each dir In arr
    If path <> "" Then path = path & "\"
    path = path & dir     
    If oFs.FolderExists(path) = False Then 
        Wscript.Echo "Creating "&path
        oFs.CreateFolder(path)
    End If
  Next
End Function

Function shellExecute(command, arguments)
    set objShell = CreateObject("shell.application")
    objShell.ShellExecute command,arguments, "", "open", 0
    set objShell = nothing
End Function 

Function deleteDirectoryAndAllFiles(path)' "cmd.exe", "/k dir"
    shellExecute "cmd.exe","/k RMDIR /S /Q "&path
End Function

Function deleteFilesMatching(path, pattern)
    shellExecute "cmd.exe","/k DEL /F /S /Q "&path&"\"&pattern
End Function

Function copyFilesLike(sourcePath,pattern,destiniationPath)
    'wscript.echo  "XCOPY /Y /K /R """&sourcePath&pattern&""" """&destiniationPath&"""" 
    shellExecute "XCOPY","/Y /K /R """&sourcePath&pattern&""" """&destiniationPath&"""" 
end Function

Function CopyFile(fromPath,toPath)
  Dim oFs
  on error resume next
  Set oFs = WScript.CreateObject("Scripting.FileSystemObject")
  Wscript.Echo "Copying File from="&fromPath&" to="&toPath 
  oFs.CopyFile fromPath,toPath
  If Err.Number <> 0 Then
	Wscript.Echo "Error copying from="&fromPath&" to="&toPath
	Wscript.Quit 1
  End If
  on error goto 0
  set oFs = nothing
End Function

Function CopyFolder(fromPath,toPath)
  Dim oFs
  on error resume next
  Set oFs = WScript.CreateObject("Scripting.FileSystemObject") 
  oFs.CopyFolder fromPath,toPath
  If Err.Number <> 0 Then
	Wscript.Echo "Error copying from="&fromPath&" to="&toPath
	Wscript.Quit 1
  End If
  on error goto 0
  set oFs = nothing
End Function
