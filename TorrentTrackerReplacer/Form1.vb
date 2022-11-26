Imports BencodeNET.Parsing
Imports BencodeNET.Torrents

Public Class Form1
    Public Sub Info(s As String)
        TextBoxInfo.AppendText(Now.ToString & "> " & s & vbCrLf)
    End Sub
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        If FolderBrowserDialog1.ShowDialog = DialogResult.OK Then
            TextBox1.Text = FolderBrowserDialog1.SelectedPath
        End If
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If Not My.Computer.FileSystem.DirectoryExists(TextBox1.Text) Then
            Info("目录不存在：" & TextBox1.Text)
            Exit Sub
        End If
        Dim so As IO.SearchOption
        If CheckBox2.Checked Then
            so = IO.SearchOption.AllDirectories
        Else
            so = IO.SearchOption.TopDirectoryOnly
        End If
        Dim flist() As IO.FileInfo = My.Computer.FileSystem.GetDirectoryInfo(TextBox1.Text).GetFiles(TextBox2.Text, so)
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
                For Each k As IList(Of String) In t.Trackers
                    For j As Integer = 0 To k.Count - 1
                        k(j) = k(j).Replace(TextBox3.Text, TextBox4.Text)
                        ' k(j) = k(j)
                    Next
                Next
                If CheckBox1.Checked Then
                    Info("保存备份文件：" & f.FullName & "." & tnow & ".bak")
                    My.Computer.FileSystem.CopyFile(f.FullName, f.FullName & "." & tnow & ".bak")
                End If
                Dim s As New IO.FileStream(f.FullName, IO.FileMode.Create)
                t.EncodeTo(s)
                s.Close()
                Info("OK")
            Catch ex As Exception
                Info(ex.ToString)
            End Try
        Next
        Info("运行结束")
    End Sub
End Class
