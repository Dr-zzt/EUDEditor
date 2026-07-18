Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports YamlDotNet.Core
Imports YamlDotNet.Core.Events
Imports YamlDotNet.RepresentationModel

' Converts between the legacy e2p/e2s save text and the YAML project format (formatVersion 2).
' Pure text conversion: no dependency on editor state, so both directions can be verified
' by round-tripping a legacy file through YAML and comparing byte-for-byte.
Public Module E2PYaml

    Private ReadOnly SEP As String = ChrW(&HB90) ' legacy value separator used by Trigger.vb

    Private ReadOnly TypeNames As String() = {
        "main", "if", "ifElse", "condClause", "actionClause", "then", "notThen",
        "condition", "action", "while", "for", "switchOld", "whileCond", "whileThen",
        "forThen", "functions", "functionDef", "params", "codeBlock", "functionCall",
        "wait", "folder", "folderBody", "rawText", "rawTrigger", "rawTriggers",
        "triggerCond", "triggerAct", "switchExpr", "switchCase"}

    ' ElementType groups from Trigger.vb ToSaveFile: which types carry a value line
    Private ReadOnly MultiValueTypes As Integer() = {7, 8, 10, 16, 19, 21, 29}
    Private ReadOnly SingleValueTypes As Integer() = {3, 12, 20, 23, 24, 26, 28}

    Private ReadOnly SectionToKey As New Dictionary(Of String, String) From {
        {"ProjectSET", "project"}, {"DatEditSET", "datEdit"}, {"FireGraftSET", "fireGraft"},
        {"BtnSET", "buttons"}, {"ReqSET", "requirements"}, {"BinEditSET", "binEdit"},
        {"SoundPlayerSET", "soundPlayer"}, {"GRPEditorSET", "grpEditor"}, {"TileSET", "tileset"},
        {"SCDBSet", "scdb"}, {"FileManagerSET", "fileManager"}, {"TriggerEditorSET", "triggers"},
        {"PluginSET", "plugins"}}

    Private ReadOnly BtnFields As String() = {"slot", "icon", "cond", "act", "condValue", "actValue", "enableStr", "disableStr"}

    ' Requirement tables in require.dat order (see ProgramData: units, upgrades,
    ' tech research, tech use, orders) and the dat index their entry ids refer to.
    Private ReadOnly ReqTableNames As String() = {"units", "upgrades", "techResearch", "techUse", "orders"}
    Private ReadOnly ReqTableDat As Integer() = {0, 5, 6, 6, 7}

    ' Original .dat value lookup: (datIndex, fieldIndex, rawIndex) -> base value.
    ' The editor sets this after DataLoad so datEdit entries can be stored as
    ' absolute values ("value") instead of offsets ("delta"). When Nothing
    ' (standalone use) the converter falls back to writing deltas.
    Public BaseValue As Func(Of Integer, Integer, Integer, Long) = Nothing

    Public Function IsLegacyFormat(text As String) As Boolean
        Return text.TrimStart().StartsWith("S_ProjectSET")
    End Function

    ' ------------------------------------------------------------ scalar helpers

    Private Function PlainScalar(value As String) As YamlScalarNode
        Return New YamlScalarNode(value) With {.Style = ScalarStyle.Plain}
    End Function

    ' Legacy value -> YAML scalar. Booleans become real YAML booleans; everything else
    ' keeps its exact text, quoted whenever plain style could be misparsed.
    Private Function ValueScalar(raw As String) As YamlScalarNode
        If raw = "True" Then Return PlainScalar("true")
        If raw = "False" Then Return PlainScalar("false")
        If Regex.IsMatch(raw, "^-?(0|[1-9][0-9]*)$") Then Return PlainScalar(raw)
        If raw.Contains(vbLf) Then
            Dim canLiteral As Boolean = Not Regex.IsMatch(raw, "[ \t]\n") AndAlso
                Not raw.EndsWith(" ") AndAlso Not raw.EndsWith(vbTab) AndAlso Not raw.Contains(vbCr)
            Return New YamlScalarNode(raw) With {.Style = If(canLiteral, ScalarStyle.Literal, ScalarStyle.DoubleQuoted)}
        End If
        If raw = "" OrElse Regex.IsMatch(raw, "^(?i:true|false|yes|no|on|off|null|~)$") Then
            Return New YamlScalarNode(raw) With {.Style = ScalarStyle.SingleQuoted}
        End If
        Return New YamlScalarNode(raw) ' emitter decides quoting when needed
    End Function

    ' YAML scalar -> legacy value text
    Private Function ScalarIn(node As YamlNode) As String
        Dim s As YamlScalarNode = CType(node, YamlScalarNode)
        If s.Style = ScalarStyle.Plain OrElse s.Style = ScalarStyle.Any Then
            If s.Value = "true" Then Return "True"
            If s.Value = "false" Then Return "False"
        End If
        Return s.Value
    End Function

    ' ------------------------------------------------------------ section split

    Private Class Section
        Public Name As String
        Public Lines As New List(Of String)
    End Class

    Private Function SplitSections(text As String) As List(Of Section)
        Dim result As New List(Of Section)
        Dim cur As Section = Nothing
        For Each ln As String In text.Split(New String() {vbCrLf}, StringSplitOptions.None)
            If cur Is Nothing AndAlso ln.StartsWith("S_") Then
                cur = New Section With {.Name = ln.Substring(2)}
            ElseIf cur IsNot Nothing AndAlso ln = "E_" & cur.Name Then
                result.Add(cur)
                cur = Nothing
            ElseIf cur IsNot Nothing Then
                cur.Lines.Add(ln)
            End If
        Next
        Return result
    End Function

    ' ------------------------------------------------------------ legacy -> YAML

    Public Function LegacyToYaml(legacyText As String) As String
        Dim root As New YamlMappingNode()
        root.Add(PlainScalar("formatVersion"), PlainScalar("2"))

        Dim sections As List(Of Section) = SplitSections(legacyText)
        ' DatEditStyle project setting: "delta" keeps raw offsets, the default
        ' ("value") stores absolute values whenever the base data is available.
        Dim deltaStyle As Boolean = sections.Any(Function(s) s.Name = "ProjectSET" AndAlso
            s.Lines.Contains("DatEditStyle : delta"))

        For Each sec As Section In sections
            Dim key As String = If(SectionToKey.ContainsKey(sec.Name), SectionToKey(sec.Name), sec.Name)
            Select Case sec.Name
                Case "DatEditSET"
                    root.Add(PlainScalar(key), BuildDatEdit(sec.Lines, deltaStyle))
                Case "BtnSET"
                    root.Add(PlainScalar(key), BuildButtons(sec.Lines))
                Case "ReqSET"
                    root.Add(PlainScalar(key), BuildRequirements(sec.Lines))
                Case "TriggerEditorSET"
                    root.Add(PlainScalar(key), BuildTriggers(sec.Lines))
                Case "PluginSET"
                    root.Add(PlainScalar(key), BuildKvMap(SquashPluginBlobs(sec.Lines)))
                Case Else
                    root.Add(PlainScalar(key), BuildKvMap(sec.Lines))
            End Select
        Next

        Dim writer As New StringWriter()
        Dim stream As New YamlStream(New YamlDocument(root))
        stream.Save(New Emitter(writer, EmitterSettings.Default.WithBestWidth(4096)), False)
        Return InjectComments(writer.ToString())
    End Function

    ' ------------------------------------------------------------ name comments

    Private Function EntryName(datIndex As Integer, id As Integer) As String
        If datIndex >= 0 AndAlso datIndex < E2PDatDefs.DatEntryNames.Length Then
            Dim names As String() = E2PDatDefs.DatEntryNames(datIndex)
            If id >= 0 AndAlso id < names.Length Then Return names(id)
        End If
        Return ""
    End Function

    ' Appends "# <entry name>" comments to lines that reference dat entries.
    ' Comments are purely informational: the YAML parser drops them on load.
    Private Function InjectComments(yamlText As String) As String
        Dim lines As String() = yamlText.Split(New String() {vbCrLf}, StringSplitOptions.None)
        Dim section As String = ""
        For n As Integer = 0 To lines.Length - 1
            Dim ln As String = lines(n)
            Dim mSection As Match = Regex.Match(ln, "^([A-Za-z_]\w*):")
            If mSection.Success Then
                section = mSection.Groups(1).Value
                Continue For
            End If

            Dim name As String = ""
            Select Case section
                Case "datEdit"
                    Dim m As Match = Regex.Match(ln, "^- \{dat: (\w+), .*, id: (\d+), (?:delta|value): [^,}]+\}$")
                    If m.Success Then
                        name = EntryName(Array.IndexOf(E2PDatDefs.DatNames, m.Groups(1).Value),
                                         Integer.Parse(m.Groups(2).Value))
                    End If
                Case "buttons"
                    Dim m As Match = Regex.Match(ln, "^  (\d+):\s*$")
                    If m.Success Then
                        Dim id As Integer = Integer.Parse(m.Groups(1).Value)
                        If id >= 0 AndAlso id < E2PDatDefs.UnitBtnNames.Length Then
                            name = E2PDatDefs.UnitBtnNames(id)
                            ln = ln.TrimEnd()
                        End If
                    End If
                Case "fireGraft"
                    Dim m As Match = Regex.Match(ln, "^  FireGraft(\d+): ")
                    If m.Success Then name = EntryName(0, Integer.Parse(m.Groups(1).Value))
                Case "fileManager"
                    Dim m As Match = Regex.Match(ln, "^  (?:wireframData|grpwireData|tranwireData)(\d+): ")
                    If m.Success Then name = EntryName(0, Integer.Parse(m.Groups(1).Value))
                Case "requirements"
                    Dim m As Match = Regex.Match(ln, "^- \{table: (\w+), id: (\d+), .*code: \[([^\]]*)\]\}$")
                    If m.Success Then
                        Dim tableIdx As Integer = Array.IndexOf(ReqTableNames, m.Groups(1).Value)
                        If tableIdx >= 0 Then
                            Dim entName As String = EntryName(ReqTableDat(tableIdx), Integer.Parse(m.Groups(2).Value))
                            Dim interp As String = ReqCodeComment(m.Groups(3).Value)
                            name = If(entName = "", interp, entName & If(interp = "", "", ": " & interp))
                        End If
                    End If
            End Select

            If name <> "" Then lines(n) = ln & " # " & name
        Next
        Return String.Join(vbCrLf, lines)
    End Function

    ' Human-readable rendering of a requirement opcode program, mirroring how
    ' FireGraftForm.ReadReqData displays it: 0xFFnn opcodes take their name from
    ' reqopcode.txt, opcodes 2/3/4 consume the next value as a unit (37: tech),
    ' and bare values mean "Must have..." that unit. Informational comment only.
    Private Function ReqCodeComment(codesText As String) As String
        If codesText.Trim() = "" Then Return ""
        Dim parts As New List(Of String)
        Try
            Dim vals As Integer() = codesText.Split(","c).Select(Function(s) Integer.Parse(s.Trim())).ToArray()
            Dim n As Integer = 0
            While n < vals.Length
                Dim c As Integer = vals(n)
                If c > &HFF Then
                    Dim op As Integer = If(c = &HFFFF, 0, c - &HFF00)
                    Dim opName As String = If(op >= 0 AndAlso op < E2PDatDefs.ReqOpcodeNames.Length,
                                              E2PDatDefs.ReqOpcodeNames(op).Trim(), "op" & op)
                    If (op = 2 OrElse op = 3 OrElse op = 4 OrElse op = 37) AndAlso n + 1 < vals.Length Then
                        n += 1
                        opName = opName & " " & EntryName(If(op = 37, 6, 0), vals(n))
                    End If
                    parts.Add(opName)
                Else
                    parts.Add(E2PDatDefs.ReqOpcodeNames(3).Trim() & " " & EntryName(0, c))
                End If
                n += 1
            End While
        Catch
            Return ""
        End Try
        Dim result As String = String.Join("; ", parts)
        If result.Length > 160 Then
            Dim keep As New List(Of String)
            Dim total As Integer = 0
            For Each p As String In parts
                If total + p.Length > 150 Then Exit For
                keep.Add(p)
                total += p.Length + 2
            Next
            result = String.Join("; ", keep) & "; …"
        End If
        Return result
    End Function

    ' extraedssetting / extramainsettings hold multi-line euddraft plugin text (the legacy
    ' parser special-cases them, see parsingModule.FindSetting). Re-join those blobs into a
    ' single logical line (with vbLf breaks) so BuildKvMap stores them as one value.
    Private Function SquashPluginBlobs(lines As List(Of String)) As List(Of String)
        Dim result As New List(Of String)
        Dim i As Integer = 0
        While i < lines.Count
            Dim ln As String = lines(i)
            If ln.StartsWith("extraedssetting : ") Then
                Dim blob As New List(Of String) From {ln}
                i += 1
                While i < lines.Count AndAlso Not lines(i).StartsWith("extramainsettings")
                    blob.Add(lines(i))
                    i += 1
                End While
                result.Add(String.Join(vbLf, blob))
            ElseIf ln.StartsWith("extramainsettings : ") Then
                Dim blob As New List(Of String) From {ln}
                i += 1
                While i < lines.Count
                    blob.Add(lines(i))
                    i += 1
                End While
                result.Add(String.Join(vbLf, blob))
            Else
                result.Add(ln)
                i += 1
            End If
        End While
        Return result
    End Function

    Private Function BuildKvMap(lines As List(Of String)) As YamlMappingNode
        Dim map As New YamlMappingNode()
        Dim rawLines As New List(Of String)
        For Each ln As String In lines
            If ln.Contains(" : ") Then
                Dim pos As Integer = ln.IndexOf(" : ")
                Dim key As String = ln.Substring(0, pos)
                Dim value As String = ln.Substring(pos + 3)
                If key = "nqccommands" AndAlso value.StartsWith("\") Then
                    ' "\"-separated NQC command records (see PluginForm): one list item each
                    Dim seq As New YamlSequenceNode()
                    For Each item As String In value.Substring(1).Split("\"c)
                        seq.Add(ValueScalar(item))
                    Next
                    map.Add(PlainScalar(key), seq)
                Else
                    map.Add(New YamlScalarNode(key), ValueScalar(value))
                End If
            ElseIf ln <> "" Then
                rawLines.Add(ln)
            End If
        Next
        If rawLines.Count > 0 Then
            Dim seq As New YamlSequenceNode()
            For Each ln As String In rawLines
                seq.Add(ValueScalar(ln))
            Next
            map.Add(PlainScalar("_lines"), seq)
        End If
        Return map
    End Function

    ' DatEdit entries are stored semantically: which table, which field (by name from
    ' Data\*.def), which entry id, and the resulting value ("value", absolute) or the
    ' offset applied to the original value ("delta", used when DatEditStyle is delta
    ' or the base data is unavailable).
    ' Unknown tables (e.g. externally loaded .dat files) fall back to the raw tuple.
    Private Function BuildDatEdit(lines As List(Of String), deltaStyle As Boolean) As YamlSequenceNode
        Dim seq As New YamlSequenceNode()
        For Each ln As String In lines
            If ln.Trim() = "" Then Continue For
            Dim parts As String() = ln.Trim().Split(","c)
            Dim i As Integer = Integer.Parse(parts(0))
            Dim j As Integer = Integer.Parse(parts(1))
            Dim k As Integer = Integer.Parse(parts(2))
            If i >= 0 AndAlso i < E2PDatDefs.DatNames.Length AndAlso j >= 0 AndAlso j < E2PDatDefs.DatFieldNames(i).Length Then
                Dim entry As New YamlMappingNode() With {.Style = MappingStyle.Flow}
                entry.Add(PlainScalar("dat"), PlainScalar(E2PDatDefs.DatNames(i)))
                entry.Add(PlainScalar("field"), ValueScalar(E2PDatDefs.DatFieldNames(i)(j)))
                entry.Add(PlainScalar("id"), PlainScalar(CStr(k + E2PDatDefs.DatFieldStarts(i)(j))))
                Dim stored As Boolean = False
                If Not deltaStyle AndAlso BaseValue IsNot Nothing Then
                    Try
                        entry.Add(PlainScalar("value"), PlainScalar(CStr(BaseValue(i, j, k) + Long.Parse(parts(3)))))
                        stored = True
                    Catch
                    End Try
                End If
                If Not stored Then entry.Add(PlainScalar("delta"), PlainScalar(parts(3)))
                seq.Add(entry)
            Else
                Dim row As New YamlSequenceNode() With {.Style = SequenceStyle.Flow}
                For Each v As String In parts
                    row.Add(PlainScalar(v))
                Next
                seq.Add(row)
            End If
        Next
        Return seq
    End Function

    Private Function BuildButtons(lines As List(Of String)) As YamlMappingNode
        Dim map As New YamlMappingNode()
        For Each ln As String In lines
            Dim m As Match = Regex.Match(ln, "^BtnData(\d+) : (.*)$")
            If Not m.Success Then Continue For
            Dim vals As String() = m.Groups(2).Value.Split(","c)
            Dim groups As Integer = (vals.Length - 1) \ 8
            Dim list As New YamlSequenceNode()
            For g As Integer = 0 To groups - 1
                Dim btn As New YamlMappingNode() With {.Style = MappingStyle.Flow}
                For f As Integer = 0 To 7
                    btn.Add(PlainScalar(BtnFields(f)), ValueScalar(vals(g * 8 + f)))
                Next
                list.Add(btn)
            Next
            map.Add(PlainScalar(m.Groups(1).Value), list)
        Next
        Return map
    End Function

    Private Function BuildRequirements(lines As List(Of String)) As YamlSequenceNode
        Dim seq As New YamlSequenceNode()
        Dim entries As New Dictionary(Of String, YamlMappingNode)
        Dim codes As New Dictionary(Of String, YamlSequenceNode)
        For Each ln As String In lines
            Dim m As Match = Regex.Match(ln, "^Req(Use|Pos|Count|Data)(\d+),(\d+)(?:,(\d+))? : (.*)$")
            If Not m.Success Then Continue For
            Dim id As String = m.Groups(2).Value & "," & m.Groups(3).Value
            If Not entries.ContainsKey(id) Then
                Dim tableIdx As Integer = Integer.Parse(m.Groups(2).Value)
                Dim tableName As String = If(tableIdx >= 0 AndAlso tableIdx < ReqTableNames.Length,
                                             ReqTableNames(tableIdx), m.Groups(2).Value)
                Dim ent As New YamlMappingNode() With {.Style = MappingStyle.Flow}
                ent.Add(PlainScalar("table"), PlainScalar(tableName))
                ent.Add(PlainScalar("id"), PlainScalar(m.Groups(3).Value))
                entries(id) = ent
                codes(id) = New YamlSequenceNode() With {.Style = SequenceStyle.Flow}
                seq.Add(ent)
            End If
            Select Case m.Groups(1).Value
                Case "Use"
                    entries(id).Add(PlainScalar("use"), ValueScalar(m.Groups(5).Value))
                Case "Pos"
                    entries(id).Add(PlainScalar("pos"), ValueScalar(m.Groups(5).Value))
                Case "Data"
                    codes(id).Add(ValueScalar(m.Groups(5).Value))
                    ' "Count" is redundant (always equals the number of Data lines) and is dropped
            End Select
        Next
        For Each id As String In entries.Keys
            entries(id).Add(PlainScalar("code"), codes(id))
        Next
        Return seq
    End Function

    ' ------------------------------------------------------------ trigger tree

    Private Class TrigNode
        Public TypeId As Integer
        Public F1 As String = "False" ' disabled
        Public F2 As String = "False" ' folded
        Public F3 As String = "False" ' notCond
        Public ActName As String
        Public ConName As String
        Public Values As List(Of String)
        Public Kids As New List(Of TrigNode)
    End Class

    Private Function HasValueLine(typeId As Integer) As Boolean
        Return MultiValueTypes.Contains(typeId) OrElse SingleValueTypes.Contains(typeId)
    End Function

    Private Function ParseElement(lines As List(Of String), ByRef i As Integer) As TrigNode
        Dim parts As String() = lines(i).Substring(5).Split(","c) ' "Type:t,b,b,b"
        Dim node As New TrigNode With {
            .TypeId = Integer.Parse(parts(0)), .F1 = parts(1), .F2 = parts(2), .F3 = parts(3)}
        i += 1
        If node.TypeId = 8 Then
            node.ActName = lines(i).Substring(4) ' "act:Name"
            i += 1
        ElseIf node.TypeId = 7 Then
            node.ConName = lines(i).Substring(4) ' "con:Name"
            i += 1
        End If
        If HasValueLine(node.TypeId) Then
            Dim vlines As New List(Of String)
            While Not lines(i).StartsWith("ElementsCount:")
                vlines.Add(lines(i))
                i += 1
            End While
            Dim joined As String = String.Join(vbLf, vlines)
            If MultiValueTypes.Contains(node.TypeId) Then
                node.Values = New List(Of String)(joined.Split(New String() {SEP}, StringSplitOptions.None))
            Else
                node.Values = New List(Of String) From {joined}
            End If
        End If
        Dim childCount As Integer = Integer.Parse(lines(i).Substring("ElementsCount:".Length))
        i += 1
        For k As Integer = 1 To childCount
            node.Kids.Add(ParseElement(lines, i))
        Next
        i += 1 ' skip "END"
        Return node
    End Function

    Private Function BuildTriggers(lines As List(Of String)) As YamlMappingNode
        Dim map As New YamlMappingNode()
        Dim i As Integer = 0
        While i < lines.Count
            Dim m As Match = Regex.Match(lines(i), "^&(\w+)&$")
            If m.Success Then
                i += 1
                map.Add(PlainScalar(m.Groups(1).Value), TrigNodeToYaml(ParseElement(lines, i)))
            Else
                i += 1
            End If
        End While
        Return map
    End Function

    Private Function TrigNodeToYaml(node As TrigNode) As YamlMappingNode
        Dim map As New YamlMappingNode()
        Dim typeName As String = If(node.TypeId >= 0 AndAlso node.TypeId < TypeNames.Length,
                                    TypeNames(node.TypeId), node.TypeId.ToString())
        map.Add(PlainScalar("type"), PlainScalar(typeName))
        If node.F1 = "True" Then map.Add(PlainScalar("disabled"), PlainScalar("true"))
        If node.F2 = "True" Then map.Add(PlainScalar("folded"), PlainScalar("true"))
        If node.F3 = "True" Then map.Add(PlainScalar("notCond"), PlainScalar("true"))
        If node.ActName IsNot Nothing Then map.Add(PlainScalar("action"), New YamlScalarNode(node.ActName))
        If node.ConName IsNot Nothing Then map.Add(PlainScalar("condition"), New YamlScalarNode(node.ConName))
        If node.Values IsNot Nothing Then
            If node.Values.Count = 1 Then
                map.Add(PlainScalar("value"), ValueScalar(node.Values(0)))
            Else
                Dim hasMultiline As Boolean = node.Values.Any(Function(v) v.Contains(vbLf))
                Dim seq As New YamlSequenceNode()
                If Not hasMultiline Then seq.Style = SequenceStyle.Flow
                For Each v As String In node.Values
                    seq.Add(ValueScalar(v))
                Next
                map.Add(PlainScalar("values"), seq)
            End If
        End If
        If node.Kids.Count > 0 Then
            Dim seq As New YamlSequenceNode()
            For Each kid As TrigNode In node.Kids
                seq.Add(TrigNodeToYaml(kid))
            Next
            map.Add(PlainScalar("children"), seq)
        End If
        Return map
    End Function

    ' ------------------------------------------------------------ YAML -> legacy

    Public Function YamlToLegacy(yamlText As String) As String
        Dim stream As New YamlStream()
        stream.Load(New StringReader(yamlText))
        Dim root As YamlMappingNode = CType(stream.Documents(0).RootNode, YamlMappingNode)

        Dim keyToSection As New Dictionary(Of String, String)
        For Each pair As KeyValuePair(Of String, String) In SectionToKey
            keyToSection(pair.Value) = pair.Key
        Next

        Dim out As New List(Of String)
        For Each entry As KeyValuePair(Of YamlNode, YamlNode) In root.Children
            Dim key As String = CType(entry.Key, YamlScalarNode).Value
            If key = "formatVersion" Then Continue For
            Dim secName As String = If(keyToSection.ContainsKey(key), keyToSection(key), key)
            out.Add("S_" & secName)
            Select Case secName
                Case "DatEditSET"
                    EmitDatEdit(entry.Value, out)
                Case "BtnSET"
                    EmitButtons(entry.Value, out)
                Case "ReqSET"
                    EmitRequirements(entry.Value, out)
                Case "TriggerEditorSET"
                    EmitTriggers(entry.Value, out)
                Case Else
                    EmitKvMap(entry.Value, out)
            End Select
            out.Add("E_" & secName)
        Next
        Return String.Join(vbCrLf, out) & vbCrLf
    End Function

    Private Sub EmitKvMap(node As YamlNode, out As List(Of String))
        For Each entry As KeyValuePair(Of YamlNode, YamlNode) In CType(node, YamlMappingNode).Children
            Dim key As String = CType(entry.Key, YamlScalarNode).Value
            If key = "_lines" Then
                For Each item As YamlNode In CType(entry.Value, YamlSequenceNode)
                    out.Add(ScalarIn(item))
                Next
            ElseIf TypeOf entry.Value Is YamlSequenceNode Then
                Dim parts As New List(Of String)
                For Each item As YamlNode In CType(entry.Value, YamlSequenceNode)
                    parts.Add(ScalarIn(item))
                Next
                out.Add(key & " : \" & String.Join("\", parts))
            Else
                out.AddRange((key & " : " & ScalarIn(entry.Value)).Split(New String() {vbLf}, StringSplitOptions.None))
            End If
        Next
    End Sub

    Private Sub EmitDatEdit(node As YamlNode, out As List(Of String))
        For Each item As YamlNode In CType(node, YamlSequenceNode)
            If TypeOf item Is YamlMappingNode Then
                Dim map As YamlMappingNode = CType(item, YamlMappingNode)
                Dim datStr As String = CType(map.Children(New YamlScalarNode("dat")), YamlScalarNode).Value
                Dim fieldStr As String = CType(map.Children(New YamlScalarNode("field")), YamlScalarNode).Value
                Dim i As Integer = Array.IndexOf(E2PDatDefs.DatNames, datStr)
                If i < 0 Then i = Integer.Parse(datStr)
                Dim j As Integer = Array.IndexOf(E2PDatDefs.DatFieldNames(i), fieldStr)
                If j < 0 Then j = Integer.Parse(fieldStr)
                Dim id As Integer = Integer.Parse(ScalarIn(map.Children(New YamlScalarNode("id"))))
                Dim k As Integer = id - E2PDatDefs.DatFieldStarts(i)(j)
                Dim delta As String
                Dim valueKey As New YamlScalarNode("value")
                If map.Children.ContainsKey(valueKey) Then
                    If BaseValue Is Nothing Then
                        Throw New InvalidDataException("datEdit 'value' entries need the base dat data; use 'delta' instead")
                    End If
                    delta = CStr(Long.Parse(ScalarIn(map.Children(valueKey))) - BaseValue(i, j, k))
                Else
                    delta = ScalarIn(map.Children(New YamlScalarNode("delta")))
                End If
                out.Add(i & "," & j & "," & k & "," & delta)
            Else
                Dim vals As New List(Of String)
                For Each v As YamlNode In CType(item, YamlSequenceNode)
                    vals.Add(ScalarIn(v))
                Next
                out.Add(String.Join(",", vals))
            End If
        Next
    End Sub

    Private Sub EmitButtons(node As YamlNode, out As List(Of String))
        For Each entry As KeyValuePair(Of YamlNode, YamlNode) In CType(node, YamlMappingNode).Children
            Dim id As String = CType(entry.Key, YamlScalarNode).Value
            out.Add("BtnUse" & id & " : True")
            Dim row As New StringBuilder()
            For Each btn As YamlNode In CType(entry.Value, YamlSequenceNode)
                Dim btnMap As YamlMappingNode = CType(btn, YamlMappingNode)
                For f As Integer = 0 To 7
                    row.Append(ScalarIn(btnMap.Children(New YamlScalarNode(BtnFields(f))))).Append(","c)
                Next
            Next
            out.Add("BtnData" & id & " : " & row.ToString())
        Next
    End Sub

    Private Sub EmitRequirements(node As YamlNode, out As List(Of String))
        For Each ent As YamlNode In CType(node, YamlSequenceNode)
            Dim map As YamlMappingNode = CType(ent, YamlMappingNode)
            Dim tableStr As String = CType(map.Children(New YamlScalarNode("table")), YamlScalarNode).Value
            Dim tableIdx As Integer = Array.IndexOf(ReqTableNames, tableStr)
            Dim i As String = If(tableIdx >= 0, CStr(tableIdx), tableStr)
            Dim j As String = ScalarIn(map.Children(New YamlScalarNode("id")))
            Dim code As YamlSequenceNode = CType(map.Children(New YamlScalarNode("code")), YamlSequenceNode)
            out.Add("ReqUse" & i & "," & j & " : " & ScalarIn(map.Children(New YamlScalarNode("use"))))
            out.Add("ReqPos" & i & "," & j & " : " & ScalarIn(map.Children(New YamlScalarNode("pos"))))
            out.Add("ReqCount" & i & "," & j & " : " & code.Children.Count)
            For p As Integer = 0 To code.Children.Count - 1
                out.Add("ReqData" & i & "," & j & "," & p & " : " & ScalarIn(code.Children(p)))
            Next
        Next
    End Sub

    Private Sub EmitTriggers(node As YamlNode, out As List(Of String))
        For Each entry As KeyValuePair(Of YamlNode, YamlNode) In CType(node, YamlMappingNode).Children
            out.Add("&" & CType(entry.Key, YamlScalarNode).Value & "&")
            EmitElement(YamlToTrigNode(entry.Value), out)
            out.Add("") ' AppendLine(ToSaveFile) leaves a blank line after each root
        Next
        out.Add("") ' Save() appends SaveTrigger() & vbCrLf: one more blank line
    End Sub

    Private Function YamlToTrigNode(node As YamlNode) As TrigNode
        Dim map As YamlMappingNode = CType(node, YamlMappingNode)
        Dim result As New TrigNode()
        For Each entry As KeyValuePair(Of YamlNode, YamlNode) In map.Children
            Dim key As String = CType(entry.Key, YamlScalarNode).Value
            Select Case key
                Case "type"
                    Dim name As String = CType(entry.Value, YamlScalarNode).Value
                    Dim idx As Integer = Array.IndexOf(TypeNames, name)
                    result.TypeId = If(idx >= 0, idx, Integer.Parse(name))
                Case "disabled"
                    result.F1 = ScalarIn(entry.Value)
                Case "folded"
                    result.F2 = ScalarIn(entry.Value)
                Case "notCond"
                    result.F3 = ScalarIn(entry.Value)
                Case "action"
                    result.ActName = CType(entry.Value, YamlScalarNode).Value
                Case "condition"
                    result.ConName = CType(entry.Value, YamlScalarNode).Value
                Case "value"
                    result.Values = New List(Of String) From {ScalarIn(entry.Value)}
                Case "values"
                    result.Values = New List(Of String)
                    For Each v As YamlNode In CType(entry.Value, YamlSequenceNode)
                        result.Values.Add(ScalarIn(v))
                    Next
                Case "children"
                    For Each kid As YamlNode In CType(entry.Value, YamlSequenceNode)
                        result.Kids.Add(YamlToTrigNode(kid))
                    Next
            End Select
        Next
        If result.Values Is Nothing AndAlso HasValueLine(result.TypeId) Then
            result.Values = New List(Of String) From {""}
        End If
        Return result
    End Function

    Private Sub EmitElement(node As TrigNode, out As List(Of String))
        out.Add("Type:" & node.TypeId & "," & node.F1 & "," & node.F2 & "," & node.F3)
        If node.ActName IsNot Nothing Then out.Add("act:" & node.ActName)
        If node.ConName IsNot Nothing Then out.Add("con:" & node.ConName)
        If HasValueLine(node.TypeId) Then
            Dim joined As String = String.Join(SEP, node.Values)
            out.AddRange(joined.Split(New String() {vbLf}, StringSplitOptions.None))
        End If
        out.Add("ElementsCount:" & node.Kids.Count)
        For Each kid As TrigNode In node.Kids
            EmitElement(kid, out)
        Next
        out.Add("END")
    End Sub

End Module
