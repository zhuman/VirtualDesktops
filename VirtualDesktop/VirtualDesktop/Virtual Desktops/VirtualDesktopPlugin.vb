''' <summary>
''' Provides a base class for the virtual desktop plugin model.
''' </summary>
''' <remarks></remarks>
Public MustInherit Class VirtualDesktopPlugin

	Dim vdm As VirtualDesktopManager

	Public ReadOnly Property VirtualDesktopManager As VirtualDesktopManager
		Get
			Return vdm
		End Get
	End Property

	Public Sub New(vdm As VirtualDesktopManager)
		Me.vdm = vdm
	End Sub

	Public MustOverride Sub Start()
	Public MustOverride Sub [Stop]()

End Class

''' <summary>
''' Represents a plugin type that was found and the load attempt.
''' </summary>
''' <remarks></remarks>
Public NotInheritable Class VirtualDesktopLoadedPlugin

	Public Property Type As Type
	Public Property Instance As VirtualDesktopPlugin
	Public Property LoadError As String
	Public Property Name As String
	Public Property Description As String

End Class
