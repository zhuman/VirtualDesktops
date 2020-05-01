Module Utilities

	Private Declare Auto Function SetProcessShutdownParameters Lib "kernel32.dll" (level As Integer, flags As Integer) As Boolean

	''' <summary>
	''' Sets the shutdown priority of the current process, changing the order in which this process should be shut down.
	''' </summary>
	''' <param name="priority"></param>
	''' <remarks></remarks>
	Public Sub SetShutdownPriority(priority As Integer)
		SetProcessShutdownParameters(priority, 0)
	End Sub

	''' <summary>
	''' Launches a process on a separate thread in order to prevent blocking
	''' </summary>
	''' <param name="launchParams"></param>
	''' <remarks></remarks>
	Public Sub LaunchApplication(launchParams As ProcessStartInfo)
		Dim t As New Threading.Thread(Sub()
										  Try
											  Process.Start(launchParams)
										  Catch ex As Exception
											  Debug.Print("Error launching process: " & ex.Message)
										  End Try
									  End Sub)
		t.Start()
	End Sub

End Module
