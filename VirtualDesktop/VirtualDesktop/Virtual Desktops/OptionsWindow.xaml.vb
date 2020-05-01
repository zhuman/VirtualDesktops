Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media

Public Class OptionsWindow

	Dim vdm As VirtualDesktopManager
	Dim shouldRestart As Boolean
	Dim normalPrograms As New List(Of String)
	Dim stickyPrograms As New List(Of String)
	Dim minimizePrograms As New List(Of String)
	Dim desktopPrograms As New Dictionary(Of String, Integer)

	Public Sub New(vdm As VirtualDesktopManager)
		Me.vdm = vdm

		' This call is required by the designer.
		InitializeComponent()

		' Add any initialization after the InitializeComponent() call.
		If Environment.OSVersion.Version.Major >= 6 Then
			Me.Icon = New ImageSourceConverter().ConvertFromString("pack://application:,,/Resources/Main Icon.ico")
		Else
			Me.Icon = New ImageSourceConverter().ConvertFromString("pack://application:,,/Resources/Finestra16.png")
		End If
	End Sub

	Private Function GdiToWpfColor(c As System.Drawing.Color) As System.Windows.Media.Color
		Return Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B)
	End Function

	Private Function WpfToGdiColor(c As System.Windows.Media.Color) As System.Drawing.Color
		Return System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B)
	End Function

	Private Sub LoadSettings()
		shouldRestart = False

		numDesktops.Value = My.Settings.NumDesktops
		clrOverviewBackground.SelectedColor = GdiToWpfColor(My.Settings.PreviewBackground)
		chkSwitchInd.IsChecked = My.Settings.ShowIndicator
		chkIndicCenter.IsChecked = My.Settings.CenterIndicWindow
		sldIndSize.Value = My.Settings.IndicWinSize
		chkMultTrayIcons.IsChecked = My.Settings.MultTrayIcons
		chkMiniPrevIcon.IsChecked = My.Settings.ShowMiniPrev
		chkDeskBack.IsChecked = My.Settings.UseDesktopBackgrounds
		Me.chkIconToolbar.IsChecked = My.Settings.UseIconToolbar
		Me.chkUpdates.IsChecked = My.Settings.AutoCheckForUpdates
		Me.chkThumbs.IsChecked = My.Settings.CacheWindowThumbnails
		Me.chkTaskbar.IsChecked = My.Settings.TaskbarSwitching
		chkWinWatch.IsChecked = My.Settings.EnableWindowWatcher
		Me.cboMiniPrev.SelectedIndex = If(My.Settings.ShowMiniPrevHover, 1, 0)
		Me.chkShowSplash.IsChecked = My.Settings.EnableSplashScreen

		'Monitors
		radMonAll.IsChecked = My.Settings.AllMonitors
		radMonSelected.IsChecked = Not radMonAll.IsChecked
		radMonSelected.IsEnabled = Screen.AllScreens.Length > 1
		Dim useMon As New Specialized.StringCollection
		useMon.AddRange(My.Settings.UseMonitors.Split(New Char() {"|"}, StringSplitOptions.RemoveEmptyEntries))
		For Each s As Screen In Screen.AllScreens
			lstMonitors.Items.Add(s.Bounds.Width.ToString & "x" & s.Bounds.Height.ToString & " - " & s.BitsPerPixel & " bit - " & s.DeviceName)
			If useMon.Contains((lstMonitors.Items.Count - 1).ToString) Then
				lstMonitors.SelectedItems.Add(lstMonitors.Items(lstMonitors.Items.Count - 1))
			End If
		Next
		sldPrevWinFade.Value = My.Settings.FadeSpeed

		'Window animation
		chkAnimWinEnable.IsChecked = My.Settings.WinAnimEnable
		chkAnimWinIn.IsChecked = My.Settings.WinAnimIn
		chkAnimWinOut.IsChecked = My.Settings.WinAnimOut
		sldAnimWinDelay.Value = My.Settings.WinAnimTime

		'Mouse edge switching
		chkEdgeSwitchEnable.IsChecked = My.Settings.EdgeSwitchEnable
		chkEdgeSwitchMouseWrap.IsChecked = My.Settings.EdgeSwitchMouseWrap
		sldEdgeSwitchDelay.Value = My.Settings.EdgeSwitchDelay

		'Rules
		stickyPrograms.Clear()
		minimizePrograms.Clear()
		desktopPrograms.Clear()
		For Each s As String In My.Settings.StickyPrograms
			stickyPrograms.Add(s)
			lstRules.Items.Add(s)
		Next
		For Each s As String In My.Settings.ProgramDesktops.Keys
			If Not stickyPrograms.Contains(s) Then
				desktopPrograms.Add(s, My.Settings.ProgramDesktops(s))
				lstRules.Items.Add(s)
			End If
		Next
		For Each s As String In My.Settings.MinimizePrograms
			minimizePrograms.Add(s)
			If Not lstRules.Items.Contains(s) Then lstRules.Items.Add(s)
		Next

		'Set up desktop names
		lstDeskNames.Items.Clear()
		cboRuleDesktop.Items.Clear()
		For i As Integer = 0 To vdm.Desktops.Count - 1
			If i >= My.Settings.DesktopNames.Count Then
				My.Settings.DesktopNames.Add(My.Resources.DesktopWord & " " & (i + 1).ToString)
			End If
			lstDeskNames.Items.Add(My.Settings.DesktopNames(i))
			cboRuleDesktop.Items.Add(My.Settings.DesktopNames(i))
		Next
		cboRuleDesktop.SelectedIndex = 0

		'Set up the switcher hotkey controls
		hotSwitch.AcceleratorValue = My.Settings.DesktopAcc
		hotSwitch.SelectedKeyIndex = If(My.Settings.DesktopNumpad, 0, 1)
		hotSend.AcceleratorValue = My.Settings.SendToDeskAcc
		hotSend.SelectedKeyIndex = If(My.Settings.SendToDeskNumpad, 0, 1)
		hotWinMenu.AcceleratorValue = My.Settings.WinMenuAcc
		hotWinMenu.SelectedKey = My.Settings.WinMenuKey
		hotArrow.AcceleratorValue = My.Settings.ArrowKeyAcc
		hotArrowSend.AcceleratorValue = My.Settings.ArrowKeyWinAcc
		hotExpose.AcceleratorValue = My.Settings.AllWindowsAcc
		hotExpose.SelectedKey = My.Settings.AllWindowsKey
		hotPreview.AcceleratorValue = My.Settings.MiniPrevAcc
		hotPreview.SelectedKey = My.Settings.MiniPrevKey
		hotOverview.AcceleratorValue = My.Settings.SwitcherAcc
		hotOverview.SelectedKey = My.Settings.SwitcherKey

		'Load the currently loaded plugin list
		lstPlugins.Items.Clear()
		For Each p In vdm.LoadedPlugins
			lstPlugins.Items.Add(p.Name)
			If p.Instance IsNot Nothing Then
				lstPlugins.SelectedItems.Add(lstPlugins.Items(lstPlugins.Items.Count - 1))
			End If
		Next
	End Sub

	Private Sub SaveSettings()
		If numDesktops.Value <> My.Settings.NumDesktops Then
			shouldRestart = True
			My.Settings.NumDesktops = numDesktops.Value
		End If
		If My.Settings.CenterIndicWindow <> chkIndicCenter.IsChecked Then
			shouldRestart = True
			My.Settings.CenterIndicWindow = chkIndicCenter.IsChecked
		End If
		If My.Settings.MiniPrevSize <> sldMiniPrevSize.Value Then
			shouldRestart = True
			My.Settings.MiniPrevSize = sldMiniPrevSize.Value
		End If
		If sldIndSize.Value <> My.Settings.IndicWinSize Then
			shouldRestart = True
			My.Settings.IndicWinSize = sldIndSize.Value
		End If
		If chkIconToolbar.IsChecked <> My.Settings.UseIconToolbar Then
			shouldRestart = True
			My.Settings.UseIconToolbar = chkIconToolbar.IsChecked
		End If
		If My.Settings.TaskbarSwitching <> chkTaskbar.IsChecked Then
			shouldRestart = True
			My.Settings.TaskbarSwitching = chkTaskbar.IsChecked
		End If
		If My.Settings.EnableWindowWatcher <> chkWinWatch.IsChecked Then
			shouldRestart = True
			My.Settings.EnableWindowWatcher = chkWinWatch.IsChecked
		End If
		My.Settings.EnableSplashScreen = chkShowSplash.IsChecked
		My.Settings.PreviewBackground = WpfToGdiColor(clrOverviewBackground.SelectedColor)
		My.Settings.ShowIndicator = chkSwitchInd.IsChecked
		My.Settings.ShowMiniPrev = chkMiniPrevIcon.IsChecked
		My.Settings.UseDesktopBackgrounds = chkDeskBack.IsChecked
		My.Settings.AutoCheckForUpdates = chkUpdates.IsChecked
		My.Settings.CacheWindowThumbnails = chkThumbs.IsChecked
		My.Settings.ShowMiniPrevHover = (cboMiniPrev.SelectedIndex = 1)
		If My.Settings.MultTrayIcons <> chkMultTrayIcons.IsChecked Then
			My.Settings.MultTrayIcons = chkMultTrayIcons.IsChecked
			shouldRestart = True
		End If
		If radMonAll.IsChecked <> My.Settings.AllMonitors Then
			My.Settings.AllMonitors = radMonAll.IsChecked
			shouldRestart = True
		End If
		Dim newMons As String = ""
		For Each i In lstMonitors.SelectedItems
			newMons = newMons & "|" & lstMonitors.Items.IndexOf(i).ToString
		Next
		If newMons <> My.Settings.UseMonitors Then
			My.Settings.UseMonitors = newMons
			shouldRestart = True
		End If
		My.Settings.FadeSpeed = sldPrevWinFade.Value
		My.Settings.StickyPrograms.Clear()
		For Each s As String In stickyPrograms
			My.Settings.StickyPrograms.Add(s)
		Next
		My.Settings.MinimizePrograms.Clear()
		For Each s As String In minimizePrograms
			My.Settings.MinimizePrograms.Add(s)
		Next
		My.Settings.ProgramDesktops.Clear()
		For Each s As String In desktopPrograms.Keys
			My.Settings.ProgramDesktops.Add(s, desktopPrograms(s))
		Next

		For i As Integer = 0 To lstDeskNames.Items.Count - 1
			If My.Settings.DesktopNames.Count > i Then
				My.Settings.DesktopNames(i) = lstDeskNames.Items(i)
			Else
				My.Settings.DesktopNames.Add(lstDeskNames.Items(i))
			End If
			vdm.Desktops(i).Name = lstDeskNames.Items(i)
		Next

		'Window animation
		If My.Settings.WinAnimEnable <> chkAnimWinEnable.IsChecked Then
			My.Settings.WinAnimEnable = chkAnimWinEnable.IsChecked
			shouldRestart = True
		End If
		My.Settings.WinAnimIn = chkAnimWinIn.IsChecked
		My.Settings.WinAnimOut = chkAnimWinOut.IsChecked
		My.Settings.WinAnimTime = sldAnimWinDelay.Value

		'Mouse edge switching
		If My.Settings.EdgeSwitchEnable <> chkEdgeSwitchEnable.IsChecked Then
			My.Settings.EdgeSwitchEnable = chkEdgeSwitchEnable.IsChecked
			shouldRestart = True
		End If
		My.Settings.EdgeSwitchMouseWrap = chkEdgeSwitchMouseWrap.IsChecked
		If My.Settings.EdgeSwitchDelay <> sldEdgeSwitchDelay.Value Then
			My.Settings.EdgeSwitchDelay = sldEdgeSwitchDelay.Value
			shouldRestart = True
		End If

		'Save the hotkeys
		My.Settings.IsDirty = False
		My.Settings.DesktopAcc = hotSwitch.AcceleratorValue
		My.Settings.DesktopNumpad = hotSwitch.SelectedKeyIndex = 0
		My.Settings.SendToDeskAcc = hotSend.AcceleratorValue
		My.Settings.SendToDeskNumpad = hotSend.SelectedKeyIndex = 0
		My.Settings.WinMenuAcc = hotWinMenu.AcceleratorValue
		My.Settings.WinMenuKey = hotWinMenu.SelectedKey
		My.Settings.ArrowKeyAcc = hotArrow.AcceleratorValue
		My.Settings.ArrowKeyWinAcc = hotArrowSend.AcceleratorValue
		My.Settings.AllWindowsAcc = hotExpose.AcceleratorValue
		My.Settings.AllWindowsKey = hotExpose.SelectedKey
		My.Settings.MiniPrevAcc = hotPreview.AcceleratorValue
		My.Settings.MiniPrevKey = hotPreview.SelectedKey
		My.Settings.SwitcherAcc = hotOverview.AcceleratorValue
		My.Settings.SwitcherKey = hotOverview.SelectedKey
		If My.Settings.IsDirty Then shouldRestart = True

		My.Settings.Save()
	End Sub

	Private Sub OptionsWindow_IsVisibleChanged(sender As Object, e As System.Windows.DependencyPropertyChangedEventArgs) Handles Me.IsVisibleChanged
		If Me.IsVisible Then
			LoadSettings()
		End If
	End Sub

	Private Sub btnOk_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btnOk.Click
		If lstMonitors.SelectedItems.Count = 0 Then
			Me.radMonAll.IsChecked = True
		End If
		SaveSettings()
		If shouldRestart Then
			MessageBox.Show(My.Resources.OptionsRestartWarning, My.Resources.OptionsRestartWarningTitle, MessageBoxButtons.OK, MessageBoxIcon.Information)
			RestartAppOnExit = True
			Forms.Application.Exit()
			Me.Close()
		Else
			Me.Close()
		End If
	End Sub

	Private Sub btnCancel_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btnCancel.Click
		Me.Close()
	End Sub

	Private Sub lstDeskNames_SelectionChanged(sender As Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles lstDeskNames.SelectionChanged
		grdDeskNameEdit.IsEnabled = lstDeskNames.SelectedItems.Count > 0
		txtDeskName.Text = lstDeskNames.SelectedItem
	End Sub

	Private Sub btnDeskName_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btnDeskName.Click
		If txtDeskName.Text = String.Empty Then
			MessageBox.Show("You must enter a non-empty desktop name.", "Desktop Name", MessageBoxButton.OK, MessageBoxImage.Warning)
		Else
			lstDeskNames.Items(lstDeskNames.SelectedIndex) = txtDeskName.Text
		End If
	End Sub

	Dim rulesIndexChanging As Boolean = False

	Private Sub lstRules_SelectionChanged(sender As Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles lstRules.SelectionChanged
		rulesIndexChanging = True
		btnRuleRem.IsEnabled = lstRules.SelectedIndex >= 0
		grpRules.IsEnabled = lstRules.SelectedIndex >= 0
		If lstRules.SelectedIndex >= 0 Then
			radRuleSticky.IsChecked = stickyPrograms.Contains(lstRules.SelectedItem)
			chkRuleMinimize.IsChecked = minimizePrograms.Contains(lstRules.SelectedItem)
			radRuleDesktop.IsChecked = desktopPrograms.ContainsKey(lstRules.SelectedItem)
			If radRuleSticky.IsChecked OrElse radRuleDesktop.IsChecked Then
				chkRuleEnforced.IsChecked = True
			Else
				chkRuleEnforced.IsChecked = False
			End If
			If radRuleDesktop.IsChecked Then
				cboRuleDesktop.SelectedIndex = desktopPrograms(lstRules.SelectedItem)
			Else
				cboRuleDesktop.SelectedIndex = 0
			End If
		End If
		rulesIndexChanging = False
	End Sub

	Private Sub btnRuleRem_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btnRuleRem.Click
		If lstRules.SelectedIndex >= 0 Then
			If stickyPrograms.Contains(lstRules.SelectedItem) Then stickyPrograms.Remove(lstRules.SelectedItem)
			If minimizePrograms.Contains(lstRules.SelectedItem) Then minimizePrograms.Remove(lstRules.SelectedItem)
			If desktopPrograms.ContainsKey(lstRules.SelectedItem) Then desktopPrograms.Remove(lstRules.SelectedItem)
			lstRules.Items.RemoveAt(lstRules.SelectedIndex)
		End If
	End Sub

	Private Sub btnRuleAdd_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btnRuleAdd.Click
		If txtRule.Text = String.Empty Then
			MessageBox.Show("You must enter the name of an application. It should match the name of the application's EXE file, without the extension (e.g. ""iexplore"" for Internet Explorer).", "Add Rule", MessageBoxButton.OK, MessageBoxImage.Information)
		Else
			stickyPrograms.Add(txtRule.Text)
			lstRules.Items.Add(txtRule.Text)
			lstRules.SelectedIndex = lstRules.Items.Count - 1
			txtRule.Text = ""
		End If
	End Sub

	Private Sub radRuleSticky_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles radRuleSticky.Checked, radRuleSticky.Unchecked
		If Not rulesIndexChanging AndAlso lstRules.SelectedIndex >= 0 Then
			If radRuleSticky.IsChecked AndAlso radRuleSticky.IsEnabled Then
				If Not stickyPrograms.Contains(lstRules.SelectedItem) Then stickyPrograms.Add(lstRules.SelectedItem)
			Else
				If stickyPrograms.Contains(lstRules.SelectedItem) Then stickyPrograms.Remove(lstRules.SelectedItem)
			End If
		End If
	End Sub

	Private Sub radRuleDesktop_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles radRuleDesktop.Checked, radRuleDesktop.Unchecked
		If Not rulesIndexChanging AndAlso lstRules.SelectedIndex >= 0 Then
			If radRuleDesktop.IsChecked AndAlso radRuleDesktop.IsEnabled Then
				If Not desktopPrograms.ContainsKey(lstRules.SelectedItem) Then desktopPrograms.Add(lstRules.SelectedItem, cboRuleDesktop.SelectedIndex)
			Else
				If desktopPrograms.ContainsKey(lstRules.SelectedItem) Then desktopPrograms.Remove(lstRules.SelectedItem)
			End If
		End If
	End Sub

	Private Sub cboRuleDesktop_SelectionChanged(sender As Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles cboRuleDesktop.SelectionChanged
		If Not rulesIndexChanging AndAlso lstRules.SelectedIndex >= 0 Then
			If radRuleDesktop.IsChecked Then
				desktopPrograms(lstRules.SelectedItem) = cboRuleDesktop.SelectedIndex
			End If
		End If
	End Sub

	Private Sub chkRuleMinimize_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles chkRuleMinimize.Checked, chkRuleMinimize.Unchecked
		If Not rulesIndexChanging AndAlso lstRules.SelectedIndex >= 0 Then
			If chkRuleMinimize.IsChecked Then
				If Not minimizePrograms.Contains(lstRules.SelectedItem) Then minimizePrograms.Add(lstRules.SelectedItem)
			Else
				If minimizePrograms.Contains(lstRules.SelectedItem) Then minimizePrograms.Remove(lstRules.SelectedItem)
			End If
		End If
	End Sub

	Private Sub chkRuleEnforced_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles chkRuleEnforced.Checked, chkRuleEnforced.Unchecked
		radRuleDesktop.IsEnabled = chkRuleEnforced.IsChecked
		radRuleSticky.IsEnabled = chkRuleEnforced.IsChecked
	End Sub

	Private Sub btnDonate_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btnDonate.Click
		Utilities.LaunchApplication(New ProcessStartInfo("http://www.z-sys.org/donate/") With {.UseShellExecute = True})
	End Sub

	Private Sub btnReset_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btnReset.Click
		If MessageBox.Show("Are you sure you want to reset all settings to their default values?", "Reset Options", MessageBoxButton.YesNo, MessageBoxImage.Question) = MessageBoxResult.Yes Then
			My.Settings.Reset()
			My.Settings.Save()

			MessageBox.Show(My.Resources.OptionsRestartWarning, My.Resources.OptionsRestartWarningTitle, MessageBoxButtons.OK, MessageBoxIcon.Information)
			RestartAppOnExit = True
			Forms.Application.Exit()
			Me.Close()
		End If
	End Sub

	Private Sub ShowRulesInfo(sender As Object, e As EventArgs)
		Dim element = TryCast(sender, FrameworkElement)
		If element IsNot Nothing Then
			Dim tip = TryCast(element.ToolTip, String)
			lblRuleHelp.Text = tip
		End If
	End Sub

	Private Sub HideRulesInfo(sender As Object, e As EventArgs)
		lblRuleHelp.Text = My.Resources.OptionsHelpRule
	End Sub

End Class
