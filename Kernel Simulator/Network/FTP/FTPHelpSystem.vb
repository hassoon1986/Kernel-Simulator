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

Public Module FTPHelpSystem

    'This dictionary is the definitions for commands.
    Public ftpDefinitions As Dictionary(Of String, String)

    ''' <summary>
    ''' Updates the help definition so it reflects the available commands
    ''' </summary>
    Public Sub InitFTPHelp()
        ftpDefinitions = New Dictionary(Of String, String) From {{"currlocaldir (Alias: pwdl)", DoTranslation("Gets current local directory", currentLang)},
                                                                 {"currremotedir (Alias: pwdr)", DoTranslation("Gets current remote directory", currentLang)},
                                                                 {"connect", DoTranslation("Connects to an FTP server (it must start with ""ftp://"" or ""ftps://"")", currentLang)},
                                                                 {"changelocaldir (Alias: cdl)", DoTranslation("Changes local directory to download to or upload from", currentLang)},
                                                                 {"changeremotedir (Alias: cdr)", DoTranslation("Changes remote directory to download from or upload to", currentLang)},
                                                                 {"delete (Alias: del)", DoTranslation("Deletes remote file from server", currentLang)},
                                                                 {"disconnect", DoTranslation("Disconnects from server", currentLang)},
                                                                 {"download (Alias: get)", DoTranslation("Downloads remote file to local directory using binary or text", currentLang)},
                                                                 {"exit", DoTranslation("Exits FTP shell and returns to kernel", currentLang)},
                                                                 {"help", DoTranslation("Shows help screen", currentLang)},
                                                                 {"listlocal (Alias: lsl)", DoTranslation("Lists local directory", currentLang)},
                                                                 {"listremote (Alias: lsr)", DoTranslation("Lists remote directory", currentLang)},
                                                                 {"rename (Alias: ren)", DoTranslation("Renames specific file or directory", currentLang)},
                                                                 {"upload (Alias: put)", DoTranslation("Uploads local file to remote directory using binary or text", currentLang)}}
    End Sub

    ''' <summary>
    ''' Shows the list of commands.
    ''' </summary>
    ''' <param name="command">Specified command</param>
    Public Sub FTPShowHelp(Optional ByVal command As String = "")

        If command = "" Then
            If simHelp = False Then
                For Each cmd As String In ftpDefinitions.Keys
                    W("- {0}: ", False, ColTypes.HelpCmd, cmd) : W("{0}", True, ColTypes.HelpDef, ftpDefinitions(cmd))
                Next
            Else
                W(String.Join(", ", availableCommands), True, ColTypes.Neutral)
            End If
        ElseIf command = ("currlocaldir" Or "pwdl") Then
            W(DoTranslation("Usage:", currentLang) + " currlocaldir or pwdl", True, ColTypes.Neutral)
        ElseIf command = ("currremotedir" Or "pwdr") Then
            W(DoTranslation("Usage:", currentLang) + " currremotedir or pwdr", True, ColTypes.Neutral)
        ElseIf command = "connect" Then
            W(DoTranslation("Usage:", currentLang) + " connect <server>", True, ColTypes.Neutral)
        ElseIf command = ("changelocaldir" Or "cdl") Then
            W(DoTranslation("Usage:", currentLang) + " changelocaldir <directory> or cdl <directory>", True, ColTypes.Neutral)
        ElseIf command = ("changeremotedir" Or "cdr") Then
            W(DoTranslation("Usage:", currentLang) + " changeremotedir <directory> or cdr <directory>", True, ColTypes.Neutral)
        ElseIf command = ("delete" Or "del") Then
            W(DoTranslation("Usage:", currentLang) + " delete <file> or del <file>", True, ColTypes.Neutral)
        ElseIf command = "disconnect" Then
            W(DoTranslation("Usage:", currentLang) + " disconnect", True, ColTypes.Neutral)
        ElseIf command = ("download" Or "get") Then
            W(DoTranslation("Usage:", currentLang) + " download <file> or get <file>", True, ColTypes.Neutral)
        ElseIf command = "exit" Then
            W(DoTranslation("Usage:", currentLang) + " exit", True, ColTypes.Neutral)
        ElseIf command = ("listlocal" Or "lsl") Then
            W(DoTranslation("Usage:", currentLang) + " listlocal [dir] or lsl [dir]", True, ColTypes.Neutral)
        ElseIf command = ("listremote" Or "ldr") Then
            W(DoTranslation("Usage:", currentLang) + " listremote [dir] or lsr [dir]", True, ColTypes.Neutral)
        ElseIf command = ("rename" Or "ren") Then
            W(DoTranslation("Usage:", currentLang) + " rename <oldfilename> <newfilename> or ren <oldfilename> <newfilename>", True, ColTypes.Neutral)
        ElseIf command = ("upload" Or "put") Then
            W(DoTranslation("Usage:", currentLang) + " upload <file> or put <file>", True, ColTypes.Neutral)
        End If

    End Sub

End Module
