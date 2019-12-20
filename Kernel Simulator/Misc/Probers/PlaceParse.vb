﻿
'    Kernel Simulator  Copyright (C) 2018-2019  EoflaOE
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

Public Module PlaceParse

    'Placeholders (strings)
    Private ReadOnly userplace As String = "<user>"
    Private ReadOnly sdateplace As String = "<shortdate>"
    Private ReadOnly ldateplace As String = "<longdate>"
    Private ReadOnly stimeplace As String = "<shorttime>"
    Private ReadOnly ltimeplace As String = "<longtime>"
    Private ReadOnly dateplace As String = "<date>"
    Private ReadOnly timeplace As String = "<time>"
    Private ReadOnly tzplace As String = "<timezone>"
    Private ReadOnly stzplace As String = "<summertimezone>"
    Private ReadOnly sysplace As String = "<system>"

    'Probing code
    Public Function ProbePlaces(ByVal text As String) As String

        EventManager.RaisePlaceholderParsing()
        Try
            If text.Contains(userplace) Then text = text.Replace(userplace, signedinusrnm)
            If text.Contains(sdateplace) Then text = text.Replace(sdateplace, KernelDateTime.ToShortDateString)
            If text.Contains(ldateplace) Then text = text.Replace(ldateplace, KernelDateTime.ToLongDateString)
            If text.Contains(stimeplace) Then text = text.Replace(stimeplace, KernelDateTime.ToShortTimeString)
            If text.Contains(ltimeplace) Then text = text.Replace(ltimeplace, KernelDateTime.ToShortDateString)
            If text.Contains(dateplace) Then text = text.Replace(dateplace, RenderDate)
            If text.Contains(timeplace) Then text = text.Replace(timeplace, RenderTime)
            If text.Contains(tzplace) Then text = text.Replace(tzplace, TimeZone.CurrentTimeZone.StandardName)
            If text.Contains(stzplace) Then text = text.Replace(stzplace, TimeZone.CurrentTimeZone.DaylightName)
            If text.Contains(sysplace) Then text = text.Replace(sysplace, EnvironmentOSType)
            EventManager.RaisePlaceholderParsed()
        Catch nre As NullReferenceException
            Dim STrace As New StackTrace(True)
            Dim Source As String = Path.GetFileName(STrace.GetFrame(1).GetFileName)
            Dim LineNum As String = STrace.GetFrame(1).GetFileLineNumber
            WStkTrc(nre)
            If DebugMode = True Then
                W(DoTranslation("There is a null reference exception on {0}:{1} - Stack trace:", currentLang) + vbNewLine + nre.StackTrace, True, ColTypes.Neutral, Source, LineNum)
            Else
                W(DoTranslation("There is a null reference exception on {0}:{1}", currentLang), True, ColTypes.Neutral, Source, LineNum)
            End If
        Catch ex As Exception
            WStkTrc(ex)
            If DebugMode = True Then
                W(DoTranslation("Error trying to parse placeholders. {0} - Stack trace:", currentLang) + vbNewLine + ex.StackTrace, True, ColTypes.Neutral, ex.Message)
            Else
                W(DoTranslation("Error trying to parse placeholders. {0}", currentLang), True, ColTypes.Neutral, ex.Message)
            End If
        End Try
        Return text

    End Function

End Module
