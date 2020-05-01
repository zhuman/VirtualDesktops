Imports System
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Windows.Forms
Imports System.Runtime.InteropServices

Public Class LayeredForm
	Inherits Form

	Dim _image As Bitmap, _opacity As Byte = 255
	Dim _clickThrough As Boolean = False

	''' <summary>
	''' Gets or sets the background image of the form.
	''' </summary>
	''' <value>An image, in an alpha based format.</value>
	''' <returns></returns>
	''' <remarks></remarks>
	Public Property Image() As Bitmap
		Get
			Image = _image
		End Get
		Set(value As Bitmap)
			_image = value
			Me.CreateControl()
			Me.SetBitmap(value)
		End Set
	End Property

	Public Overridable Overloads Property Opacity() As Byte
		Get
			Opacity = _opacity
		End Get
		Set(value As Byte)
			_opacity = value
			Me.CreateControl()
			SetOpacity(_opacity)
		End Set
	End Property

	Public Shadows ReadOnly Property Width() As Integer
		Get
			If Me.Image Is Nothing Then
				Return 0
			Else
				Return Me.Image.Width
			End If
		End Get
	End Property

	Public Shadows ReadOnly Property Height() As Integer
		Get
			If Me.Image Is Nothing Then
				Return 0
			Else
				Return Me.Image.Height
			End If
		End Get
	End Property

	Public Sub New(clickThrough As Boolean)
		FormBorderStyle = Windows.Forms.FormBorderStyle.None
		_clickThrough = clickThrough
	End Sub

	Public Sub New()
		Me.New(False)
	End Sub

#Region "UpdateLayeredWindow Functions"

	Protected Sub SetBitmap(bitmap As Bitmap)
		SetBitmap(bitmap, _opacity)
	End Sub

	Protected Sub SetBitmap(bitmap As Bitmap, opacity As Byte)
		If Not Me.IsDisposed And bitmap IsNot Nothing Then
			If Not (bitmap.PixelFormat = PixelFormat.Format32bppPArgb) Then
				Throw New ApplicationException("The bitmap must be premultiplied 32bpp with alpha-channel.")
			End If
			Dim screenDc As IntPtr = Win32.GetDC(IntPtr.Zero)
			Dim memDc As IntPtr = Win32.CreateCompatibleDC(screenDc)
			Dim hBitmap As IntPtr = IntPtr.Zero
			Dim oldBitmap As IntPtr = IntPtr.Zero
			Try
				hBitmap = bitmap.GetHbitmap(Color.FromArgb(0))
				oldBitmap = Win32.SelectObject(memDc, hBitmap)
				Dim size As Win32.Size = New Win32.Size(bitmap.Width, bitmap.Height)
				Dim pointSource As Win32.Point = New Win32.Point(0, 0)
				Dim topPos As Win32.Point = New Win32.Point(Left, Top)
				Dim blend As Win32.BLENDFUNCTION = New Win32.BLENDFUNCTION
				blend.BlendOp = Win32.AC_SRC_OVER
				blend.BlendFlags = 0
				blend.SourceConstantAlpha = opacity
				blend.AlphaFormat = Win32.AC_SRC_ALPHA
				Win32.UpdateLayeredWindow(Handle, screenDc, topPos, size, memDc, pointSource, 0, blend, Win32.ULW_ALPHA)
			Finally
				Win32.ReleaseDC(IntPtr.Zero, screenDc)
				If Not (hBitmap = IntPtr.Zero) Then
					Win32.SelectObject(memDc, oldBitmap)
					Win32.DeleteObject(hBitmap)
				End If
				Win32.DeleteDC(memDc)
			End Try
		End If
	End Sub

	Protected Sub SetOpacity(opacity As Byte)
		If Not Me.IsDisposed Then
			Dim blend As Win32.BLENDFUNCTION = New Win32.BLENDFUNCTION
			blend.BlendOp = Win32.AC_SRC_OVER
			blend.BlendFlags = 0
			blend.SourceConstantAlpha = opacity
			blend.AlphaFormat = Win32.AC_SRC_ALPHA
			Win32.UpdateLayeredWindowOpacity(Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, blend, Win32.ULW_ALPHA)
		End If
	End Sub

#End Region

	Protected Overloads Overrides ReadOnly Property CreateParams() As CreateParams
		Get
			Const WS_EX_TRANSPARENT As Integer = &H20&
			Dim cp As CreateParams = MyBase.CreateParams
			If Not Me.DesignMode Then
				cp.ExStyle = cp.ExStyle Or (524288)	'Layered window style constant
				If _clickThrough = True Then cp.ExStyle = cp.ExStyle Or WS_EX_TRANSPARENT
			End If
			Return cp
		End Get
	End Property

	Protected Overrides Sub OnShown(e As System.EventArgs)
		If Not Me.DesignMode Then
			If Me.StartPosition = FormStartPosition.CenterScreen Then
				Me.Location = New Point((Screen.FromPoint(Me.PointToScreen(New Point(0, 0))).Bounds.Width / 2) - (Me.Width / 2), (Screen.FromPoint(Me.PointToScreen(New Point(0, 0))).Bounds.Height / 2) - (Me.Height / 2))
			End If
		End If
		MyBase.OnShown(e)
	End Sub

	Protected Overrides Sub OnHandleCreated(e As System.EventArgs)
		MyBase.OnHandleCreated(e)
		If Me.Image IsNot Nothing Then
			Me.SetBitmap(Me.Image)
		End If
	End Sub

	Private Const WM_PAINT As Integer = &HF
	Private Const WM_ERASEBKGND As Integer = &H14
	Private Const WM_NCPAINT As Integer = &H85
	Private Const WM_PRINT As Integer = &H317
	Private Const WM_PRINTCLIENT As Integer = &H318

	Public Overrides Sub Refresh()
		SetBitmap(Me.Image, Me.Opacity)
	End Sub

#Region "Fading"

	Public Event FadeFinished(sender As Object, e As EventArgs)

	WithEvents fadeTimer As New Timer
	Dim fadeUp As Boolean = True, fadeAmount As Double, fadeIncreaseBy As Double

	''' <summary>
	''' Fades the form in or out determined by its <seealso cref="Form.Visible" /> property.
	''' </summary>
	''' <param name="length">The length of time in milliseconds that it should take the form to fade.</param>
	''' <remarks></remarks>
	Public Sub StartFade(Optional length As Integer = 2000)
		fadeTimer.Interval = 5
		If Me.Visible Then
			fadeAmount = Me.Opacity
			fadeUp = False
		Else
			fadeAmount = 0
			Me.Opacity = 0
			Me.Show()
			fadeUp = True
		End If

		fadeIncreaseBy = (1 / length) * IIf(fadeUp, 255, Me.Opacity) * 5
		fadeTimer.Enabled = True
	End Sub

	Public Sub StopFade()
		fadeTimer.Enabled = False
	End Sub

	Private Sub fadeTimer_Tick(sender As Object, e As System.EventArgs) Handles fadeTimer.Tick
		If fadeUp = True Then
			If Me.Opacity > 254 Then
				fadeTimer.Enabled = False
				RaiseEvent FadeFinished(Me, New EventArgs)
			Else
				fadeAmount += fadeIncreaseBy
				Me.Opacity = CByte(Math.Min(255, fadeAmount))
			End If
		Else
			If Me.Opacity < 1 Then
				fadeTimer.Enabled = False
				Me.Visible = False
				RaiseEvent FadeFinished(Me, New EventArgs)
			Else
				fadeAmount -= fadeIncreaseBy
				Me.Opacity = CByte(Math.Max(0, fadeAmount))
			End If
		End If
	End Sub

#End Region

	''' <summary>
	''' Includes declarations for API calls and required data types.
	''' </summary>
	''' <remarks></remarks>
	Private Class Win32

		Public Enum Bool
			[False] = 0
			[True]
		End Enum

		<StructLayout(LayoutKind.Sequential)>
		Public Structure Point
			Public x As Int32
			Public y As Int32

			Public Sub New(x As Int32, y As Int32)
				Me.x = x
				Me.y = y
			End Sub
		End Structure

		<StructLayout(LayoutKind.Sequential)>
		Public Structure Size
			Public cx As Int32
			Public cy As Int32

			Public Sub New(cx As Int32, cy As Int32)
				Me.cx = cx
				Me.cy = cy
			End Sub
		End Structure

		<StructLayout(LayoutKind.Sequential)>
		Structure ARGB
			Public Blue As Byte
			Public Green As Byte
			Public Red As Byte
			Public Alpha As Byte
		End Structure

		<StructLayout(LayoutKind.Sequential)>
		Public Structure BLENDFUNCTION
			Public BlendOp As Byte
			Public BlendFlags As Byte
			Public SourceConstantAlpha As Byte
			Public AlphaFormat As Byte
		End Structure
		Public Const ULW_COLORKEY As Int32 = 1
		Public Const ULW_ALPHA As Int32 = 2
		Public Const ULW_OPAQUE As Int32 = 4
		Public Const AC_SRC_OVER As Byte = 0
		Public Const AC_SRC_ALPHA As Byte = 1

		Public Declare Function UpdateLayeredWindow Lib "user32.dll" (hwnd As IntPtr, hdcDst As IntPtr, ByRef pptDst As Point, ByRef psize As Size, hdcSrc As IntPtr, ByRef pprSrc As Point, crKey As Int32, ByRef pblend As BLENDFUNCTION, dwFlags As Int32) As Bool
		Public Declare Auto Function UpdateLayeredWindowOpacity Lib "user32" Alias "UpdateLayeredWindow" (hwnd As IntPtr, hdcDst As IntPtr, pptDst As IntPtr, psize As IntPtr, hdcSrc As IntPtr, pprSrc As IntPtr, crKey As Int32, ByRef pblend As BLENDFUNCTION, dwFlags As Int32) As Bool
		Public Declare Function GetDC Lib "user32.dll" (hWnd As IntPtr) As IntPtr
		Public Declare Function ReleaseDC Lib "user32.dll" (hWnd As IntPtr, hDC As IntPtr) As Integer
		Public Declare Function CreateCompatibleDC Lib "gdi32.dll" (hDC As IntPtr) As IntPtr
		Public Declare Function DeleteDC Lib "gdi32.dll" (hdc As IntPtr) As Bool
		Public Declare Function SelectObject Lib "gdi32.dll" (hDC As IntPtr, hObject As IntPtr) As IntPtr
		Public Declare Function DeleteObject Lib "gdi32.dll" (hObject As IntPtr) As Bool

	End Class

End Class