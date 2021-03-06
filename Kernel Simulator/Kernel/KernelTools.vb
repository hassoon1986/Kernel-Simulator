﻿
'    Kernel Simulator  Copyright (C) 2018-2020  EoflaOE
'
'    This file is part of Kernel Simulator
'
'    Kernel Simulator is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    Kernel Simulator is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with this program.  If not, see <https://www.gnu.org/licenses/>.

Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Diagnostics.Process
Imports Newtonsoft.Json.Linq

Public Module KernelTools

    ' A dictionary for storing paths and files (used for mods, screensavers, etc.)
    Public paths As New Dictionary(Of String, String)

    ' ----------------------------------------------- Kernel errors -----------------------------------------------

    ''' <summary>
    ''' Indicates that there's something wrong with the kernel.
    ''' </summary>
    ''' <param name="ErrorType">Specifies whether the error is serious, fatal, unrecoverable, or double panic. C/S/D/F/U</param>
    ''' <param name="Reboot">Specifies whether to reboot on panic or to show the message to press any key to shut down</param>
    ''' <param name="RebootTime">Specifies seconds before reboot. 0 is instant. Negative numbers are not allowed.</param>
    ''' <param name="Description">Explanation of what happened when it errored.</param>
    ''' <param name="Exc">An exception to get stack traces, etc. Used for dump files currently.</param>
    ''' <param name="Variables">Optional. Specifies variables to get on text that will be printed.</param>
    Public Sub KernelError(ByVal ErrorType As Char, ByVal Reboot As Boolean, ByVal RebootTime As Long, ByVal Description As String, ByVal Exc As Exception, ByVal ParamArray Variables() As Object)
        Try
            'Unquiet
            If Not BootArgs Is Nothing Then
                If BootArgs.Contains("quiet") Then
                    Wdbg("I", "Removing quiet...")
                    Console.SetOut(DefConsoleOut)
                End If
            End If

            'Check error types and its capabilities
            Wdbg("I", "Error type: {0}", ErrorType)
            If ErrorType = "S" Or ErrorType = "F" Or ErrorType = "U" Or ErrorType = "D" Or ErrorType = "C" Then
                If ErrorType = "U" And RebootTime > 5 Or ErrorType = "D" And RebootTime > 5 Then
                    'If the error type is unrecoverable, or double, and the reboot time exceeds 5 seconds, then
                    'generate a second kernel error stating that there is something wrong with the reboot time.
                    Wdbg("W", "Errors that have type {0} shouldn't exceed 5 seconds. RebootTime was {1} seconds", ErrorType, RebootTime)
                    KernelError("D", True, 5, DoTranslation("DOUBLE PANIC: Reboot Time exceeds maximum allowed {0} error reboot time. You found a kernel bug.", currentLang), Nothing, CStr(ErrorType))
                    StopPanicAndGoToDoublePanic = True
                ElseIf ErrorType = "U" And Reboot = False Or ErrorType = "D" And Reboot = False Then
                    'If the error type is unrecoverable, or double, and the rebooting is false where it should
                    'not be false, then it can deal with this issue by enabling reboot.
                    Wdbg("W", "Errors that have type {0} enforced Reboot = True.", ErrorType)
                    W(DoTranslation("[{0}] panic: Reboot enabled due to error level being {0}.", currentLang), True, ColTypes.Uncontinuable, ErrorType)
                    Reboot = True
                End If
                If RebootTime > 3600 Then
                    'If the reboot time exceeds 1 hour, then it will set the time to 1 minute.
                    Wdbg("W", "RebootTime shouldn't exceed 1 hour. Was {0} seconds", RebootTime)
                    W(DoTranslation("[{0}] panic: Time to reboot: {1} seconds, exceeds 1 hour. It is set to 1 minute.", currentLang), True, ColTypes.Uncontinuable, ErrorType, CStr(RebootTime))
                    RebootTime = 60
                End If
            Else
                'If the error type is other than D/F/C/U/S, then it will generate a second error.
                Wdbg("E", "Error type {0} is not valid.", ErrorType)
                KernelError("D", True, 5, DoTranslation("DOUBLE PANIC: Error Type {0} invalid.", currentLang), Nothing, CStr(ErrorType))
                StopPanicAndGoToDoublePanic = True
            End If

            'Parse variables ({0}, {1}, ...) in the "Description" string variable
            For v As Integer = 0 To Variables.Length - 1
                Description = Description.Replace($"{{{CStr(v)}}}", Variables(v))
                Wdbg("I", "Variable {0} parsed --> {1}", CStr(v), Variables(v))
            Next

            'Fire an event
            EventManager.RaiseKernelError()

            'Make a dump file
            GeneratePanicDump(Description, ErrorType, Exc)

            'Check error capabilities
            If Description.Contains("DOUBLE PANIC: ") And ErrorType = "D" Then
                'If the description has a double panic tag and the error type is Double
                Wdbg("F", "Double panic caused by bug in kernel crash.")
                W(DoTranslation("[{0}] dpanic: {1} -- Rebooting in {2} seconds...", currentLang), True, ColTypes.Uncontinuable, ErrorType, Description, CStr(RebootTime))
                Thread.Sleep(RebootTime * 1000)
                Wdbg("F", "Rebooting")
                PowerManage("reboot")
                adminList.Clear()
                disabledList.Clear()
            ElseIf StopPanicAndGoToDoublePanic = True Then
                'Switch to Double Panic
                Exit Sub
            ElseIf ErrorType = "C" And Reboot = True Then
                'Check if error is Continuable and reboot is enabled
                Wdbg("W", "Continuable kernel errors shouldn't have Reboot = True.")
                W(DoTranslation("[{0}] panic: Reboot disabled due to error level being {0}.", currentLang) + vbNewLine +
                  DoTranslation("[{0}] panic: {1} -- Press any key to continue using the kernel.", currentLang), True, ColTypes.Continuable, ErrorType, Description)
                Console.ReadKey()
            ElseIf ErrorType = "C" And Reboot = False Then
                'Check if error is Continuable and reboot is disabled
                EventManager.RaiseContKernelError()
                W(DoTranslation("[{0}] panic: {1} -- Press any key to continue using the kernel.", currentLang), True, ColTypes.Continuable, ErrorType, Description)
                Console.ReadKey()
            ElseIf (Reboot = False And ErrorType <> "D") Or (Reboot = False And ErrorType <> "C") Then
                'If rebooting is disabled and the error type does not equal Double or Continuable
                Wdbg("W", "Reboot is False, ErrorType is not double or continuable.")
                W(DoTranslation("[{0}] panic: {1} -- Press any key to shutdown.", currentLang), True, ColTypes.Uncontinuable, ErrorType, Description)
                Console.ReadKey()
                PowerManage("shutdown")
            Else
                'Everything else.
                Wdbg("F", "Kernel panic initiated with reboot time: {0} seconds, Error Type: {1}", RebootTime, ErrorType)
                W(DoTranslation("[{0}] panic: {1} -- Rebooting in {2} seconds...", currentLang), True, ColTypes.Uncontinuable, ErrorType, Description, CStr(RebootTime))
                Thread.Sleep(RebootTime * 1000)
                PowerManage("reboot")
                adminList.Clear()
                disabledList.Clear()
            End If
        Catch ex As Exception
            If DebugMode = True Then
                W(ex.StackTrace, True, ColTypes.Uncontinuable) : WStkTrc(ex)
                KernelError("D", True, 5, DoTranslation("DOUBLE PANIC: Kernel bug: {0}", currentLang), ex, ex.Message)
            Else
                KernelError("D", True, 5, DoTranslation("DOUBLE PANIC: Kernel bug: {0}", currentLang), ex, ex.Message)
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Generates the stack trace dump file for kernel panics
    ''' </summary>
    ''' <param name="Description">Error description</param>
    ''' <param name="ErrorType">Error type</param>
    ''' <param name="Exc">Exception</param>
    Sub GeneratePanicDump(ByVal Description As String, ByVal ErrorType As Char, ByVal Exc As Exception)
        Try
            'Open a file stream for dump
            Dim Dump As New StreamWriter($"{paths("Home")}/dmp_{RenderDate.Replace("/", "-")}_{RenderTime.Replace(":", "-")}.txt")
            Wdbg("I", "Opened file stream in home directory, saved as dmp_{0}_{1}.txt", $"{RenderDate.Replace("/", "-")}_{RenderTime.Replace(":", "-")}")

            'Write info (Header)
            Dump.AutoFlush = True
            Dump.WriteLine(DoTranslation("----------------------------- Kernel panic dump -----------------------------", currentLang) + vbNewLine + vbNewLine +
                           DoTranslation(">> Panic information <<", currentLang) + vbNewLine +
                           DoTranslation("> Description: {0}", currentLang) + vbNewLine +
                           DoTranslation("> Error type: {1}", currentLang) + vbNewLine +
                           DoTranslation("> Date and Time: {2}", currentLang) + vbNewLine, Description, ErrorType, FormatDateTime(Date.Now, DateFormat.GeneralDate))

            'Write Info (Exception)
            If Not IsNothing(Exc) Then
                Dim Count As Integer = 1
                Dump.WriteLine(DoTranslation(">> Exception information <<", currentLang) + vbNewLine +
                               DoTranslation("> Exception: {0}", currentLang) + vbNewLine +
                               DoTranslation("> Description: {1}", currentLang) + vbNewLine +
                               DoTranslation("> HRESULT: {2}", currentLang) + vbNewLine +
                               DoTranslation("> Source: {3}", currentLang) + vbNewLine + vbNewLine +
                               DoTranslation("> Stack trace <", currentLang) + vbNewLine + vbNewLine +
                               Exc.StackTrace + vbNewLine + vbNewLine +
                               DoTranslation(">> Inner exception {0} information <<", currentLang), Exc.ToString.Substring(0, Exc.ToString.IndexOf(":")), Exc.Message, Exc.HResult, Exc.Source)

                'Write info (Inner exceptions)
                Dim InnerExc As Exception = Exc.InnerException
                While Not InnerExc Is Nothing
                    Count += 1
                    Dump.WriteLine(DoTranslation("> Exception: {0}", currentLang) + vbNewLine +
                                   DoTranslation("> Description: {1}", currentLang) + vbNewLine +
                                   DoTranslation("> HRESULT: {2}", currentLang) + vbNewLine +
                                   DoTranslation("> Source: {3}", currentLang) + vbNewLine + vbNewLine +
                                   DoTranslation("> Stack trace <", currentLang) + vbNewLine + vbNewLine +
                                   InnerExc.StackTrace + vbNewLine, InnerExc.ToString.Substring(0, InnerExc.ToString.IndexOf(":")), InnerExc.Message, InnerExc.HResult, InnerExc.Source)
                    InnerExc = InnerExc.InnerException
                    If Not InnerExc Is Nothing Then
                        Dump.WriteLine(DoTranslation(">> Inner exception {0} information <<", currentLang), Count)
                    Else
                        Dump.WriteLine(DoTranslation(">> Exception {0} is the root cause <<", currentLang) + vbNewLine, Count)
                    End If
                End While
            Else
                Dump.WriteLine(DoTranslation(">> No exception; might be a kernel error. <<", currentLang) + vbNewLine)
            End If

            'Write info (Frames)
            Dump.WriteLine(DoTranslation(">> Frames, files, lines, and columns <<", currentLang))
            Try
                Dim ExcTrace As New StackTrace(Exc, True)
                Dim FrameNo As Integer = 1
                For Each Frame As StackFrame In ExcTrace.GetFrames
                    If Not (Frame.GetFileName = "" And Frame.GetFileLineNumber = 0 And Frame.GetFileColumnNumber = 0) Then
                        Dump.WriteLine(DoTranslation("> Frame {0}: File: {1} | Line: {2} | Column: {3}", currentLang), FrameNo, Frame.GetFileName, Frame.GetFileLineNumber, Frame.GetFileColumnNumber)
                    End If
                    FrameNo += 1
                Next
            Catch ex As Exception
                WStkTrc(ex)
                Dump.WriteLine(DoTranslation("> There is an error when trying to get frame information. {0}: {1}", currentLang), ex.ToString.Substring(0, ex.ToString.IndexOf(":")), ex.Message.Replace(vbNewLine, " | "))
            End Try

            'Close stream
            Wdbg("I", "Closing file stream for dump...")
            Dump.Flush() : Dump.Close()
        Catch ex As Exception
            W(DoTranslation("Dump information gatherer crashed when trying to get information about {0}: {1}", currentLang), True, ColTypes.Err, Exc.ToString.Substring(0, Exc.ToString.IndexOf(":")), ex.Message)
            WStkTrc(ex)
        End Try
    End Sub

    ' ----------------------------------------------- Power management -----------------------------------------------

    ''' <summary>
    ''' Manage computer's (actually, simulated computer) power
    ''' </summary>
    ''' <param name="PowerMode">Whether it would be "shutdown", "rebootsafe", or "reboot"</param>
    Public Sub PowerManage(ByVal PowerMode As String, Optional ByVal IP As String = "0.0.0.0")
        Wdbg("I", "Power management has the argument of {0}", PowerMode)
        If PowerMode = "shutdown" Then
            EventManager.RaisePreShutdown()
            W(DoTranslation("Shutting down...", currentLang), True, ColTypes.Neutral)
            ResetEverything()
            EventManager.RaisePostShutdown()
            Environment.Exit(0)
        ElseIf PowerMode = "reboot" Then
            EventManager.RaisePreReboot()
            W(DoTranslation("Rebooting...", currentLang), True, ColTypes.Neutral)
            ResetEverything()
            EventManager.RaisePostReboot()
            Console.Clear()
            RebootRequested = True
            LogoutRequested = True
            SafeMode = False
            paths.Clear()
        ElseIf PowerMode = "rebootsafe" Then
            EventManager.RaisePreReboot()
            W(DoTranslation("Rebooting...", currentLang), True, ColTypes.Neutral)
            ResetEverything()
            EventManager.RaisePostReboot()
            Console.Clear()
            RebootRequested = True
            LogoutRequested = True
            SafeMode = True
            paths.Clear()
        ElseIf PowerMode = "remoteshutdown" Then
            SendCommand("<Request:Shutdown>(" + IP + ")", IP)
        ElseIf PowerMode = "remoterestart" Then
            SendCommand("<Request:Reboot>(" + IP + ")", IP)
        End If
    End Sub

    ' ----------------------------------------------- Init and reset -----------------------------------------------
    ''' <summary>
    ''' Reset everything for the next restart
    ''' </summary>
    Sub ResetEverything()
        'Reset every variable that is resettable
        If argsInjected = False Then
            answerargs = Nothing
        End If
        Erase BootArgs
        argsFlag = False
        StopPanicAndGoToDoublePanic = False
        strcommand = Nothing
        slotsUsedName = Nothing
        slotsUsedNum = 0
        totalSlots = 0
        modcmnds.Clear()
        moddefs.Clear()
        scripts.Clear()
        Wdbg("I", "General variables reset")

        'Reset hardware info
        HDDList.Clear()
        RAMList.Clear()
        CPUList.Clear()
        Wdbg("I", "All hardware reset")

        'Release RAM used
        DisposeAll()
        Wdbg("I", "Garbage collector finished")

        'Disconnect all hosts from remote debugger
        StartRDebugThread(False)
        Wdbg("I", "Remote debugger stopped")

        'Close settings
        configReader = New IniFile()
        Wdbg("I", "Settings closed")

        'Stop all mods
        ParseMods(False)
        Wdbg("I", "Mods stopped")

        'Disable Debugger
        If DebugMode = True Then
            Wdbg("I", "Shutting down debugger")
            DebugMode = False
            dbgWriter.Close() : dbgWriter.Dispose()
        End If

        'Disable safe mode
        SafeMode = True
    End Sub

    ''' <summary>
    ''' Initializes everything
    ''' </summary>
    Sub InitEverything()
        'Initialize help
        InitHelp()
        InitFTPHelp()
        IMAPInitHelp()

        'We need to create a file so InitAliases() won't give out an error
        If Not File.Exists(paths("Aliases")) Then
            Dim fstream As FileStream = File.Create(paths("Aliases"))
            fstream.Close()
        End If

        'Initialize aliases
        InitAliases()

        'Initialize date
        If Not TimeDateIsSet Then
            InitTimeDate()
            TimeDateIsSet = True
        End If

        'Check for multiple instances of KS
        If instanceChecked = False Then MultiInstance()

        'Open debugging stream
        If dbgWriter Is Nothing Then dbgWriter = New StreamWriter(paths("Debugging"), True) With {.AutoFlush = True}

        'Write headers for debug
        Wdbg("I", "-------------------------------------------------------------------")
        Wdbg("I", "Kernel initialized, version {0}.", KernelVersion)
        Wdbg("I", "OS: {0}", EnvironmentOSType)

        'Create config file and then read it
        InitializeConfig()

        If RebootRequested Then
            Exit Sub
        End If

        'Show welcome message. Don't remove license
        If StartScroll Then
            WriteSlowlyC(DoTranslation("---===+++> Welcome to the kernel | Version {0} <+++===---", currentLang), True, 10, ColTypes.Neutral, KernelVersion)
        Else
            W(DoTranslation("---===+++> Welcome to the kernel | Version {0} <+++===---", currentLang), True, ColTypes.Neutral, KernelVersion)
        End If
        W(vbNewLine + "    Kernel Simulator  Copyright (C) 2018-2020  EoflaOE" + vbNewLine +
                      "    This program comes with ABSOLUTELY NO WARRANTY, not even " + vbNewLine +
                      "    MERCHANTABILITY or FITNESS for particular purposes." + vbNewLine +
                      "    This is free software, and you are welcome to redistribute it" + vbNewLine +
                      "    under certain conditions; See COPYING file in source code." + vbNewLine, True, ColTypes.License)
        W("OS: " + DoTranslation("Running on {0}", currentLang), True, ColTypes.Neutral, EnvironmentOSType)
#If SPECIFIER = "DEV" Then 'WARNING: When the development nearly ends, change the compiler constant value to "REL" to suppress this message out of stable versions
        W(DoTranslation("Looks like you were running the development version of the kernel. While you can see the aspects, it is frequently updated and might introduce bugs. It is recommended that you stay on the stable version.", currentLang), True, ColTypes.Neutral)
#ElseIf SPECIFIER = "RC" Then
        W(DoTranslation("Looks like you were running the release candidate version. It is recommended that you stay on the stable version.", currentLang), True, ColTypes.Neutral)
#ElseIf SPECIFIER = "NEARING" Then
        W(DoTranslation("Looks like you were running the nearing-release version. While it's safer to use now, it is recommended that you stay on the stable version.", currentLang), True, ColTypes.Neutral)
#End If

        'Parse real command-line arguments
        For Each argu In Environment.GetCommandLineArgs
            ParseCMDArguments(argu)
        Next

        'Check arguments
        If argsOnBoot Then
            PromptArgs()
            If argsFlag Then ParseArguments()
        End If
        If argsInjected Then
            ParseArguments()
            answerargs = ""
            argsInjected = False
        End If

        'Parse current theme string
        ParseCurrentTheme()

        'Start screensaver timeout
        If Not Timeout.IsBusy Then Timeout.RunWorkerAsync()
    End Sub

    ''' <summary>
    ''' Initializes the paths
    ''' </summary>
    Sub InitPaths()
        If EnvironmentOSType.Contains("Unix") Then
            If Not paths.ContainsKey("Mods") Then paths.Add("Mods", Environ("HOME") + "/KSMods/")
            If Not paths.ContainsKey("Configuration") Then paths.Add("Configuration", Environ("HOME") + "/kernelConfig.ini")
            If Not paths.ContainsKey("Debugging") Then paths.Add("Debugging", Environ("HOME") + "/kernelDbg.log")
            If Not paths.ContainsKey("Aliases") Then paths.Add("Aliases", Environ("HOME") + "/aliases.csv")
            If Not paths.ContainsKey("Users") Then paths.Add("Users", Environ("HOME") + "/users.csv")
            If Not paths.ContainsKey("Home") Then paths.Add("Home", Environ("HOME"))
            If Not paths.ContainsKey("Temp") Then paths.Add("Temp", "/tmp")
        Else
            If Not paths.ContainsKey("Mods") Then paths.Add("Mods", Environ("USERPROFILE").Replace("\", "/") + "/KSMods/")
            If Not paths.ContainsKey("Configuration") Then paths.Add("Configuration", Environ("USERPROFILE").Replace("\", "/") + "/kernelConfig.ini")
            If Not paths.ContainsKey("Debugging") Then paths.Add("Debugging", Environ("USERPROFILE").Replace("\", "/") + "/kernelDbg.log")
            If Not paths.ContainsKey("Aliases") Then paths.Add("Aliases", Environ("USERPROFILE").Replace("\", "/") + "/aliases.csv")
            If Not paths.ContainsKey("Users") Then paths.Add("Users", Environ("USERPROFILE") + "/users.csv")
            If Not paths.ContainsKey("Home") Then paths.Add("Home", Environ("USERPROFILE").Replace("\", "/"))
            If Not paths.ContainsKey("Temp") Then paths.Add("Temp", Environ("TEMP").Replace("\", "/"))
        End If
    End Sub

    ' ----------------------------------------------- Misc -----------------------------------------------

    Sub MultiInstance()
        'Check to see if multiple Kernel Simulator processes are running.
        Static ksInst As Mutex
        Dim ksOwner As Boolean
        ksInst = New Mutex(True, "Kernel Simulator", ksOwner)
        If Not ksOwner Then
            KernelError("F", False, 0, DoTranslation("Another instance of Kernel Simulator is running. Shutting down in case of interference.", currentLang), Nothing)
        End If
        instanceChecked = True
    End Sub

    Sub CheckKernelUpdates()
        W(DoTranslation("Checking for system updates...", currentLang), True, ColTypes.Neutral)
        Try
            Dim UpdateDown As New WebClient
            UpdateDown.Headers.Add(HttpRequestHeader.UserAgent, "EoflaOE") 'Because api.github.com requires the UserAgent header to be put, else, 403 error occurs.
            Dim UpdateStr As String = UpdateDown.DownloadString("https://api.github.com/repos/EoflaOE/Kernel-Simulator/releases")
            Dim UpdateToken As JToken = JToken.Parse(UpdateStr)
            Dim UpdateVer As String = UpdateToken.First.SelectToken("tag_name")
            Dim UpdateURL As String = UpdateToken.First.SelectToken("html_url")
            Dim CurrentVer As String = "v" + KernelVersion + "-alpha" 'We usually put -alpha in releases when we indicate that it's the alpha release.
            If UpdateVer <> CurrentVer Then
                'Found a new version
                W(DoTranslation("Found new version: ", currentLang), False, ColTypes.HelpCmd)
                W(UpdateVer, True, ColTypes.HelpDef)
                W(DoTranslation("You can download it at: ", currentLang), False, ColTypes.HelpCmd)
                W(UpdateURL, True, ColTypes.HelpDef)
            End If
        Catch ex As Exception
            W(DoTranslation("Failed to check for updates: {0}", currentLang), True, ColTypes.Err, ex.Message)
            WStkTrc(ex)
        End Try
    End Sub

    Function GetCompileDate() As DateTime 'Always successful, no need to put Try Catch
        'Variables and Constants
        Const Offset As Integer = 60 : Const LTOff As Integer = 8
        Dim asmByte(2047) As Byte : Dim asmStream As Stream
        Dim codePath As Assembly = Assembly.GetExecutingAssembly

        'Get compile date
        asmStream = New FileStream(Path.GetFullPath(codePath.Location), FileMode.Open, FileAccess.Read)
        asmStream.Read(asmByte, 0, 2048)
        If Not asmStream Is Nothing Then asmStream.Close()

        'We are almost there
        Dim i64 As Integer = BitConverter.ToInt32(asmByte, Offset)
        Dim compileseconds As Integer = BitConverter.ToInt32(asmByte, i64 + LTOff)
        Dim dt As New DateTime(1970, 1, 1, 0, 0, 0)
        dt = dt.AddSeconds(compileseconds)
        dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours)

        'Now return compile date
        Return dt
    End Function
#If SPECIFIER = "DEV" Then
    Function GetCompileDate(ByVal Asm As Assembly) As DateTime 'Only exists in development version.
        Dim dt As New DateTime(1970, 1, 1, 0, 0, 0)
        Try
            'Variables and Constants
            Const Offset As Integer = 60 : Const LTOff As Integer = 8
            Dim asmByte(2047) As Byte : Dim asmStream As Stream
            Dim codePath As Assembly = Asm

            'Get compile date
            asmStream = New FileStream(Path.GetFullPath(codePath.Location), FileMode.Open, FileAccess.Read)
            asmStream.Read(asmByte, 0, 2048)
            If Not asmStream Is Nothing Then asmStream.Close()

            'We are almost there
            Dim i64 As Integer = BitConverter.ToInt32(asmByte, Offset)
            Dim compileseconds As Integer = BitConverter.ToInt32(asmByte, i64 + LTOff)
            dt = dt.AddSeconds(compileseconds)
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours)
        Catch ex As Exception
            W(DoTranslation("Error while trying to get compile date of assembly {0}: {1}", currentLang), True, ColTypes.Err, Asm.CodeBase, ex.Message)
        End Try

        'Now return compile date
        Return dt
    End Function
#End If

    Private Declare Function SetProcessWorkingSetSize Lib "kernel32.dll" (ByVal hProcess As IntPtr, ByVal dwMinimumWorkingSetSize As Int32, ByVal dwMaximumWorkingSetSize As Int32) As Int32

    ''' <summary>
    ''' Disposes all unused memory.
    ''' </summary>
    Public Sub DisposeAll()

        Try
            Wdbg("I", "Garbage collector starting... Max generators: {0}", GC.MaxGeneration.ToString)
            GC.Collect()
            GC.WaitForPendingFinalizers()
            If EnvironmentOSType.Contains("NT") Then
                SetProcessWorkingSetSize(GetCurrentProcess().Handle, -1, -1)
            End If
            EventManager.RaiseGarbageCollected()
        Catch ex As Exception
            W(DoTranslation("Error trying to free RAM: {0} - Continuing...", currentLang), True, ColTypes.Err, ex.Message)
            If DebugMode = True Then
                W(ex.StackTrace, True, ColTypes.Neutral) : Wdbg("Error freeing RAM: {0}", ex.Message) : WStkTrc(ex)
            End If
        End Try

    End Sub

End Module
