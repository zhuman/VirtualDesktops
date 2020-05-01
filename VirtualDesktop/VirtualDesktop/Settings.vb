
Namespace My

	'This class allows you to handle specific events on the settings class:
	' The SettingChanging event is raised before a setting's value is changed.
	' The PropertyChanged event is raised after a setting's value is changed.
	' The SettingsLoaded event is raised after the setting values are loaded.
	' The SettingsSaving event is raised before the setting values are saved.
	Partial Friend NotInheritable Class MySettings

		Dim _isDirty As Boolean = False

		Public Property IsDirty As Boolean
			Get
				Return _isDirty
			End Get
			Set(value As Boolean)
				_isDirty = value
			End Set
		End Property

		Private Sub MySettings_SettingsLoaded(sender As Object, e As System.Configuration.SettingsLoadedEventArgs) Handles Me.SettingsLoaded
			If My.Settings.ProgramDesktopPrograms Is Nothing Then My.Settings.ProgramDesktopPrograms = New Specialized.StringCollection
			If My.Settings.ProgramDesktopDesktops Is Nothing Then My.Settings.ProgramDesktopDesktops = New Specialized.StringCollection
			My.Settings.ProgramDesktops = New Specialized.ListDictionary
			For i As Integer = 0 To My.Settings.ProgramDesktopPrograms.Count - 1
				My.Settings.ProgramDesktops.Add(My.Settings.ProgramDesktopPrograms(i), CInt(My.Settings.ProgramDesktopDesktops(i)))
			Next
		End Sub

		Private Sub MySettings_SettingsSaving(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.SettingsSaving
			If My.Settings.ProgramDesktopPrograms Is Nothing Then My.Settings.ProgramDesktopPrograms = New Specialized.StringCollection
			If My.Settings.ProgramDesktopDesktops Is Nothing Then My.Settings.ProgramDesktopDesktops = New Specialized.StringCollection
			If My.Settings.ProgramDesktops Is Nothing Then My.Settings.ProgramDesktops = New Specialized.ListDictionary
			My.Settings.ProgramDesktopPrograms.Clear()
			My.Settings.ProgramDesktopDesktops.Clear()
			For Each s As String In My.Settings.ProgramDesktops.Keys
				My.Settings.ProgramDesktopPrograms.Add(s)
				My.Settings.ProgramDesktopDesktops.Add(My.Settings.ProgramDesktops(s).ToString)
			Next
			_isDirty = False
		End Sub

		Protected Overrides Sub OnPropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs)
			MyBase.OnPropertyChanged(sender, e)
			_isDirty = True
		End Sub

	End Class

End Namespace
