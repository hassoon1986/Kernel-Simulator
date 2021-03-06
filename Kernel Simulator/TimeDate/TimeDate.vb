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

Imports System.ComponentModel
Imports System.Globalization
Imports System.Threading

Public Module TimeDate

    'Variables
    Public KernelDateTime As New Date
    Public WithEvents TimeDateChange As New BackgroundWorker

    ''' <summary>
    ''' Updates the time and date. Also updates the time and date corner if it was enabled in kernel configuration.
    ''' </summary>
    ''' <param name="sender"></param>
    ''' <param name="e"></param>
    Sub TimeDateChange_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs) Handles TimeDateChange.DoWork

        Dim oldWid, oldTop As Integer
        Do While True
            If TimeDateChange.CancellationPending = True Then
                e.Cancel = True
                Exit Do
            Else
                KernelDateTime = Date.Now
                If CornerTD = True And Not InSaver Then
                    oldWid = Console.WindowWidth - 19
                    oldTop = Console.WindowTop
                    WriteWhere(KernelDateTime.ToShortTimeString + " " + KernelDateTime.ToShortDateString + vbNewLine, Console.WindowWidth - 19, Console.WindowTop, ColTypes.Neutral)
                End If
            End If
            Thread.Sleep(1000)
            If oldWid <> 0 Then WriteWhere("                   ", oldWid, oldTop, ColTypes.Neutral)
        Loop

    End Sub

    ''' <summary>
    ''' Updates the KernelDateTime so it reflects the current time, and runs the updater.
    ''' </summary>
    Sub InitTimeDate()

        KernelDateTime = Date.Now
        TimeDateChange.WorkerSupportsCancellation = True
        TimeDateChange.RunWorkerAsync()

    End Sub

    ''' <summary>
    ''' Shows current time, date, and timezone.
    ''' </summary>
    Public Sub ShowCurrentTimes()

        W(DoTranslation("datetime: Time is {0}", currentLang), True, ColTypes.Neutral, RenderTime)
        W(DoTranslation("datetime: Today is {0}", currentLang), True, ColTypes.Neutral, RenderDate)
        W(DoTranslation("datetime: Time Zone: {0}", currentLang) + " ({1})", True, ColTypes.Neutral, TimeZone.CurrentTimeZone.StandardName, TimeZone.CurrentTimeZone.GetUtcOffset(KernelDateTime).ToString)

    End Sub

    ''' <summary>
    ''' Renders the current time based on kernel config (long or short)
    ''' </summary>
    ''' <returns>A long or short time</returns>
    Public Function RenderTime() As String
        If LongTimeDate Then
            Return KernelDateTime.ToLongTimeString
        Else
            Return KernelDateTime.ToShortTimeString
        End If
    End Function

    ''' <summary>
    ''' Renders the time based on specified time using the kernel config (long or short)
    ''' </summary>
    ''' <param name="DT">Specified time</param>
    ''' <returns>A long or short time</returns>
    Public Function RenderTime(ByVal DT As Date) As String
        If LongTimeDate Then
            Return DT.ToLongTimeString
        Else
            Return DT.ToShortTimeString
        End If
    End Function

    ''' <summary>
    ''' Renders the current date based on kernel config (long or short)
    ''' </summary>
    ''' <returns>A long or short date</returns>
    Public Function RenderDate() As String
        If LongTimeDate Then
            Return KernelDateTime.ToLongDateString
        Else
            Return KernelDateTime.ToShortDateString
        End If
    End Function

    ''' <summary>
    ''' Renders the current date based on specified culture
    ''' </summary>
    ''' <param name="Cult">A culture.</param>
    ''' <returns>A date</returns>
    Public Function RenderDate(ByVal Cult As CultureInfo) As String
        Return KernelDateTime.ToString(Cult)
    End Function

    ''' <summary>
    ''' Renders the date based on specified date using the kernel config (long or short)
    ''' </summary>
    ''' <param name="DT">Specified date</param>
    ''' <returns>A long or short date</returns>
    Public Function RenderDate(ByVal DT As Date) As String
        If LongTimeDate Then
            Return DT.ToLongDateString
        Else
            Return DT.ToShortDateString
        End If
    End Function

    ''' <summary>
    ''' Renders the date based on specified date and culture using the kernel config (long or short)
    ''' </summary>
    ''' <param name="DT">Specified date</param>
    ''' <param name="Cult">A culture</param>
    ''' <returns>A date</returns>
    Public Function RenderDate(ByVal DT As Date, ByVal Cult As CultureInfo) As String
        Return DT.ToString(Cult)
    End Function

End Module
