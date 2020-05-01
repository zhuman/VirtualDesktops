Public Module Main

	Public RestartAppOnExit As Boolean = True

	Public Sub Main()
		Threading.Thread.CurrentThread.Name = "Finestra: Primary Thread"

		'Handle all unhandled exceptions
		AddHandler Windows.Forms.Application.ThreadException, AddressOf Application_UnhandledException
		AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf AppDomain_UnhandledException

		'Setup the Winforms settings
		Windows.Forms.Application.SetCompatibleTextRenderingDefault(False)
		Windows.Forms.Application.EnableVisualStyles()

		'Check to see if the program is already running
		Dim createdMutex As Boolean
		Dim m As New Threading.Mutex(True, "Finestra", createdMutex)
		If Not createdMutex Then
			If Environment.GetCommandLineArgs.Length > 1 Then
				CommandListener.SendCommands()
			Else
				MessageBox.Show(My.Resources.AlreadyRunningError, My.Resources.AlreadyRunningErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
			End If
			Exit Sub
		End If

		'Make sure we close before other apps on logoff
		Utilities.SetShutdownPriority(&H300)

		'Try to speed things up
		Try
			Diagnostics.Process.GetCurrentProcess.PriorityClass = ProcessPriorityClass.AboveNormal
		Catch ex As Exception
			Debug.Print("Could not set process priority. " & ex.Message)
		End Try

		'Pull in settings from a previous version
		My.Settings.Reload()
		If Not My.Settings.WasUpgraded Then
			My.Settings.Upgrade()
			My.Settings.WasUpgraded = True
			My.Settings.FirstRun = True
			My.Settings.Save()
		End If

		Dim vdm As New VirtualDesktopManager
		Dim loadedPlugins As New List(Of VirtualDesktopLoadedPlugin)

		'Load all plug-ins
		For Each asm In AppDomain.CurrentDomain.GetAssemblies
			For Each t In asm.GetTypes
				Dim loaded As VirtualDesktopLoadedPlugin = Nothing
				Try
					If Not t.IsAbstract AndAlso GetType(VirtualDesktopPlugin).IsAssignableFrom(t) Then
						Dim cons = t.GetConstructor(New Type() {GetType(VirtualDesktopManager)})
						If cons IsNot Nothing Then
							loaded = New VirtualDesktopLoadedPlugin
							loaded.Type = t
							loaded.Name = t.Name
							loaded.Instance = cons.Invoke(New Object() {vdm})
							loadedPlugins.Add(loaded)
						End If
					End If
				Catch ex As Exception
					If loaded IsNot Nothing Then
						Debug.Print("Error loading plugin " & loaded.Type.FullName)
						loaded.LoadError = ex.ToString
						loadedPlugins.Add(loaded)
					End If
				End Try
			Next
		Next
		vdm.LoadedPlugins = loadedPlugins

		'Register for application restarts
		If Environment.OSVersion.Version.Major >= 6 Then
			Microsoft.WindowsAPICodePack.ApplicationServices.ApplicationRestartRecoveryManager.RegisterForApplicationRestart(New Microsoft.WindowsAPICodePack.ApplicationServices.RestartSettings("", Microsoft.WindowsAPICodePack.ApplicationServices.RestartRestrictions.None))
		End If

		'Here is where we jump to in order to reload settings
		While RestartAppOnExit
			RestartAppOnExit = False
			If My.Settings.EnableSplashScreen Then
				SplashScreen.ShowSplash()
			End If
			My.Settings.Reload()

			'Initialize some null settings if necessary
			If My.Settings.ProgramDesktops Is Nothing Then My.Settings.ProgramDesktops = New Specialized.ListDictionary
			If My.Settings.StickyPrograms Is Nothing Then My.Settings.StickyPrograms = New Specialized.StringCollection

			'Load the monitors to use and adjust if necessary
			Dim useMon As New Specialized.StringCollection
			useMon.AddRange(My.Settings.UseMonitors.Split(New Char() {"|"}, StringSplitOptions.RemoveEmptyEntries))
			Dim newMonString As String = ""
			For Each s As String In useMon
				If Integer.Parse(s) < Screen.AllScreens.Length Then
					newMonString = newMonString & "|" & s
				End If
			Next
			If newMonString = "" Then newMonString = "0"
			My.Settings.UseMonitors = newMonString

			'Create the desktops
			vdm.Start(My.Settings.NumDesktops)

			'Set first-run default platform settings
			If My.Settings.FirstRun Then
				'Disable the icon toolbar by default on Windows 7
				If Environment.OSVersion.Version >= New Version(6, 1) Then
					My.Settings.UseIconToolbar = False
					My.Settings.ShowMiniPrev = False
				End If
			End If

			'Start the plug-ins
			For Each p In loadedPlugins
				If p.Instance IsNot Nothing Then
					p.Instance.Start()
				End If
			Next

			'Process any commands
			If Environment.GetCommandLineArgs.Length > 1 Then
				CommandListener.SendCommands()
			End If

			'Show the welcome window
			If My.Settings.FirstRun Then
				My.Settings.FirstRun = False
			End If

			SplashScreen.CloseSplash()

			'Start the main message loop
			Try
				Windows.Forms.Application.Run()
			Finally
				Try
					'Stop the plug-ins
					For Each p In loadedPlugins
						If p.Instance IsNot Nothing Then
							p.Instance.Stop()
						End If
					Next
				Finally
					'Show all hidden windows before closing
					vdm.Stop()
				End Try
			End Try

			My.Settings.Save()

		End While

		m.ReleaseMutex()
	End Sub

	Private Sub ReportError(ex As Exception)
		MessageBox.Show("An unhandled exception occurred in Finestra:" & ex.Message & vbCrLf & vbCrLf & "An error report will be put on the desktop. Please post it in the online Finestra issue tracker to help improve Finestra.", "Finestra Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
		Try
			Dim name As String, i As Integer = 1
			Do
				name = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Finestra Error " & i & ".txt")
				i += 1
			Loop While IO.File.Exists(name)

			Using writer As New IO.StreamWriter(name)
				writer.WriteLine("Finestra Error Report")
				writer.WriteLine()
				writer.WriteLine(ex.ToString)
			End Using
		Catch ex2 As Exception
			MessageBox.Show("An error occurred while writing the error report to the desktop. " & ex2.Message, "Finestra Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
		End Try
	End Sub

	Private Sub Application_UnhandledException(sender As Object, e As Threading.ThreadExceptionEventArgs)
		ReportError(e.Exception)
	End Sub

	Private Sub AppDomain_UnhandledException(sender As Object, e As UnhandledExceptionEventArgs)
		Dim ex = TryCast(e.ExceptionObject, Exception)
		If ex IsNot Nothing Then
			ReportError(ex)
		End If
	End Sub

End Module
