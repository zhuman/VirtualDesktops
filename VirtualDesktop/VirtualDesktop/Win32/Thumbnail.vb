Public Class Thumbnail
	Implements IDisposable

	Private Declare Function DwmRegisterThumbnail Lib "dwmapi.dll" (hwndDestination As IntPtr, hwndSource As IntPtr, ByRef thumbnailId As IntPtr) As IntPtr
	Private Declare Function DwmUnregisterThumbnail Lib "dwmapi.dll" (thumbnailId As IntPtr) As IntPtr
	Private Declare Function DwmUpdateThumbnailProperties Lib "dwmapi.dll" (thumbnailId As IntPtr, ByRef properties As DWM_THUMBNAIL_PROPERTIES) As IntPtr
	Private Declare Function DwmQueryThumbnailSourceSize Lib "dwmapi.dll" (thumbnailId As IntPtr, ByRef pSize As PSIZE) As IntPtr
	Private Declare Function DwmIsCompositionEnabled Lib "dwmapi.dll" (ByRef pfEnabled As Boolean) As Integer

	<Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Sequential)> _
	Private Structure PSIZE
		Public width As Integer
		Public height As Integer
	End Structure

	<Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Sequential)> _
	Private Structure RECT
		Public left As Integer
		Public top As Integer
		Public right As Integer
		Public bottom As Integer
	End Structure

	<Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Sequential)> _
	Private Structure DWM_THUMBNAIL_PROPERTIES
		Public dwFlags As Integer
		Public rcDestination As RECT
		Public rcSource As RECT
		Public opacity As Byte
		Public fVisible As Boolean
		Public fSourceClientAreaOnly As Boolean
	End Structure

	<Flags()> _
	Private Enum DWM_TNP As Integer
		DWM_TNP_RECTDESTINATION = 1
		DWM_TNP_RECTSOURCE = 2
		DWM_TNP_OPACITY = 4
		DWM_TNP_VISIBLE = 8
		DWM_TNP_SOURCECLIENTAREAONLY = 16
	End Enum

	Dim id As IntPtr

	Public Sub New(source As IntPtr, dest As IntPtr)
		Dim r As IntPtr = DwmRegisterThumbnail(dest, source, id)
		If r <> 0 Then Throw New System.ComponentModel.Win32Exception(r.ToInt32)
	End Sub

	Public Sub New(source As IntPtr, dest As Control)
		Me.New(source, dest.Handle)
	End Sub

	Dim _opacity As Byte = 255
	Dim _destRect As Rectangle
	Dim _visible As Boolean = True
	Dim _useOnlyClient As Boolean
	Dim _useWholeWin As Boolean = True
	Dim _srcRect As Rectangle

	Public Property Opacity() As Byte
		Get
			Return _opacity
		End Get
		Set(value As Byte)
			_opacity = value
		End Set
	End Property

	Public Property DestinationRectangle() As Rectangle
		Get
			Return _destRect
		End Get
		Set(value As Rectangle)
			_destRect = value
		End Set
	End Property

	Public Property Visible() As Boolean
		Get
			Return _visible
		End Get
		Set(value As Boolean)
			_visible = value
		End Set
	End Property

	''' <summary>
	''' Whether or not to use the entire source window, or just the area specified by SourceRect.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property UseEntireSource() As Boolean
		Get
			Return Me._useWholeWin
		End Get
		Set(value As Boolean)
			Me._useWholeWin = value
		End Set
	End Property

	''' <summary>
	''' Whether or not to use only the client area of the source window.
	''' </summary>
	''' <value></value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property UseOnlyClient() As Boolean
		Get
			Return _useOnlyClient
		End Get
		Set(value As Boolean)
			_useOnlyClient = value
		End Set
	End Property

	Public Property SourceRect() As Rectangle
		Get
			Return Me._srcRect
		End Get
		Set(value As Rectangle)
			_srcRect = value
		End Set
	End Property

	Public ReadOnly Property SourceSize() As Size
		Get
			Dim s As PSIZE
			Dim r As IntPtr = DwmQueryThumbnailSourceSize(id, s)
			If r <> 0 Then Throw New System.ComponentModel.Win32Exception(r.ToInt32)
			Return New Size(s.width, s.height)
		End Get
	End Property

	Public Sub UpdateRendering()
		Try
			Dim props As New DWM_THUMBNAIL_PROPERTIES
			props.dwFlags = DWM_TNP.DWM_TNP_OPACITY Or DWM_TNP.DWM_TNP_RECTDESTINATION Or DWM_TNP.DWM_TNP_SOURCECLIENTAREAONLY Or DWM_TNP.DWM_TNP_VISIBLE
			If Me.UseEntireSource = False Then
				props.dwFlags = props.dwFlags Or DWM_TNP.DWM_TNP_RECTSOURCE
				Dim srcR As RECT
				srcR.left = Me.SourceRect.Left
				srcR.top = Me.SourceRect.Top
				srcR.bottom = Me.SourceRect.Bottom
				srcR.right = Me.SourceRect.Right
				props.rcSource = srcR
			End If

			props.fVisible = Me.Visible
			props.fSourceClientAreaOnly = Me.UseOnlyClient

			Dim destR As RECT
			destR.left = Me.DestinationRectangle.Left
			destR.top = Me.DestinationRectangle.Top
			destR.bottom = Me.DestinationRectangle.Bottom
			destR.right = Me.DestinationRectangle.Right
			props.rcDestination = destR

			props.opacity = Me.Opacity

			Dim r As Integer = DwmUpdateThumbnailProperties(id, props)
			If r <> 0 Then Throw New System.ComponentModel.Win32Exception(r)

		Catch ex As ArithmeticException

		End Try
	End Sub

	Public Shared Function IsDWMEnabled() As Boolean
		Dim r As Boolean
		Try
			If DwmIsCompositionEnabled(r) <> 0 Then
				Throw New Exception("Error testing whether DWM composition is enabled.")
			Else
				Return r
			End If
		Catch notfound As DllNotFoundException
			Return False
		End Try
	End Function

	Private disposedValue As Boolean = False		' To detect redundant calls

	Protected Overridable Sub Dispose(disposing As Boolean)
		If Not Me.disposedValue Then
			DwmUnregisterThumbnail(id)
		End If
		Me.disposedValue = True
	End Sub

	Public Sub Dispose() Implements IDisposable.Dispose
		' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
		Dispose(True)
		GC.SuppressFinalize(Me)
	End Sub

	Protected Overrides Sub Finalize()
		Dispose(False)
	End Sub


End Class
