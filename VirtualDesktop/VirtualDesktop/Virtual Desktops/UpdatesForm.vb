Imports System.IO, System.Xml

Public Class UpdatesForm

	Dim updateAddress As String
	Dim ui As MainUiPlugin

	Public Sub New(ui As MainUiPlugin)
		Me.ui = ui

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.

	End Sub

	Private Sub UpdatesForm_FormClosing(sender As Object, e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
		If e.CloseReason = CloseReason.UserClosing Then
			e.Cancel = True
			Me.Hide()
		End If
	End Sub

	Private Sub UpdatesForm_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load

	End Sub

	Friend Class UpdateInformation
		Public Version As Version
		Public Info As String
		Public Download As String
		Public Link As String
		Public DownloadedFile As String
		Public FileType As String = "msi"
	End Class

	Private Sub UpdatesFileFound(ar As IAsyncResult)
		Dim dloader As System.Net.HttpWebRequest = ar.AsyncState
		Try
			Dim s = dloader.EndGetResponse(ar).GetResponseStream

			Dim info As New UpdateInformation
			info.Version = My.Application.Info.Version

			Dim doc As New XmlDocument
			doc.Load(s)

			'Find the latest version
			For Each n As XmlNode In doc.DocumentElement.ChildNodes
				If n.Name = "Product" Then
					If n.Attributes("Name").Value = "Finestra" Then
						For Each v As XmlNode In n.ChildNodes
							If v.Name = "Version" Then
								Dim ver As New Version(v.Attributes("Number").Value)
								If info.Version < ver Then
									info.Version = ver
									For Each i As XmlNode In v.ChildNodes
										If i.Name = "Info" Then
											info.Info = i.InnerText
										ElseIf i.Name = "Download" Then
											info.Download = i.InnerText
										ElseIf i.Name = "Link" Then
											info.Link = i.InnerText
										ElseIf i.Name = "Type" Then
											info.FileType = i.InnerText.Trim().ToLower
										End If
									Next
								End If
							End If
						Next
						Exit For
					End If
				End If
			Next

			Me.BeginInvoke(Sub()
							   If info.Version > My.Application.Info.Version Then
								   Me.lblStatus.Text = "Version " & info.Version.ToString & " is now available."
								   updateAddress = info.Download
								   Me.btnDload.Enabled = True
								   Me.prgProg.Style = ProgressBarStyle.Continuous
								   Me.AcceptButton = Me.btnDload
								   ui.VirtualDesktopManager.SendGlobalMessage("An update (" & info.Version.ToString & ") is now available. Click to download it.",
											 ToolTipIcon.Info,
								   Sub()
			   Me.Show()
		   End Sub)
							   Else
								   Me.lblStatus.Text = "No new version is available."
								   Me.prgProg.Style = ProgressBarStyle.Continuous
							   End If
						   End Sub)

		Catch ex As Exception
			Me.BeginInvoke(Sub()
							   Me.lblStatus.Text = "An error occurred while updating. Please try again in a few minutes."
							   Me.prgProg.Style = ProgressBarStyle.Continuous
						   End Sub)
		End Try
	End Sub

	Private Sub btnDload_Click(sender As System.Object, e As System.EventArgs) Handles btnDload.Click
		If Not Me.wrkDload.IsBusy Then Me.wrkDload.RunWorkerAsync()
		btnDload.Enabled = False
	End Sub

	Public Event CheckForUpdatesRequested(sender As Object, e As EventArgs)

	Public Sub Check()
		If Not Me.IsHandleCreated Then Me.CreateHandle()
		RaiseEvent CheckForUpdatesRequested(Me, New EventArgs)
	End Sub

	Private Sub UpdatesForm_Shown(sender As Object, e As System.EventArgs) Handles Me.Shown, Me.CheckForUpdatesRequested
		If updateAddress = "" Then
			Dim dloader As System.Net.HttpWebRequest = System.Net.HttpWebRequest.Create("http://www.z-sys.org/deployment/updates.xml")
			dloader.BeginGetResponse(New AsyncCallback(AddressOf UpdatesFileFound), dloader)
			Me.prgProg.Style = ProgressBarStyle.Marquee
			Me.lblStatus.Text = "Checking for updates..."
		End If
	End Sub

	Private Sub btnCancel_Click(sender As System.Object, e As System.EventArgs) Handles btnCancel.Click
		If Me.wrkDload.IsBusy Then Me.wrkDload.CancelAsync()
		Me.Hide()
	End Sub

	Private Declare Auto Function ShellExecute _
	 Lib "shell32.dll" ( _
	 hwnd As Integer, _
	 lpOperation As String, _
	 lpFile As String, _
	 lpParameters As String, _
	 lpDirectory As String, _
	 nShowCmd As Integer) _
	 As Integer

	Private Sub wrkDload_DoWork(sender As System.Object, e As System.ComponentModel.DoWorkEventArgs) Handles wrkDload.DoWork
		Dim dloader As System.Net.HttpWebRequest = System.Net.HttpWebRequest.Create(updateAddress)
		Dim resp As Net.HttpWebResponse = dloader.GetResponse

		Dim tempPath As String = My.Computer.FileSystem.GetTempFileName & ".msi"
		Using s = resp.GetResponseStream, file As New IO.FileStream(tempPath, IO.FileMode.CreateNew, IO.FileAccess.ReadWrite)
			Me.wrkDload.ReportProgress(0)

			Dim writtenBytes As Integer = 0
			Dim chunk As Integer

			'Download in chunks in order to provide progress
			Do
				Dim kbyte(1000 - 1) As Byte

				chunk = s.Read(kbyte, 0, 1000)
				If chunk > 0 Then
					file.Write(kbyte, 0, chunk)
				End If
				writtenBytes += chunk
				If resp.ContentLength > 0 Then
					Me.wrkDload.ReportProgress(Math.Min(writtenBytes / resp.ContentLength * 100, 100))
				End If

			Loop While chunk > 0 AndAlso Not Me.wrkDload.CancellationPending

		End Using

		'If the user didn't cancel, launch the update
		If Not Me.wrkDload.CancellationPending Then
			Me.wrkDload.ReportProgress(100)
			ShellExecute(0, Nothing, "msiexec.exe", "/i """ & tempPath & """", 0, 1)
			Windows.Forms.Application.Exit()
		Else
			'If the user cancelled, delete the downloaded file
			IO.File.Delete(tempPath)
			e.Cancel = True
		End If
	End Sub

	Private Sub wrkDload_ProgressChanged(sender As Object, e As System.ComponentModel.ProgressChangedEventArgs) Handles wrkDload.ProgressChanged
		Me.prgProg.Value = e.ProgressPercentage
		If e.ProgressPercentage = 100 Then
			Me.btnCancel.Enabled = False
		End If
	End Sub

	Private Sub LinkLabel1_LinkClicked(sender As Object, e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
		ShellExecute(0, "open", "http://www.codeplex.com/vdm/Release/ProjectReleases.aspx", 0, 0, 1)
	End Sub

End Class