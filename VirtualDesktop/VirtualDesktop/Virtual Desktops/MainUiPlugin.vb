''' <summary>
''' Implements a plug-in that provides both the main tray icon and any per-desktop tray icons.
''' </summary>
''' <remarks></remarks>
Public Class MainUiPlugin
	Inherits VirtualDesktopPlugin

	Dim hotkeysEnabled As Boolean
	WithEvents hk As New HotKey
	Dim miniPrevWin As MiniPreviewWindow
	Dim trayIcons As New List(Of NotifyIcon)
	Dim deskIconSel As Integer = 0
	Private Delegate Sub ShowPreviewDelegate(atMouse As Boolean)
	Dim tooltipDesktop As Integer = -1
	Dim messageCallback As Action

	WithEvents _optionsWin As OptionsWindow
	Dim _recoverForm As RecoverForm
	Dim _updatesForm As UpdatesForm
	Dim _welcomeWiz As WelcomeWizard
	Dim _welcomeWin As WelcomeWindow
	Dim thumbMan As ThumbnailManager

	Public ReadOnly Property RecoverForm As RecoverForm
		Get
			If _recoverForm Is Nothing OrElse _recoverForm.IsDisposed Then
				_recoverForm = New RecoverForm(VirtualDesktopManager)
			End If
			Return _recoverForm
		End Get
	End Property

	Public ReadOnly Property UpdatesForm As UpdatesForm
		Get
			If _updatesForm Is Nothing OrElse _updatesForm.IsDisposed Then
				_updatesForm = New UpdatesForm(Me)
			End If
			Return _updatesForm
		End Get
	End Property

	Public ReadOnly Property WelcomeWiz As WelcomeWizard
		Get
			If _welcomeWiz Is Nothing OrElse _welcomeWiz.IsDisposed Then
				_welcomeWiz = New WelcomeWizard(VirtualDesktopManager)
			End If
			Return _welcomeWiz
		End Get
	End Property

	Public ReadOnly Property WelcomeWin As WelcomeWindow
		Get
			If _welcomeWin Is Nothing Then
				_welcomeWin = New WelcomeWindow()
			End If
			Return _welcomeWin
		End Get
	End Property

	Public ReadOnly Property DesktopTrayIcons() As List(Of NotifyIcon)
		Get
			Return trayIcons
		End Get
	End Property

	Public ReadOnly Property ThumbnailManager As ThumbnailManager
		Get
			Return thumbMan
		End Get
	End Property

	Public Sub New(vdm As VirtualDesktopManager)
		MyBase.New(vdm)
		thumbMan = New ThumbnailManager(vdm)
	End Sub

	Public Overrides Sub Start()
		AddHandler VirtualDesktopManager.VirtualDesktopSwitched, AddressOf VDM_DesktopSwitched
		AddHandler VirtualDesktopManager.GlobalMessage, AddressOf VDM_GlobalMessage
		AddHandler VirtualDesktopManager.GlobalCommand, AddressOf VDM_GlobalCommand

		InitializeComponents()
		SetupHotkeys()

		'Show the main tray icon
		notIco.Visible = True

		'Create tray icons for each desktop, if applicable
		If My.Settings.MultTrayIcons Then
			For d As Integer = VirtualDesktopManager.Desktops.Count - 1 To 0 Step -1
				Dim n As New NotifyIcon()
				n.Text = My.Resources.DesktopWord & " " & (d + 1).ToString
				Dim errorOccurred As Boolean = True
				While errorOccurred
					Try
						n.Icon = GetDesktopTrayIcon(d, VirtualDesktopManager.CurrentDesktopIndex = d)
						errorOccurred = False
					Catch ex As Exception
						errorOccurred = True
					End Try
				End While
				n.Visible = True
				AddHandler n.MouseClick, AddressOf switchIcon_MouseClick
				trayIcons.Insert(0, n)
			Next
		End If

		miniPrevWin = New MiniPreviewWindow(VirtualDesktopManager, Me)
		miniPrevWin.CreateControl()
		RecoverForm.CreateControl()
		UpdatesForm.CreateControl()

		'Check for updates
		If My.Settings.AutoCheckForUpdates Then UpdatesForm.Check()

		'Show the welcome window
		If My.Settings.FirstRun Then
			SplashScreen.AddOnCloseHandler(Sub()
											   miniPrevWin.Invoke(Sub()
																	  If Environment.OSVersion.Version >= New Version(6, 0) Then
																		  WelcomeWiz.Show()
																		  WelcomeWiz.Activate()
																	  Else
																		  WelcomeForm.Show()
																		  WelcomeForm.Activate()
																	  End If
																  End Sub)
										   End Sub)
		End If
	End Sub

	Public Overrides Sub [Stop]()
		RemoveHandler VirtualDesktopManager.VirtualDesktopSwitched, AddressOf VDM_DesktopSwitched
		RemoveHandler VirtualDesktopManager.GlobalMessage, AddressOf VDM_GlobalMessage
		RemoveHandler VirtualDesktopManager.GlobalCommand, AddressOf VDM_GlobalCommand

		'Hide all tray icons
		notIco.Visible = False
		For Each n As NotifyIcon In trayIcons
			n.Visible = False
			n.Dispose()
		Next
		trayIcons.Clear()

		'Unregister all hotkeys
		For i As Integer = 1 To 35
			hk.TryUnregisterHotKey(i)
		Next
	End Sub

	Private Sub switchIcon_MouseClick(sender As Object, e As MouseEventArgs)
		If e.Button = Windows.Forms.MouseButtons.Left Then
			VirtualDesktopManager.CurrentDesktopIndex = trayIcons.IndexOf(sender)
		ElseIf e.Button = Windows.Forms.MouseButtons.Right Then
			deskIconSel = trayIcons.IndexOf(sender)
			SwitchToThisDesktopToolStripMenuItem.Font = New Font(SwitchToThisDesktopToolStripMenuItem.Font, FontStyle.Bold)
			mnuDeskIcon.Show(Control.MousePosition)
			mnuDeskIcon.Focus()
		End If
	End Sub

	Private Sub notIco_BalloonTipClicked(sender As Object, e As System.EventArgs) Handles notIco.BalloonTipClicked
		If messageCallback IsNot Nothing Then
			messageCallback.Invoke()
		End If
	End Sub

	Private Sub notIco_MouseClick(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles notIco.MouseClick
		If e.Button = MouseButtons.Left AndAlso My.Settings.ShowMiniPrev AndAlso Not My.Settings.ShowMiniPrevHover AndAlso miniPrevWin IsNot Nothing Then
			miniPrevWin.ShowPreview()
			'miniPrevWin.BeginInvoke(New Action(AddressOf miniPrevWin.ShowPreview), False)
		End If
	End Sub

	Private Sub notIco_MouseMove(sender As Object, e As System.Windows.Forms.MouseEventArgs) Handles notIco.MouseMove
		If My.Settings.ShowMiniPrev AndAlso My.Settings.ShowMiniPrevHover AndAlso miniPrevWin IsNot Nothing Then
			miniPrevWin.ShowPreview()
			'miniPrevWin.BeginInvoke(New Action(AddressOf miniPrevWin.ShowPreview), False)
		End If
	End Sub

	Private Declare Auto Function DestroyIcon Lib "user32.dll" (hico As IntPtr) As Boolean

	Public Shared Function GetDesktopTrayIcon(ind As Integer, selected As Boolean) As Icon
		Using b As New Bitmap(16, 16, Imaging.PixelFormat.Format32bppPArgb)
			Using gr As Graphics = Graphics.FromImage(b)
				gr.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
				gr.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit

				Dim r As New Rectangle(Point.Empty, New Size(15, 15))
				Dim f As New Font(SystemFonts.CaptionFont.FontFamily, 12, GraphicsUnit.Pixel)

				If selected Then
					gr.FillEllipse(Brushes.White, r)
				Else
					gr.FillEllipse(New SolidBrush(Color.FromArgb(255, 100, 100, 100)), r)
					gr.DrawEllipse(New Pen(Brushes.White, 1), r)
				End If
				gr.DrawString((ind + 1).ToString, f, If(selected, Brushes.Black, Brushes.White), r, New StringFormat(StringFormatFlags.NoWrap Or StringFormatFlags.NoClip) With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center})
			End Using
			Dim hico As IntPtr = b.GetHicon
			Dim ret = Icon.FromHandle(hico)
			Return ret
		End Using
	End Function

	Private Sub VDM_DesktopSwitched(prevDesk As Integer, newDesk As Integer)
		'Update the tray icons
		If My.Settings.MultTrayIcons Then
			For i As Integer = 0 To DesktopTrayIcons.Count - 1
				Dim errorOccurred As Boolean = True
				While errorOccurred
					Try
						DesktopTrayIcons(i).Icon = GetDesktopTrayIcon(i, i = newDesk)
						errorOccurred = False
					Catch ex As Exception
						errorOccurred = True
					End Try
				End While
			Next
		End If
	End Sub

	Private Sub VDM_GlobalMessage(message As String, icon As ToolTipIcon, callback As Action)
		miniPrevWin.Invoke(Sub()
							   messageCallback = callback
							   notIco.ShowBalloonTip(5000, "Finestra Virtual Desktops", message, icon)
						   End Sub)
	End Sub

	Private Sub VDM_GlobalCommand(command As String)
		hk_HotKeyPressed(command)
	End Sub

#Region "Controls"

	Private Sub InitializeComponents()
		notIco = New System.Windows.Forms.NotifyIcon
		notIcoMenu = New System.Windows.Forms.ContextMenuStrip
		SwitchToDesktopToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		ShowSwitcherToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		OptionsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		HelpToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		AboutToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		ExitToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		mnuDeskIcon = New System.Windows.Forms.ContextMenuStrip
		SwitchToThisDesktopToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		MoveWindowsToTheCurrentDesktopToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		MoveCurrentWindowsToThisDesktopToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		RecoverWindowsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		UpdatesToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		HotkeysToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		DonateToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem
		'
		'notIco
		'
		notIco.ContextMenuStrip = notIcoMenu
		notIco.Icon = Icon.FromHandle(My.Resources.Finestra16.GetHicon)
		notIco.Text = "Finestra Virtual Desktops"
		notIco.Visible = True
		'
		'notIcoMenu
		'
		notIcoMenu.Items.AddRange(New System.Windows.Forms.ToolStripItem() {SwitchToDesktopToolStripMenuItem, ShowSwitcherToolStripMenuItem, RecoverWindowsToolStripMenuItem, New ToolStripSeparator, HotkeysToolStripMenuItem, OptionsToolStripMenuItem, New ToolStripSeparator, UpdatesToolStripMenuItem, HelpToolStripMenuItem, DonateToolStripMenuItem, AboutToolStripMenuItem, New ToolStripSeparator, ExitToolStripMenuItem})
		notIcoMenu.Name = "notIcoMenu"
		notIcoMenu.Size = New System.Drawing.Size(229, 192)
		'
		'SwitchToDesktopToolStripMenuItem
		'
		SwitchToDesktopToolStripMenuItem.Image = Global.Finestra.My.Resources.Resources.arrow_forward_16
		SwitchToDesktopToolStripMenuItem.Name = "SwitchToDesktopToolStripMenuItem"
		SwitchToDesktopToolStripMenuItem.Size = New System.Drawing.Size(228, 22)
		SwitchToDesktopToolStripMenuItem.Text = "&Switch to Desktop"
		'
		'ShowSwitcherToolStripMenuItem
		'
		ShowSwitcherToolStripMenuItem.Name = "ShowSwitcherToolStripMenuItem"
		ShowSwitcherToolStripMenuItem.ShortcutKeyDisplayString = "Windows + Z"
		ShowSwitcherToolStripMenuItem.Size = New System.Drawing.Size(228, 22)
		ShowSwitcherToolStripMenuItem.Text = "Show S&witcher"
		'
		'OptionsToolStripMenuItem
		'
		OptionsToolStripMenuItem.Image = Global.Finestra.My.Resources.Resources.applications_16
		OptionsToolStripMenuItem.Name = "OptionsToolStripMenuItem"
		OptionsToolStripMenuItem.Size = New System.Drawing.Size(228, 22)
		OptionsToolStripMenuItem.Text = "&Options"
		'
		'UpdatesToolStripMenuItem
		'
		UpdatesToolStripMenuItem.Name = "UpdatesToolStripMenuItem"
		UpdatesToolStripMenuItem.Size = New System.Drawing.Size(228, 22)
		UpdatesToolStripMenuItem.Text = "&Check for Updates"
		'
		'HotkeysToolStripMenuItem
		'
		HotkeysToolStripMenuItem.Name = "HotkeysToolStripMenuItem"
		HotkeysToolStripMenuItem.Size = New System.Drawing.Size(228, 22)
		HotkeysToolStripMenuItem.Text = "H&otkeys"
		HotkeysToolStripMenuItem.Checked = True
		HotkeysToolStripMenuItem.CheckOnClick = True
		'
		'HelpToolStripMenuItem
		'
		HelpToolStripMenuItem.Image = Global.Finestra.My.Resources.Resources.Help
		HelpToolStripMenuItem.Name = "HelpToolStripMenuItem"
		HelpToolStripMenuItem.Size = New System.Drawing.Size(228, 22)
		HelpToolStripMenuItem.Text = "&Help"
		'
		'DonateToolStripMenuItem
		'
		DonateToolStripMenuItem.Name = "DonateToolStripMenuItem"
		DonateToolStripMenuItem.Text = "&Donate"
		'
		'AboutToolStripMenuItem
		'
		AboutToolStripMenuItem.Name = "AboutToolStripMenuItem"
		AboutToolStripMenuItem.Size = New System.Drawing.Size(228, 22)
		AboutToolStripMenuItem.Text = "&About"
		'
		'ExitToolStripMenuItem
		'
		ExitToolStripMenuItem.Name = "ExitToolStripMenuItem"
		ExitToolStripMenuItem.Size = New System.Drawing.Size(228, 22)
		ExitToolStripMenuItem.Text = "E&xit"
		'
		'mnuDeskIcon
		'
		mnuDeskIcon.Items.AddRange(New System.Windows.Forms.ToolStripItem() {SwitchToThisDesktopToolStripMenuItem, MoveWindowsToTheCurrentDesktopToolStripMenuItem, MoveCurrentWindowsToThisDesktopToolStripMenuItem})
		mnuDeskIcon.Name = "mnuDeskIcon"
		mnuDeskIcon.Size = New System.Drawing.Size(282, 70)
		'
		'SwitchToThisDesktopToolStripMenuItem
		'
		SwitchToThisDesktopToolStripMenuItem.Name = "SwitchToThisDesktopToolStripMenuItem"
		SwitchToThisDesktopToolStripMenuItem.Size = New System.Drawing.Size(281, 22)
		SwitchToThisDesktopToolStripMenuItem.Text = "&Switch to this Desktop"
		'
		'MoveWindowsToTheCurrentDesktopToolStripMenuItem
		'
		MoveWindowsToTheCurrentDesktopToolStripMenuItem.Name = "MoveWindowsToTheCurrentDesktopToolStripMenuItem"
		MoveWindowsToTheCurrentDesktopToolStripMenuItem.Size = New System.Drawing.Size(281, 22)
		MoveWindowsToTheCurrentDesktopToolStripMenuItem.Text = "&Move Windows to the Current Desktop"
		'
		'MoveCurrentWindowsToThisDesktopToolStripMenuItem
		'
		MoveCurrentWindowsToThisDesktopToolStripMenuItem.Name = "MoveCurrentWindowsToThisDesktopToolStripMenuItem"
		MoveCurrentWindowsToThisDesktopToolStripMenuItem.Size = New System.Drawing.Size(281, 22)
		MoveCurrentWindowsToThisDesktopToolStripMenuItem.Text = "Move Current Windows to this &Desktop"
		'
		'RecoverWindowsToolStripMenuItem
		'
		RecoverWindowsToolStripMenuItem.Name = "RecoverWindowsToolStripMenuItem"
		RecoverWindowsToolStripMenuItem.Size = New System.Drawing.Size(228, 22)
		RecoverWindowsToolStripMenuItem.Text = "&Recover Windows"
	End Sub

	Friend WithEvents notIco As System.Windows.Forms.NotifyIcon
	Friend WithEvents notIcoMenu As System.Windows.Forms.ContextMenuStrip
	Friend WithEvents SwitchToDesktopToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents ShowSwitcherToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents ExitToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents OptionsToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents HelpToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents AboutToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents mnuDeskIcon As System.Windows.Forms.ContextMenuStrip
	Friend WithEvents SwitchToThisDesktopToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents MoveWindowsToTheCurrentDesktopToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents MoveCurrentWindowsToThisDesktopToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents RecoverWindowsToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents UpdatesToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents HotkeysToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem
	Friend WithEvents DonateToolStripMenuItem As System.Windows.Forms.ToolStripMenuItem

#End Region

#Region "Menu Events"

	Private Sub ExitToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles ExitToolStripMenuItem.Click
		Windows.Forms.Application.Exit()
	End Sub

	Private Sub ShowSwitcherToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles ShowSwitcherToolStripMenuItem.Click
		VirtualDesktopManager.SendGlobalCommand("ShowSwitcher")
	End Sub

	Private Sub SwitchToDesktopToolStripMenuItem_DropDownItemClicked(sender As Object, e As System.Windows.Forms.ToolStripItemClickedEventArgs) Handles SwitchToDesktopToolStripMenuItem.DropDownItemClicked
		VirtualDesktopManager.CurrentDesktop = VirtualDesktopManager.Desktops(SwitchToDesktopToolStripMenuItem.DropDownItems.IndexOf(e.ClickedItem))
	End Sub

	Private Sub OptionsToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles OptionsToolStripMenuItem.Click
		VirtualDesktopManager.SendGlobalCommand("ShowOptions")
	End Sub

	Private Sub AboutToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles AboutToolStripMenuItem.Click
		AboutBox.Show()
	End Sub

	Private Sub HelpToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles HelpToolStripMenuItem.Click
		Help.ShowHelp(Nothing, New IO.FileInfo(Windows.Forms.Application.ExecutablePath).DirectoryName & "\" & My.Settings.HelpName)
	End Sub

	Private Sub SwitchToThisDesktopToolStripMenuItem_Click(sender As Object, e As System.EventArgs) Handles SwitchToThisDesktopToolStripMenuItem.Click
		VirtualDesktopManager.CurrentDesktopIndex = deskIconSel
	End Sub

	Private Sub MoveCurrentWindowsToThisDesktopToolStripMenuItem_Click(sender As Object, e As System.EventArgs) Handles MoveCurrentWindowsToThisDesktopToolStripMenuItem.Click
		Dim wins As List(Of WindowInfo) = VirtualDesktopManager.CurrentDesktop.Windows
		SyncLock wins
			For Each w As WindowInfo In wins
				VirtualDesktopManager.SendWindowToDesktop(w, deskIconSel, VirtualDesktopManager.CurrentDesktopIndex)
			Next
		End SyncLock
	End Sub

	Private Sub MoveWindowsToTheCurrentDesktopToolStripMenuItem_Click(sender As Object, e As System.EventArgs) Handles MoveWindowsToTheCurrentDesktopToolStripMenuItem.Click
		Dim wins As List(Of WindowInfo) = VirtualDesktopManager.Desktops(deskIconSel).Windows
		SyncLock wins
			For Each w As WindowInfo In wins
				VirtualDesktopManager.SendWindowToDesktop(w, VirtualDesktopManager.CurrentDesktopIndex, deskIconSel)
			Next
		End SyncLock
		VirtualDesktopManager.Desktops(deskIconSel).Windows.Clear()
		VirtualDesktopManager.Desktops(deskIconSel).WindowsToRestore.Clear()
	End Sub

	Private Sub RecoverWindowsToolStripMenuItem_Click(sender As System.Object, e As System.EventArgs) Handles RecoverWindowsToolStripMenuItem.Click
		RecoverForm.Show()
	End Sub

	Private Sub UpdatesToolStripMenuItem_Click(sender As Object, e As System.EventArgs) Handles UpdatesToolStripMenuItem.Click
		UpdatesForm.Show()
	End Sub

	Private Sub HotkeysToolStripMenuItem_CheckedChanged(sender As Object, e As System.EventArgs) Handles HotkeysToolStripMenuItem.CheckedChanged
		hotkeysEnabled = HotkeysToolStripMenuItem.Checked
	End Sub

	Private Sub notIcoMenu_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles notIcoMenu.Opening
		For Each m As ToolStripMenuItem In SwitchToDesktopToolStripMenuItem.DropDownItems
			m.Text = VirtualDesktopManager.Desktops(SwitchToDesktopToolStripMenuItem.DropDownItems.IndexOf(m)).Name
		Next
	End Sub

	Private Sub DonateToolStripMenuItem_Click(sender As Object, e As System.EventArgs) Handles DonateToolStripMenuItem.Click
		Utilities.LaunchApplication(New ProcessStartInfo("http://www.z-sys.org/donate/") With {.UseShellExecute = True})
	End Sub

#End Region

#Region "Hotkeys"

	Private Sub SetupHotkeys()
		Dim hotkeyErrorStr As New System.Text.StringBuilder
		hotkeyErrorStr.AppendLine(My.Resources.HotkeyInUseError)
		Dim hotkeyErrorStrOrig As Integer = hotkeyErrorStr.Length

		'Change the defaults on Windows 7
		If My.Settings.FirstRun AndAlso Environment.OSVersion.Version >= New Version(6, 1) Then
			My.Settings.ArrowKeyAcc = HotKey.ModifierKeys.Windows Or HotKey.ModifierKeys.Alt
			My.Settings.MiniPrevAcc = 0
		End If

		'Switcher
		ShowSwitcherToolStripMenuItem.ShortcutKeyDisplayString = RegisterHotKeyCheck("ShowSwitcher", My.Settings.SwitcherAcc, My.Settings.SwitcherKey, hotkeyErrorStr)

		'Window Menu
		RegisterHotKeyCheck("WindowMenu", My.Settings.WinMenuAcc, My.Settings.WinMenuKey, hotkeyErrorStr)

		'Switch Desktop with Arrow Keys
		RegisterHotKeyCheck("SwitchDesktopDown", My.Settings.ArrowKeyAcc, Keys.Down, hotkeyErrorStr)
		RegisterHotKeyCheck("SwitchDesktopRight", My.Settings.ArrowKeyAcc, Keys.Right, hotkeyErrorStr)
		RegisterHotKeyCheck("SwitchDesktopUp", My.Settings.ArrowKeyAcc, Keys.Up, hotkeyErrorStr)
		RegisterHotKeyCheck("SwitchDesktopLeft", My.Settings.ArrowKeyAcc, Keys.Left, hotkeyErrorStr)

		'Send Window to Desktop with Arrow Keys
		RegisterHotKeyCheck("SendWindowDown", My.Settings.ArrowKeyWinAcc, Keys.Down, hotkeyErrorStr)
		RegisterHotKeyCheck("SendWindowRight", My.Settings.ArrowKeyWinAcc, Keys.Right, hotkeyErrorStr)
		RegisterHotKeyCheck("SendWindowUp", My.Settings.ArrowKeyWinAcc, Keys.Up, hotkeyErrorStr)
		RegisterHotKeyCheck("SendWindowLeft", My.Settings.ArrowKeyWinAcc, Keys.Left, hotkeyErrorStr)

		'Show All Windows
		RegisterHotKeyCheck("ShowAllWindows", My.Settings.AllWindowsAcc, My.Settings.AllWindowsKey, hotkeyErrorStr)

		'Show All App Windows
		RegisterHotKeyCheck("ShowAllAppWindows", My.Settings.AppWindowsAcc, My.Settings.AppWindowsKey, hotkeyErrorStr)

		'Monitor Swap (Undocumented)
		hk.TryRegisterHotKey("MonitorSwap", HotKey.ModifierKeys.Control Or HotKey.ModifierKeys.Windows, Keys.S)

		'Send to Desktop X
		For i As Integer = 0 To Math.Min(My.Settings.NumDesktops, 9) - 1
			RegisterHotKeyCheck("SendWindow#" & i, My.Settings.SendToDeskAcc, If(My.Settings.SendToDeskNumpad, Keys.NumPad1, Keys.D1) + i, hotkeyErrorStr)
		Next

		'Mini-preview
		RegisterHotKeyCheck("MiniPreview", My.Settings.MiniPrevAcc, My.Settings.MiniPrevKey, hotkeyErrorStr)

		'Switch to Desktop X
		SwitchToDesktopToolStripMenuItem.DropDownItems.Clear()
		For i As Integer = 0 To Math.Min(My.Settings.NumDesktops, 9) - 1
			Dim dd As New ToolStripMenuItem(VirtualDesktopManager.Desktops(i).Name)
			SwitchToDesktopToolStripMenuItem.DropDownItems.Add(dd)
			dd.ShortcutKeyDisplayString = RegisterHotKeyCheck("SwitchDesktop#" & i, My.Settings.DesktopAcc, If(My.Settings.DesktopNumpad, Keys.NumPad1, Keys.D1) + i, hotkeyErrorStr)
		Next

		'Display the hotkey error message if needed
		If hotkeyErrorStr.Length > hotkeyErrorStrOrig Then
			notIco.ShowBalloonTip(5000, "Hot Keys in Use", hotkeyErrorStr.ToString, ToolTipIcon.Warning)
		End If
	End Sub

	Public Function RegisterHotKeyCheck(id As String, acc As HotKey.ModifierKeys, key As Keys, strErrorBuild As System.Text.StringBuilder) As String
		Dim keyStr As New System.Text.StringBuilder
		If (acc And HotKey.ModifierKeys.Control) <> HotKey.ModifierKeys.None Then keyStr.Append("Control + ")
		If (acc And HotKey.ModifierKeys.Alt) <> HotKey.ModifierKeys.None Then keyStr.Append("Alt + ")
		If (acc And HotKey.ModifierKeys.Shift) <> HotKey.ModifierKeys.None Then keyStr.Append("Shift + ")
		If (acc And HotKey.ModifierKeys.Windows) <> HotKey.ModifierKeys.None Then keyStr.Append("Windows + ")
		keyStr.Append(key.ToString)
		If acc > 0 AndAlso Not hk.TryRegisterHotKey(id, acc, key) Then
			strErrorBuild.AppendLine("   " & keyStr.ToString)
		End If
		Return keyStr.ToString
	End Function

	Private Sub hk_HotKeyPressed(id As String) Handles hk.HotKeyPressed
		If hotkeysEnabled Then
			Select Case id
				Case "ShowSwitcher"
					thumbMan.ShowSwitcher()
				Case "ShowOptions"
					If _optionsWin Is Nothing Then
						Dim opt As New OptionsWindow(VirtualDesktopManager)
						System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(opt)
						opt.Show()
						opt.Activate()
						_optionsWin = opt
					Else
						_optionsWin.Activate()
					End If
				Case "WindowMenu"
					thumbMan.ShowWindowMenu()

					'Desktop Switching with Arrow Keys
				Case "SwitchDesktopDown" 'Down
					VirtualDesktopManager.CurrentDesktopIndex = VirtualDesktopManager.GetDesktopDirectional(Direction.Down, VirtualDesktopManager.CurrentDesktopIndex)
				Case "SwitchDesktopRight" 'Right
					VirtualDesktopManager.CurrentDesktopIndex = VirtualDesktopManager.GetDesktopDirectional(Direction.Right, VirtualDesktopManager.CurrentDesktopIndex)
				Case "SwitchDesktopUp" 'Up
					VirtualDesktopManager.CurrentDesktopIndex = VirtualDesktopManager.GetDesktopDirectional(Direction.Up, VirtualDesktopManager.CurrentDesktopIndex)
				Case "SwitchDesktopLeft" 'Left
					VirtualDesktopManager.CurrentDesktopIndex = VirtualDesktopManager.GetDesktopDirectional(Direction.Left, VirtualDesktopManager.CurrentDesktopIndex)

				Case "ShowAllWindows" 'Expose "Show all Windows"
					thumbMan.ShowSwitcher(True)
				Case "ShowAllAppWindows" 'Expose "Show all Current Application Windows"
					thumbMan.ShowSwitcher(True)

				Case "MonitorSwap" 'Monitor swap hotkey (undocumented)
					SwapMonitors()

				Case "MiniPreview"	'Mini-preview
					miniPrevWin.BeginInvoke(New ShowPreviewDelegate(AddressOf miniPrevWin.ShowPreview), New Object() {True})

					'Send Window to Desktop <Arrow Keys>
				Case "SendWindowDown"
					Dim w As WindowInfo = WindowInfo.GetForegroundWindow
					If VirtualDesktopManager.IsWindowValid(w) Then
						VirtualDesktopManager.SendWindowToDesktop(w, VirtualDesktopManager.GetDesktopDirectional(Direction.Down, VirtualDesktopManager.CurrentDesktopIndex), VirtualDesktopManager.CurrentDesktopIndex)
					End If
				Case "SendWindowRight"
					Dim w As WindowInfo = WindowInfo.GetForegroundWindow
					If VirtualDesktopManager.IsWindowValid(w) Then
						VirtualDesktopManager.SendWindowToDesktop(w, VirtualDesktopManager.GetDesktopDirectional(Direction.Right, VirtualDesktopManager.CurrentDesktopIndex), VirtualDesktopManager.CurrentDesktopIndex)
					End If
				Case "SendWindowUp"
					Dim w As WindowInfo = WindowInfo.GetForegroundWindow
					If VirtualDesktopManager.IsWindowValid(w) Then
						VirtualDesktopManager.SendWindowToDesktop(w, VirtualDesktopManager.GetDesktopDirectional(Direction.Up, VirtualDesktopManager.CurrentDesktopIndex), VirtualDesktopManager.CurrentDesktopIndex)
					End If
				Case "SendWindowLeft"
					Dim w As WindowInfo = WindowInfo.GetForegroundWindow
					If VirtualDesktopManager.IsWindowValid(w) Then
						VirtualDesktopManager.SendWindowToDesktop(w, VirtualDesktopManager.GetDesktopDirectional(Direction.Left, VirtualDesktopManager.CurrentDesktopIndex), VirtualDesktopManager.CurrentDesktopIndex)
					End If

				Case Else
					If id.StartsWith("SwitchDesktop#") Then
						'9 hotkeys are for the desktop numbered hotkeys
						VirtualDesktopManager.CurrentDesktopIndex = Integer.Parse(id.Split("#")(1))
					ElseIf id.StartsWith("SendWindow#") Then
						Dim desk As Integer = Integer.Parse(id.Split("#")(1))
						If VirtualDesktopManager.Desktops.Count > desk AndAlso desk <> VirtualDesktopManager.CurrentDesktopIndex Then
							Dim w As WindowInfo = WindowInfo.GetForegroundWindow
							If VirtualDesktopManager.IsWindowValid(w) Then
								VirtualDesktopManager.SendWindowToDesktop(w, desk, VirtualDesktopManager.CurrentDesktopIndex)
							End If
						End If
					End If
			End Select
		End If
	End Sub

	''' <summary>
	''' Swaps all windows between two monitors on the system. This function is undocumented and unsupported.
	''' </summary>
	''' <remarks></remarks>
	Public Sub SwapMonitors()
		If Screen.AllScreens.Length = 2 Then
			Dim mon1 As New List(Of WindowInfo)
			Dim wins As List(Of WindowInfo) = WindowInfo.GetWindows
			For Each w As WindowInfo In wins
				If VirtualDesktopManager.IsWindowValid(w) Then
					If Screen.AllScreens(0).DeviceName = Screen.FromHandle(w.Handle).DeviceName Then
						mon1.Add(w)
					Else
						Try
							If w.Maximized Then
								w.Rectangle = Screen.AllScreens(0).WorkingArea
							ElseIf w.Minimized Then
								w.NormalRectangle = New Rectangle(w.NormalRectangle.Location - Screen.AllScreens(1).Bounds.Location + Screen.AllScreens(0).Bounds.Location, w.NormalRectangle.Size)
							Else
								w.Rectangle = New Rectangle(w.Rectangle.Location - Screen.AllScreens(1).Bounds.Location + Screen.AllScreens(0).Bounds.Location, w.Rectangle.Size)
							End If
						Catch ex As Exception

						End Try
					End If
				End If
			Next
			For Each w As WindowInfo In mon1
				Try
					If w.Maximized Then
						w.Rectangle = Screen.AllScreens(1).WorkingArea
					ElseIf w.Minimized Then
						w.NormalRectangle = New Rectangle(w.NormalRectangle.Location - Screen.AllScreens(0).Bounds.Location + Screen.AllScreens(1).Bounds.Location, w.NormalRectangle.Size)
					Else
						w.Rectangle = New Rectangle(w.Rectangle.Location - Screen.AllScreens(0).Bounds.Location + Screen.AllScreens(1).Bounds.Location, w.Rectangle.Size)
					End If
				Catch ex As Exception

				End Try
			Next
		End If
	End Sub

#End Region

	Private Sub _optionsWin_Closed(sender As Object, e As System.EventArgs) Handles _optionsWin.Closed
		_optionsWin = Nothing
	End Sub

End Class
