Public Class NativeWindowEx
	Inherits NativeWindow

	Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
		MyBase.WndProc(m)
		RaiseEvent MessageRecieved(m)
	End Sub

	Public Event MessageRecieved(ByRef m As Message)

End Class
