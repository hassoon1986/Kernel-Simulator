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

Public Module UserManagement

    Public Sub initializeUser(ByVal uninitUser As String, Optional ByVal unpassword As String = "")

        Try
            userword.Add(uninitUser, unpassword)
            Wdbg("userword = {1}", userword, String.Join(", ", userword.ToArray))
            adminList.Add(uninitUser, False)
            disabledList.Add(uninitUser, False)
        Catch ex As Exception
            If (DebugMode = True) Then
                Wln(DoTranslation("Error trying to add username.", currentLang) + vbNewLine +
                    DoTranslation("Error {0}: {1}", currentLang) + vbNewLine + "{2}", "neutralText", Err.Number, Err.Description, ex.StackTrace)
                Wdbg(ex.StackTrace, True)
            Else
                Wln(DoTranslation("Error trying to add username.", currentLang) + vbNewLine +
                    DoTranslation("Error {0}: {1}", currentLang), "neutralText", Err.Number, Err.Description)
            End If
        End Try

    End Sub

    Public Sub adduser(ByVal newUser As String, Optional ByVal newPassword As String = "")

        'Adds users
        If (Quiet = False) Then
            Wln(DoTranslation("usrmgr: Creating username {0}...", currentLang), "neutralText", newUser)
        End If
        If (newPassword = Nothing) Then
            initializeUser(newUser)
        Else
            initializeUser(newUser, newPassword)
        End If

    End Sub

    Public Sub resetUsers()

        'Resets users and permissions
        adminList.Clear()
        disabledList.Clear()
        userword.Clear()

        'Resets outputs
        password = Nothing
        LoginFlag = False
        CruserFlag = False
        signedinusrnm = Nothing

        'Resets inputs
        answerpass = Nothing
        answeruser = Nothing
        arguser = Nothing
        argword = Nothing

    End Sub

    'This sub is an accomplice of in-shell command arguments.
    Public Sub removeUserFromDatabase(ByVal user As String)

        Try
            Dim DoneFlag As String = "No"
            If InStr(user, " ") > 0 Then
                Wln(DoTranslation("Spaces are not allowed.", currentLang), "neutralText")
            ElseIf (user = "q") Then
                DoneFlag = "Cancelled"
            ElseIf (user.IndexOfAny("[~`!@#$%^&*()-+=|{}':;.,<>/?]".ToCharArray) <> -1) Then
                Wln(DoTranslation("Special characters are not allowed.", currentLang), "neutralText")
            ElseIf (user = Nothing) Then
                Wln(DoTranslation("Blank username.", currentLang), "neutralText")
            ElseIf userword.ContainsKey(user) = False Then
                Wdbg("ASSERT(isFound({0})) = False", user)
                Wln(DoTranslation("User {0} not found.", currentLang), "neutralText", user)
            Else
                For Each usersRemove As String In userword.Keys.ToArray
                    If (usersRemove = user And user = "root") Then
                        Wln(DoTranslation("User {0} isn't allowed to be removed.", currentLang), "neutralText", user)
                    ElseIf (user = usersRemove And usersRemove = signedinusrnm) Then
                        Wln(DoTranslation("User {0} is already logged in. Log-out and log-in as another admin.", currentLang), "neutralText", user)
                        Wdbg("ASSERT({0}.isLoggedIn(ASSERT({0} = {1}) = True)) = True", user, signedinusrnm)
                    ElseIf (usersRemove = user And user <> "root") Then
                        adminList.Remove(user)
                        disabledList.Remove(user)
                        Wdbg("userword.ToBeRemoved = {0}", String.Join(", ", userword(user).ToArray))
                        userword.Remove(user)
                        Wln(DoTranslation("User {0} removed.", currentLang), "neutralText", user)
                        DoneFlag = "Yes"
                    End If
                Next
            End If
        Catch ex As Exception
            If (DebugMode = True) Then
                Wln(DoTranslation("Error trying to remove username.", currentLang) + vbNewLine +
                    DoTranslation("Error {0}: {1}", currentLang) + vbNewLine + "{2}", "neutralText", Err.Number, Err.Description, ex.StackTrace)
                Wdbg(ex.StackTrace, True)
            Else
                Wln(DoTranslation("Error trying to remove username.", currentLang) + vbNewLine +
                    DoTranslation("Error {0}: {1}", currentLang), "neutralText", Err.Number, Err.Description)
            End If
        End Try
        user = Nothing

    End Sub

End Module
