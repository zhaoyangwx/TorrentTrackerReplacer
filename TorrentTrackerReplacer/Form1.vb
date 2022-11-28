﻿Imports BencodeNET.Parsing
Imports BencodeNET.Torrents

Public Class Form1
    Public Sub Info(s As String)
        Me.Invoke(Sub() TextBoxInfo.AppendText(Now.ToString & "> " & s & vbCrLf))
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            TextBox1.Text = FolderBrowserDialog1.SelectedPath
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim T1 As String = TextBox1.Text
        Dim T2 As String = TextBox2.Text
        Dim SkipKW() As String = TextBox5.Text.Split({"|"}, StringSplitOptions.RemoveEmptyEntries)
        If Not T2.EndsWith(".torrent") Then
            If MessageBox.Show("文件后缀似乎有误，应为.torrent，确认继续？", "警告", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
                Exit Sub
            End If
        End If
        Dim th As New Threading.Thread(
            Sub()
                Try
                    If Not My.Computer.FileSystem.DirectoryExists(T1) Then
                        Info("目录不存在：" & T1)
                        Exit Try
                    End If
                    Dim so As IO.SearchOption
                    If CheckBox2.Checked Then
                        so = IO.SearchOption.AllDirectories
                    Else
                        so = IO.SearchOption.TopDirectoryOnly
                    End If
                    Dim flist() As IO.FileInfo = My.Computer.FileSystem.GetDirectoryInfo(T1).GetFiles(T2, so)
                    Info("已找到 " & flist.Length & " 个文件")
                    Dim i As Integer = 0
                    Dim tnow As String = Now.ToString("yyyyMMdd_HHmmss")
                    For Each f As IO.FileInfo In flist
                        Threading.Interlocked.Increment(i)
                        Try
                            Info("[" & i & "/" & flist.Length & "]" & f.FullName)
                            Dim t As Torrent
                            t = New BencodeParser(System.Text.Encoding.UTF8).Parse(Of Torrent)(f.FullName)
                            If t.Pieces Is Nothing Then Throw New Exception("文件无效")
                            If t.Pieces.Length = 0 Then Throw New Exception("文件无效")
                            If t.Pieces.Length Mod 20 <> 0 Then Throw New Exception("文件无效")
                            Dim Skip As Boolean = False
                            Dim Matched As Boolean = False
                            For Each k As IList(Of String) In t.Trackers
                                For j As Integer = 0 To k.Count - 1
                                    For Each KW As String In SkipKW
                                        If k(j).Contains(KW) Then
                                            Skip = True
                                            Info("匹配关键词 " & KW & " 跳过当前文件")
                                            Exit Try
                                        End If
                                    Next
                                    If k(j).Contains(TextBox3.Text) Then
                                        Matched = True
                                        k(j) = k(j).Replace(TextBox3.Text, TextBox4.Text)
                                    End If
                                    ' k(j) = k(j)
                                Next
                            Next
                            If Not Matched Then
                                Info("未找到关键词，跳过当前文件")
                                Exit Try
                            End If
                            If CheckBox1.Checked Then
                                Dim bakpath As String = My.Computer.FileSystem.CombinePath(f.DirectoryName, "Backup_" & tnow)
                                bakpath = My.Computer.FileSystem.CombinePath(bakpath, f.Name)
                                Info("保存备份文件：" & bakpath)
                                My.Computer.FileSystem.CopyFile(f.FullName, bakpath)
                            End If
                            Dim s As New IO.FileStream(f.FullName, IO.FileMode.Create)
                            t.EncodeTo(s)
                            s.Close()
                            Info("替换完成")
                        Catch ex As Exception
                            Info(ex.ToString)
                        End Try
                    Next
                    Info("运行结束")
                Catch ex As Exception
                    Info(ex.ToString)
                End Try
                Invoke(Sub()
                           For Each c As Control In Me.Controls
                               If TypeOf c Is TextBox Then
                                   CType(c, TextBox).ReadOnly = False
                               Else
                                   c.Enabled = True
                               End If
                           Next
                       End Sub)
            End Sub)
        For Each c As Control In Me.Controls
            If TypeOf c Is TextBox Then
                CType(c, TextBox).ReadOnly = True
            Else
                c.Enabled = False
            End If
        Next
        th.Start()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Dim T1 As String = TextBox1.Text
        Dim T2 As String = TextBox2.Text
        Dim SkipKW() As String = TextBox5.Text.Split({"|"}, StringSplitOptions.RemoveEmptyEntries)
        If Not T2.EndsWith(".fastresume") Then
            If MessageBox.Show("文件后缀似乎有误，应为.fastresume，确认继续？", "警告", MessageBoxButtons.OKCancel) = DialogResult.Cancel Then
                Exit Sub
            End If
        End If
        Dim th As New Threading.Thread(
            Sub()
                Try
                    If Not My.Computer.FileSystem.DirectoryExists(T1) Then
                        Info("目录不存在：" & T1)
                        Exit Try
                    End If
                    Dim so As IO.SearchOption
                    If CheckBox2.Checked Then
                        so = IO.SearchOption.AllDirectories
                    Else
                        so = IO.SearchOption.TopDirectoryOnly
                    End If
                    Dim flist() As IO.FileInfo = My.Computer.FileSystem.GetDirectoryInfo(T1).GetFiles(T2, so)
                    Info("已找到 " & flist.Length & " 个文件")
                    Dim i As Integer = 0
                    Dim tnow As String = Now.ToString("yyyyMMdd_HHmmss")
                    For Each f As IO.FileInfo In flist
                        Threading.Interlocked.Increment(i)
                        Try
                            Info("[" & i & "/" & flist.Length & "]" & f.FullName)
                            Dim t() As Byte = My.Computer.FileSystem.ReadAllBytes(f.FullName)
                            Dim ts As String = My.Computer.FileSystem.ReadAllText(f.FullName)
                            ts = ts.Substring(ts.IndexOf("trackersll"))
                            For Each KW As String In SkipKW
                                If ts.Contains(KW) Then
                                    Info("匹配关键词 " & KW & " 跳过当前文件")
                                    Exit Try
                                End If
                            Next
                            If t.Length = 0 Then Throw New Exception("文件无效")

                            Dim RA() As Byte = System.Text.Encoding.UTF8.GetBytes(TextBox3.Text)
                            Dim RB() As Byte = System.Text.Encoding.UTF8.GetBytes(TextBox4.Text)
                            Dim ByteEqual As New Func(Of Byte(), Byte(), Integer, Boolean)(
                                Function(a As Byte(), b As Byte(), startindex As Integer) As Boolean
                                    If b.Length + startindex > a.Length Then Return False
                                    For bi As Integer = 0 To b.Length - 1
                                        If a(bi + startindex) <> b(bi) Then Return False
                                    Next
                                    Return True
                                End Function)

                            Dim tout As New IO.MemoryStream()
                            Dim tPtr As Integer = 0
                            Dim Replaced As Integer = 0
                            While tPtr <= t.Length - 1
                                If Not ByteEqual(t, RA, tPtr) Then
                                    tout.WriteByte(t(tPtr))
                                    tPtr += 1
                                Else
                                    tout.Write(RB, 0, RB.Length)
                                    tPtr += RA.Length
                                    Replaced += 1
                                End If
                            End While
                            If Replaced = 0 Then
                                Info("未匹配到目标文本")
                                Exit Try
                            End If
                            If Replaced > 1 Then Info("警告：匹配次数" & Replaced)
                            t = tout.ToArray()
                            tout = New IO.MemoryStream()
                            tPtr = 0
                            For ti As Integer = 0 To t.Length - 1
                                If ByteEqual(t, System.Text.Encoding.UTF8.GetBytes("trackersll"), ti) Then
                                    tout.Write(t, 0, ti + 10)
                                    For tj As Integer = ti + 10 To t.Length - 1
                                        If ByteEqual(t, System.Text.Encoding.UTF8.GetBytes(":"), tj) Then
                                            Dim slenb(tj - ti - 11) As Byte
                                            Array.Copy(t, ti + 10, slenb, 0, tj - ti - 10)
                                            Dim slen As String = System.Text.Encoding.UTF8.GetString(slenb)
                                            Dim tlen() As Byte = System.Text.Encoding.UTF8.GetBytes((Integer.Parse(slen) + Replaced * (RB.Length - RA.Length)).ToString)
                                            tout.Write(tlen, 0, tlen.Length)
                                            tout.Write(t, tj, t.Length - tj)
                                            Exit For
                                        End If
                                    Next
                                    Exit For
                                End If
                            Next

                            t = tout.ToArray()
                            If CheckBox1.Checked Then
                                Dim bakpath As String = My.Computer.FileSystem.CombinePath(f.DirectoryName, "Backup_" & tnow)
                                bakpath = My.Computer.FileSystem.CombinePath(bakpath, f.Name)
                                Info("保存备份文件：" & bakpath)
                                My.Computer.FileSystem.CopyFile(f.FullName, bakpath)
                            End If
                            My.Computer.FileSystem.WriteAllBytes(f.FullName, t, False)
                            Info("OK")
                        Catch ex As Exception
                            Info(ex.ToString)
                        End Try
                    Next
                    Info("运行结束")
                Catch ex As Exception
                    Info(ex.ToString)
                End Try
                Invoke(Sub()
                           For Each c As Control In Me.Controls
                               If TypeOf c Is TextBox Then
                                   CType(c, TextBox).ReadOnly = False
                               Else
                                   c.Enabled = True
                               End If
                           Next
                       End Sub)
            End Sub)
        For Each c As Control In Me.Controls
            If TypeOf c Is TextBox Then
                CType(c, TextBox).ReadOnly = True
            Else
                c.Enabled = False
            End If
        Next
        th.Start()
    End Sub
End Class
