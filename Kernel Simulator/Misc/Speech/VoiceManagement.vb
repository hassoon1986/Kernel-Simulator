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

Imports LibVLCSharp.Shared

Public Module VoiceManagement

    ''' <summary>
    ''' Converts specified text to speech, and plays it back
    ''' </summary>
    ''' <param name="Text">Any text</param>
    Public Sub Speak(ByVal Text As String)
        'Download required file
        Dim SpeakReq As New WebClient
        SpeakReq.Headers.Add("Content-Type", "audio/mpeg")
        SpeakReq.Headers.Add("User-Agent", "KS on (" + EnvironmentOSType + ")")
        Wdbg("I", "Headers required: {0}", SpeakReq.Headers.Count)
        SpeakReq.DownloadFile("http://translate.google.com/translate_tts?tl=en&q=" + Text + "&client=gtx", paths("Temp") + "/tts.mpeg")

        'Use VLC for playback. Linux: Requires installing packages as specified in https://github.com/videolan/libvlcsharp/blob/3.x/docs/linux-setup.md
        'Linux: Used process `cvlc` because of instabilities (not being able to play mpeg files) as a workaround. You still have to follow installation steps in the link above
        If EnvironmentOSType.Contains("Unix") Then
            Dim VLCProc As New Process()
            Dim VLCProcS As New ProcessStartInfo("/usr/bin/cvlc", "--play-and-exit -vvvv " + paths("Temp") + "/tts.mpeg")
            AddHandler VLCProc.OutputDataReceived, Sub(sender, e) W(e.Data, True, ColTypes.Neutral)
            VLCProcS.UseShellExecute = False
            VLCProcS.RedirectStandardOutput = True
            VLCProc.StartInfo = VLCProcS
            VLCProc.Start()
            VLCProc.WaitForExit()
        Else
            Core.Initialize()
            Dim MLib As New LibVLC
            Dim MP As New MediaPlayer(MLib)
            AddHandler MLib.Log, Sub(sender, e) W($"{e.Level}@{e.Module}: {e.Message}", True, ColTypes.Neutral)
            Dim MFile As New Media(MLib, paths("Temp") + "/tts.mpeg")
            MP.Media = MFile
            MP.Play()
            While MP.State = VLCState.Playing
            End While
            MFile.Dispose()
            MP.Dispose()
        End If

        'Dispose and close objects
        Wdbg("I", "Stopped.")

        'Remove the file
        IO.File.Delete(paths("Temp") + "/tts.mpeg")
    End Sub

End Module
