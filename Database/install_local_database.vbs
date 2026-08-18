Option Explicit

Const ForAppending = 8

Dim fso
Dim shell
Set fso = CreateObject("Scripting.FileSystemObject")
Set shell = CreateObject("WScript.Shell")

Dim scriptDir
scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)

Dim dbName
dbName = "mtm_waitlist"

Dim username
Dim password
username = InputBox("MySQL username:", "MTM Waitlist Database Install", "root")
If Trim(username) = "" Then
    WScript.Echo "Install canceled: username is required."
    WScript.Quit 1
End If

password = InputBox("MySQL password for user '" & username & "':", "MTM Waitlist Database Install", "")

If InStr(password, Chr(34)) > 0 Then
    WScript.Echo "Install canceled: passwords containing a double quote character are not supported by this script."
    WScript.Quit 1
End If

Dim proceed
proceed = MsgBox("This will DROP and RECREATE database '" & dbName & "'." & vbCrLf & vbCrLf & _
    "Do you want to continue?", vbYesNo + vbExclamation, "Dangerous Operation")
If proceed <> vbYes Then
    WScript.Echo "Install canceled by user before database reset."
    WScript.Quit 1
End If

Dim mysqlPath
mysqlPath = ResolveMysqlPath(shell)
If mysqlPath = "" Then
    mysqlPath = InputBox("mysql.exe was not found in PATH or common install folders." & vbCrLf & _
        "Enter the full path to mysql.exe:", "Locate mysql.exe", "")
End If

If mysqlPath = "" Or (Not fso.FileExists(mysqlPath)) Then
    WScript.Echo "Install canceled: valid mysql.exe path not provided."
    WScript.Quit 1
End If

Dim logPath
logPath = scriptDir & "\install_local_database.log"
If fso.FileExists(logPath) Then
    fso.DeleteFile logPath, True
End If

WriteLine logPath, "==== MTM Waitlist local DB install started: " & Now & " ===="
WriteLine logPath, "Database: " & dbName
WriteLine logPath, "mysql.exe: " & mysqlPath
WriteLine logPath, ""

Dim clientConfigPath
clientConfigPath = scriptDir & "\_mysql_install_client.cnf"
CreateClientConfig clientConfigPath, username, password

Dim preflightExit
preflightExit = RunCommandCapture(shell, BuildPingCommand(mysqlPath, clientConfigPath), logPath, "Preflight: mysql connectivity check")
If preflightExit <> 0 Then
    WScript.Echo "Install canceled: unable to connect to MySQL with provided credentials."
    WriteLine logPath, "Install canceled: preflight check failed with exit " & preflightExit & "."
    SafeDeleteFile clientConfigPath
    WScript.Quit 1
End If

Dim scripts
scripts = Array( _
    "Bootstrap\create_database.sql", _
    "Tables\AllTables.sql", _
    "Functions\AllFunct.sql", _
    "StoredProcedures\AllSPs.sql", _
    "Views\AllViews.sql", _
    "Seeds\AllSeeds.sql", _
    "Bootstrap\update_table_descriptions.sql", _
    "Validation\startup_schema\validate.sql", _
    "Validation\settings_schema\validate.sql" _
)

Dim failures
failures = ""

Dim i
For i = 0 To UBound(scripts)
    Dim sqlPath
    sqlPath = scriptDir & "\" & scripts(i)

    If Not fso.FileExists(sqlPath) Then
        Dim missingMsg
        missingMsg = "MISSING SCRIPT: " & scripts(i)
        WScript.Echo missingMsg
        WriteLine logPath, missingMsg
        failures = failures & "- " & scripts(i) & " (missing file)" & vbCrLf

        If Not PromptContinue("Missing SQL file." & vbCrLf & scripts(i)) Then
            WriteLine logPath, "Install aborted by user after missing file."
            Exit For
        End If
    Else
        Dim stepMsg
        stepMsg = "Running: " & scripts(i)
        WScript.Echo stepMsg
        WriteLine logPath, stepMsg

        Dim exitCode
        exitCode = RunSql(shell, mysqlPath, clientConfigPath, sqlPath, logPath)

        If exitCode <> 0 Then
            Dim errMsg
            errMsg = "FAILED (exit " & exitCode & "): " & scripts(i)
            WScript.Echo errMsg
            WriteLine logPath, errMsg
            failures = failures & "- " & scripts(i) & " (exit " & exitCode & ")" & vbCrLf

            If Not PromptContinue("Error while running:" & vbCrLf & scripts(i) & vbCrLf & vbCrLf & _
                "Check log and continue anyway?") Then
                WriteLine logPath, "Install aborted by user after script error."
                Exit For
            End If
        Else
            WriteLine logPath, "SUCCESS: " & scripts(i)
        End If

        WriteLine logPath, ""
    End If
Next

WriteLine logPath, "==== MTM Waitlist local DB install finished: " & Now & " ===="

SafeDeleteFile clientConfigPath

If failures = "" Then
    WScript.Echo "Database install completed successfully." & vbCrLf & "Log: " & logPath
    WScript.Quit 0
Else
    WScript.Echo "Database install completed with errors." & vbCrLf & vbCrLf & failures & vbCrLf & "Log: " & logPath
    WScript.Quit 2
End If

Function ResolveMysqlPath(shellObj)
    Dim execObj
    Dim output
    Dim cmd

    cmd = "cmd /c where mysql"
    Set execObj = shellObj.Exec(cmd)
    output = Trim(execObj.StdOut.ReadAll())

    If execObj.ExitCode = 0 And output <> "" Then
        ResolveMysqlPath = Split(output, vbCrLf)(0)
        Exit Function
    End If

    Dim candidates
    candidates = Array( _
        "C:\Program Files\MySQL\MySQL Server 5.7\bin\mysql.exe", _
        "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe", _
        "C:\Program Files (x86)\MySQL\MySQL Server 5.7\bin\mysql.exe", _
        "C:\Program Files (x86)\MySQL\MySQL Server 8.0\bin\mysql.exe" _
    )

    Dim j
    For j = 0 To UBound(candidates)
        If fso.FileExists(candidates(j)) Then
            ResolveMysqlPath = candidates(j)
            Exit Function
        End If
    Next

    ResolveMysqlPath = ""
End Function

Function RunSql(shellObj, mysqlExe, clientConfig, scriptPath, logFile)
    Dim quotedMysql
    Dim quotedConfig
    Dim quotedSql
    Dim cmd

    quotedMysql = QuoteArg(mysqlExe)
    quotedConfig = QuoteArg(clientConfig)
    quotedSql = QuoteArg(scriptPath)

    cmd = "cmd /c type " & quotedSql & " | " & quotedMysql & _
        " --defaults-extra-file=" & quotedConfig & _
        " --default-character-set=utf8mb4 -h localhost -P 3306 2>&1"

    RunSql = RunCommandCapture(shellObj, cmd, logFile, "mysql output")
End Function

Function BuildPingCommand(mysqlExe, clientConfig)
    BuildPingCommand = QuoteArg(mysqlExe) & " --defaults-extra-file=" & QuoteArg(clientConfig) & _
        " --default-character-set=utf8mb4 -h localhost -P 3306 --execute=""SELECT 1;"""
End Function

Function QuoteArg(value)
    QuoteArg = """" & value & """"
End Function

Function PromptContinue(message)
    Dim choice
    choice = MsgBox(message, vbYesNo + vbQuestion, "Continue install?")
    PromptContinue = (choice = vbYes)
End Function

Sub WriteLine(filePath, text)
    Dim stream
    Set stream = fso.OpenTextFile(filePath, ForAppending, True)
    stream.WriteLine text
    stream.Close
End Sub

Sub CreateClientConfig(configPath, userName, userPassword)
    Dim stream
    Set stream = fso.OpenTextFile(configPath, 2, True)
    stream.WriteLine "[client]"
    stream.WriteLine "user=" & userName
    stream.WriteLine "password=" & userPassword
    stream.Close
End Sub

Sub SafeDeleteFile(filePath)
    On Error Resume Next
    If fso.FileExists(filePath) Then
        fso.DeleteFile filePath, True
    End If
    On Error GoTo 0
End Sub

Function RunCommandCapture(shellObj, commandText, logFile, label)
    Dim execObj
    Dim output
    Dim errorOutput

    WriteLine logFile, label & ": " & commandText

    Set execObj = shellObj.Exec(commandText)
    output = execObj.StdOut.ReadAll()
    errorOutput = execObj.StdErr.ReadAll()

    If Trim(output) <> "" Then
        WriteLine logFile, output
    End If

    If Trim(errorOutput) <> "" Then
        WriteLine logFile, errorOutput
    End If

    RunCommandCapture = execObj.ExitCode
End Function
