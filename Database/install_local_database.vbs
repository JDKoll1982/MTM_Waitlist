Option Explicit

' ============================================================================
' MTM Waitlist - local / shared database installer
' ============================================================================
' Deploys BOTH databases:
'   1. mtm_waitlist  - the application's own store (DROP + RECREATE)
'   2. mtm_mock      - the Infor Visual mirror cache (created/updated in place;
'                      existing cache rows are deliberately kept, because the
'                      cache can be rebuilt by the service but is expensive to
'                      refill, and the artifacts are re-runnable).
'
' Target host selection:
'   The script first checks whether the shared database server
'   (HOST_REMOTE below) is reachable. If it answers, everything is pushed
'   there; otherwise everything is pushed to this machine (localhost).
'   If the shared server answers but MySQL refuses the credentials there, the
'   script falls back to this machine rather than stopping.
'
'   Selection order is:  ping the shared server -> MySQL preflight on it ->
'   on failure, MySQL preflight on localhost -> abort only if neither works.
'
' Log: install_local_database.log next to this script (rewritten each run).
' Exit codes: 0 = success, 2 = completed with errors, 1 = canceled/failed.
' ============================================================================

Const ForAppending = 8

Dim fso
Dim shell
Set fso = CreateObject("Scripting.FileSystemObject")
Set shell = CreateObject("WScript.Shell")

Dim scriptDir
scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)

' ----------------------------------------------------------------------------
' Configuration
' ----------------------------------------------------------------------------
Dim dbName
dbName = "mtm_waitlist"

Dim mockDbName
mockDbName = "mtm_mock"

' Preferred database server. If it is reachable, the install targets it;
' otherwise the install targets this machine.
Dim remoteHost
remoteHost = "172.16.1.104"

Dim localHost
localHost = "localhost"

Dim dbPort
dbPort = 3306

' How long a single ping attempt waits for a reply, in milliseconds.
Dim pingTimeoutMs
pingTimeoutMs = 1000

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

' ----------------------------------------------------------------------------
' Pick the database host BEFORE asking for confirmation, so the prompt can name
' the server the install will actually touch.
' ----------------------------------------------------------------------------
Dim hostSelectionNote
Dim targetHost
targetHost = ResolveTargetHost(hostSelectionNote)

WScript.Echo hostSelectionNote

Dim targetDescription
targetDescription = DescribeHost(targetHost)

Dim proceed
proceed = MsgBox("This will DROP and RECREATE database '" & dbName & "' on " & targetHost & "." & vbCrLf & vbCrLf & _
    "It will also create/update the '" & mockDbName & "' cache database in place." & vbCrLf & _
    "Existing cached rows in '" & mockDbName & "' are kept." & vbCrLf & vbCrLf & _
    "Target: " & targetDescription & vbCrLf & vbCrLf & _
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

WriteLine logPath, "==== MTM Waitlist database install started: " & Now & " ===="
WriteLine logPath, "Application database: " & dbName & " (dropped and recreated)"
WriteLine logPath, "Cache database:       " & mockDbName & " (created/updated in place)"
WriteLine logPath, "Host selection:       " & hostSelectionNote
WriteLine logPath, "Target:               " & targetHost & ":" & dbPort & " (" & targetDescription & ")"
WriteLine logPath, "mysql.exe:            " & mysqlPath
WriteLine logPath, ""

Dim clientConfigPath
clientConfigPath = scriptDir & "\_mysql_install_client.cnf"
CreateClientConfig clientConfigPath, username, password

' ----------------------------------------------------------------------------
' Connectivity preflight, with a fall back to this machine.
' ----------------------------------------------------------------------------
Dim preflightExit
preflightExit = RunCommandCapture(shell, BuildPingCommand(mysqlPath, clientConfigPath, targetHost, dbPort), logPath, "Preflight: mysql connectivity check (" & targetHost & ")")

If preflightExit <> 0 And IsRemoteHost(targetHost) Then
    Dim fallbackMsg
    fallbackMsg = "Could not connect to " & remoteHost & " with the provided credentials. Falling back to this machine (" & localHost & ")."
    WScript.Echo fallbackMsg
    WriteLine logPath, fallbackMsg

    targetHost = localHost
    targetDescription = DescribeHost(targetHost)
    WriteLine logPath, "Target changed to: " & targetHost & " (" & targetDescription & ")"

    preflightExit = RunCommandCapture(shell, BuildPingCommand(mysqlPath, clientConfigPath, targetHost, dbPort), logPath, "Preflight: mysql connectivity check (" & targetHost & ")")
End If

If preflightExit <> 0 Then
    WScript.Echo "Install canceled: unable to connect to MySQL with provided credentials." & vbCrLf & _
        "Host tried: " & targetHost
    WriteLine logPath, "Install canceled: preflight check failed with exit " & preflightExit & "."
    SafeDeleteFile clientConfigPath
    WScript.Quit 1
End If

' ----------------------------------------------------------------------------
' Deploy. Application database first, then the cache database.
' ----------------------------------------------------------------------------
Dim appScripts
appScripts = Array( _
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

' Dependency order for the cache database (see the header of
' Database/Mock/Bootstrap/create_database.sql): database, tables, procedures,
' the mandatory table-description maintenance file, then the baseline seed.
' The mock master lists live at the Mock root (Database/Mock/AllTables.sql,
' AllSPs.sql, AllSeeds.sql), not inside the Tables/StoredProcedures/Seeds folders.
Dim mockScripts
mockScripts = Array( _
    "Mock\Bootstrap\create_database.sql", _
    "Mock\AllTables.sql", _
    "Mock\AllSPs.sql", _
    "Mock\Bootstrap\update_table_descriptions.sql", _
    "Mock\AllSeeds.sql" _
)

Dim failures
failures = ""

failures = failures & RunScriptPhase("APPLICATION DATABASE (" & dbName & ")", appScripts, mysqlPath, clientConfigPath, logPath, targetHost, dbPort)
failures = failures & RunScriptPhase("CACHE DATABASE (" & mockDbName & ")", mockScripts, mysqlPath, clientConfigPath, logPath, targetHost, dbPort)

WriteLine logPath, "==== MTM Waitlist database install finished: " & Now & " ===="

SafeDeleteFile clientConfigPath

If failures = "" Then
    WScript.Echo "Database install completed successfully (" & targetDescription & ")." & vbCrLf & _
        "  " & dbName & ": dropped and recreated" & vbCrLf & _
        "  " & mockDbName & ": created/updated in place" & vbCrLf & vbCrLf & _
        "Log: " & logPath
    WScript.Quit 0
Else
    WScript.Echo "Database install completed with errors." & vbCrLf & vbCrLf & failures & vbCrLf & "Log: " & logPath
    WScript.Quit 2
End If

' ----------------------------------------------------------------------------
' Host selection helpers
' ----------------------------------------------------------------------------

' Chooses the database host: the shared server when it answers, else this machine.
Function ResolveTargetHost(ByRef note)
    If IsHostReachable(remoteHost, pingTimeoutMs) Then
        note = "Shared server " & remoteHost & " answered; installing there."
        ResolveTargetHost = remoteHost
    Else
        note = "Shared server " & remoteHost & " did not answer; installing on this machine (" & localHost & ")."
        ResolveTargetHost = localHost
    End If
End Function

Function IsRemoteHost(hostName)
    IsRemoteHost = (LCase(Trim(hostName)) <> LCase(localHost))
End Function

Function DescribeHost(hostName)
    If IsRemoteHost(hostName) Then
        DescribeHost = "shared server"
    Else
        DescribeHost = "this machine"
    End If
End Function

' A single ping attempt. Kept deliberately short so an offline share cannot stall
' the installer; the MySQL preflight that follows is the real test.
Function IsHostReachable(hostName, timeoutMs)
    Dim exitCode
    exitCode = shell.Run("cmd /c ping -n 1 -w " & CStr(timeoutMs) & " " & hostName & " >nul 2>&1", 0, True)
    IsHostReachable = (exitCode = 0)
End Function

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
        "C:\Program Files\MySQL\MySQL Server 9.6\bin\mysql.exe", _
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

' ----------------------------------------------------------------------------
' Deployment helpers
' ----------------------------------------------------------------------------

' Runs one phase's scripts in order. Returns a failure summary (empty when clean).
Function RunScriptPhase(phaseLabel, scripts, mysqlExe, clientConfig, logFile, hostName, portNumber)
    Dim summary
    Dim index

    summary = ""
    WriteLine logFile, "---- " & phaseLabel & " ----"
    WriteLine logFile, ""

    For index = 0 To UBound(scripts)
        Dim sqlPath
        sqlPath = scriptDir & "\" & scripts(index)

        If Not fso.FileExists(sqlPath) Then
            Dim missingMsg
            missingMsg = "MISSING SCRIPT: " & scripts(index)
            WScript.Echo missingMsg
            WriteLine logFile, missingMsg
            summary = summary & "- " & scripts(index) & " (missing file)" & vbCrLf

            If Not PromptContinue(phaseLabel & vbCrLf & vbCrLf & "Missing SQL file." & vbCrLf & scripts(index)) Then
                WriteLine logFile, "Install aborted by user after missing file."
                Exit For
            End If
        Else
            Dim stepMsg
            stepMsg = "Running: " & scripts(index)
            WScript.Echo stepMsg
            WriteLine logFile, stepMsg

            Dim exitCode
            exitCode = RunSql(shell, mysqlExe, clientConfig, sqlPath, logFile, hostName, portNumber)

            If exitCode <> 0 Then
                Dim errMsg
                errMsg = "FAILED (exit " & exitCode & "): " & scripts(index)
                WScript.Echo errMsg
                WriteLine logFile, errMsg
                summary = summary & "- " & scripts(index) & " (exit " & exitCode & ")" & vbCrLf

                If Not PromptContinue(phaseLabel & vbCrLf & vbCrLf & "Error while running:" & vbCrLf & scripts(index) & vbCrLf & vbCrLf & _
                    "Check log and continue anyway?") Then
                    WriteLine logFile, "Install aborted by user after script error."
                    Exit For
                End If
            Else
                WriteLine logFile, "SUCCESS: " & scripts(index)
            End If

            WriteLine logFile, ""
        End If
    Next

    RunScriptPhase = summary
End Function

Function RunSql(shellObj, mysqlExe, clientConfig, scriptPath, logFile, hostName, portNumber)
    Dim quotedMysql
    Dim quotedConfig
    Dim quotedSql
    Dim cmd

    quotedMysql = QuoteArg(mysqlExe)
    quotedConfig = QuoteArg(clientConfig)
    quotedSql = QuoteArg(scriptPath)

    cmd = "cmd /c type " & quotedSql & " | " & quotedMysql & _
        " --defaults-extra-file=" & quotedConfig & _
        " --default-character-set=utf8mb4 -h " & hostName & " -P " & CStr(portNumber) & " 2>&1"

    RunSql = RunCommandCapture(shellObj, cmd, logFile, "mysql output")
End Function

Function BuildPingCommand(mysqlExe, clientConfig, hostName, portNumber)
    BuildPingCommand = QuoteArg(mysqlExe) & " --defaults-extra-file=" & QuoteArg(clientConfig) & _
        " --default-character-set=utf8mb4 -h " & hostName & " -P " & CStr(portNumber) & " --execute=""SELECT 1;"""
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
