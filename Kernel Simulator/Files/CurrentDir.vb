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

Public Module CurrentDir

    'Variables
    'TODO: Change the file system to real file system
    Public currDir As String = "/"

    Public Sub setCurrDir(ByVal dir As String)

        If (AvailableDirs.Contains(dir)) Then
            currDir = "/" + dir
        ElseIf (dir = "") Then
            currDir = "/"
        Else
            Wln(DoTranslation("Cannot change directory to /{0} because that directory leads nowhere.", currentLang), "neutralText", dir)
        End If

    End Sub

End Module