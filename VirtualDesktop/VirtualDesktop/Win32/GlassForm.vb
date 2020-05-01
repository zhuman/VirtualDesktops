Imports System.Runtime.InteropServices

Public Class GlassForm
	Inherits Form

	Public Sub New()
		Me.SetStyle(ControlStyles.SupportsTransparentBackColor, True)
	End Sub

	Private _glassMargin As Padding
	Public Property GlassMargin() As Padding
		Get
			Return _glassMargin
		End Get
		Set(value As Padding)
			_glassMargin = value

			Try
				If NativeMethods.DwmIsCompositionEnabled Then
					Dim m As New NativeMethods.MARGINS With {.Bottom = value.Bottom, .Left = value.Left, .Right = value.Right, .Top = value.Top}
					NativeMethods.DwmExtendFrameIntoClientArea(Me.Handle, m)
				End If
			Catch ex As MissingMethodException

			End Try
		End Set
	End Property

	Public Sub EnableBlur()
		Dim b As NativeMethods.DWM_BLURBEHIND
		b.Enable = True
		b.Flags = NativeMethods.DWM_BB_ENABLE

		NativeMethods.DwmEnableBlurBehindWindow(Me.Handle, b)
	End Sub

	Public Shared Function IsCompositionEnabled() As Boolean
		If Environment.OSVersion.Version.Major < 6 Then
			Return False
		End If

		Return NativeMethods.DwmIsCompositionEnabled()
	End Function

	Public Sub DrawGlassText(hdc As Drawing.IDeviceContext, text As String, font As Font, color As Color, rect As Rectangle, formatting As TextFormatFlags, iGlowSize As Integer)
		Dim destdc As IntPtr = hdc.GetHdc()
		DrawGlassText(destdc, text, font, color, rect, formatting, iGlowSize)
		hdc.ReleaseHdc()
	End Sub

	Public Sub DrawGlassText(ctrl As Control, text As String, font As Font, color As Color, rect As Rectangle, formatting As TextFormatFlags, iGlowSize As Integer)
		Dim destdc As IntPtr = NativeMethods.GetDC(ctrl.Handle)
		DrawGlassText(destdc, text, font, color, rect, formatting, iGlowSize)
	End Sub

	Public Sub DrawGlassText(hdc As IntPtr, text As [String], font As Font, color As Color, rect As Rectangle, formatting As TextFormatFlags, iGlowSize As Integer)
		If IsCompositionEnabled() Then
			Dim rc As NativeMethods.RECT
			Dim rc2 As NativeMethods.RECT

			rc.left = rect.Left - iGlowSize
			rc.right = rect.Right + 2 * iGlowSize
			rc.top = rect.Top - iGlowSize
			rc.bottom = rect.Bottom + 2 * iGlowSize

			'Just the same rect with rc,but (0,0) at the lefttop
			rc2.left = iGlowSize
			rc2.top = iGlowSize
			rc2.right = rect.Right - rect.Left + iGlowSize
			rc2.bottom = rect.Bottom - rect.Top + iGlowSize

			'hwnd must be the handle of form,not control
			Dim Memdc As IntPtr = NativeMethods.CreateCompatibleDC(hdc)

			' Set up a memory DC where we'll draw the text.
			Dim bitmap As IntPtr
			Dim bitmapOld As IntPtr = IntPtr.Zero
			Dim logfnotOld As IntPtr

			'Text format
			Dim uFormat As Integer = formatting	'NativeInterop.DT_SINGLELINE Or NativeInterop.DT_CENTER Or NativeInterop.DT_VCENTER Or NativeInterop.DT_NOPREFIX

			Dim dib As New NativeMethods.BITMAPINFO()
			'Negative because DrawThemeTextEx() uses a top-down DIB
			dib.bmiHeader.biHeight = -(rc.bottom - rc.top)
			dib.bmiHeader.biWidth = rc.right - rc.left
			dib.bmiHeader.biPlanes = 1
			dib.bmiHeader.biSize = Marshal.SizeOf(GetType(NativeMethods.BITMAPINFOHEADER))
			dib.bmiHeader.biBitCount = 32
			dib.bmiHeader.biCompression = NativeMethods.BI_RGB

			If Not (NativeMethods.SaveDC(Memdc) = 0) Then

				'Create a 32-bit bmp for use in offscreen drawing when glass is on
				bitmap = NativeMethods.CreateDIBSection(Memdc, dib, NativeMethods.DIB_RGB_COLORS, 0, IntPtr.Zero, 0)

				If Not (bitmap = IntPtr.Zero) Then
					bitmapOld = NativeMethods.SelectObject(Memdc, bitmap)
					Dim hFont As IntPtr = font.ToHfont()
					logfnotOld = NativeMethods.SelectObject(Memdc, hFont)
					Try
						Dim renderer As New System.Windows.Forms.VisualStyles.VisualStyleRenderer(System.Windows.Forms.VisualStyles.VisualStyleElement.Window.Caption.Active)

						Dim dttOpts As New NativeMethods.DTTOPTS()
						dttOpts.dwSize = CUInt(Marshal.SizeOf(GetType(NativeMethods.DTTOPTS)))
						dttOpts.dwFlags = NativeMethods.DTT_COMPOSITED Or NativeMethods.DTT_GLOWSIZE Or NativeMethods.DTT_TEXTCOLOR
						dttOpts.iGlowSize = iGlowSize
						dttOpts.crText = color.B << 16 Or color.G << 8 Or color.R

						NativeMethods.BitBlt(Memdc, 0, 0, rc.right - rc.left, rc.bottom - rc.top, hdc, rc.left, rc.top, NativeMethods.SRCCOPY)
						NativeMethods.DrawThemeTextEx(renderer.Handle, Memdc, 0, 0, text, -1, uFormat, rc2, dttOpts)
						NativeMethods.BitBlt(hdc, rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top, Memdc, 0, 0, NativeMethods.SRCCOPY)

						'Dim blend As NativeInterop.BLENDFUNCTION
						'blend.BlendOp = NativeInterop.AC_SRC_OVER
						'blend.BlendFlags = 0
						'blend.AlphaFormat = NativeInterop.AC_SRC_ALPHA
						'blend.SourceConstantAlpha = 255
						'NativeInterop.AlphaBlend(hdc, rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top, Memdc, 0, 0, blend)
					Catch e As Exception
						Trace.WriteLine(e.Message)
					End Try

					'Remember to clean up
					NativeMethods.SelectObject(Memdc, bitmapOld)
					NativeMethods.SelectObject(Memdc, logfnotOld)
					NativeMethods.DeleteObject(bitmap)
					NativeMethods.DeleteObject(hFont)

					NativeMethods.ReleaseDC(Memdc, -1)
					NativeMethods.DeleteDC(Memdc)
				End If
			End If
		End If
	End Sub

	Public Sub EnableNcRendering()
		Dim policy As NativeMethods.DWMNCRENDERINGPOLICY = NativeMethods.DWMNCRENDERINGPOLICY.DWMNCRP_ENABLED
		NativeMethods.DwmSetWindowAttribute(Me.Handle, NativeMethods.DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_POLICY, policy, Marshal.SizeOf(policy))
	End Sub

	Protected Overrides Sub OnPaintBackground(e As System.Windows.Forms.PaintEventArgs)
		e.Graphics.Clear(Me.BackColor)
	End Sub

	Protected Overrides Sub OnMove(e As System.EventArgs)

	End Sub

	Friend Class NativeMethods

		Public Const DTT_TEXTCOLOR As Integer = (1 << 0)
		Public Const DTT_BORDERCOLOR As Integer = (1 << 1)
		Public Const DTT_SHADOWCOLOR As Integer = (1 << 2)
		Public Const DTT_SHADOWTYPE As Integer = (1 << 3)
		Public Const DTT_SHADOWOFFSET As Integer = (1 << 4)
		Public Const DTT_BORDERSIZE As Integer = (1 << 5)
		Public Const DTT_FONTPROP As Integer = (1 << 6)
		Public Const DTT_COLORPROP As Integer = (1 << 7)
		Public Const DTT_STATEID As Integer = (1 << 8)
		Public Const DTT_CALCRECT As Integer = (1 << 9)
		Public Const DTT_APPLYOVERLAY As Integer = (1 << 10)
		Public Const DTT_GLOWSIZE As Integer = (1 << 11)
		Public Const DTT_CALLBACK As Integer = (1 << 12)
		Public Const DTT_COMPOSITED As Integer = (1 << 13)

		'Text format consts
		Public Const DT_SINGLELINE As Integer = &H20
		Public Const DT_CENTER As Integer = &H1
		Public Const DT_VCENTER As Integer = &H4
		Public Const DT_NOPREFIX As Integer = &H800

		'Const for BitBlt
		Public Const SRCCOPY As Integer = &HCC0020

		'Consts for CreateDIBSection
		Public Const BI_RGB As Integer = 0
		'Color table in RGBs
		Public Const DIB_RGB_COLORS As Integer = 0

		Public Structure POINTAPI
			Public x As Integer
			Public y As Integer
		End Structure

		Public Structure DTTOPTS
			Public dwSize As UInteger
			Public dwFlags As UInteger
			Public crText As UInteger
			Public crBorder As UInteger
			Public crShadow As UInteger
			Public iTextShadowType As Integer
			Public ptShadowOffset As POINTAPI
			Public iBorderSize As Integer
			Public iFontPropId As Integer
			Public iColorPropId As Integer
			Public iStateId As Integer
			Public fApplyOverlay As Integer
			Public iGlowSize As Integer
			Public pfnDrawTextCallback As IntPtr
			Public lParam As Integer
		End Structure

		Public Structure RECT
			Public left As Integer
			Public top As Integer
			Public right As Integer
			Public bottom As Integer
		End Structure

		Public Structure BITMAPINFOHEADER
			Public biSize As Integer
			Public biWidth As Integer
			Public biHeight As Integer
			Public biPlanes As Short
			Public biBitCount As Short
			Public biCompression As Integer
			Public biSizeImage As Integer
			Public biXPelsPerMeter As Integer
			Public biYPelsPerMeter As Integer
			Public biClrUsed As Integer
			Public biClrImportant As Integer
		End Structure

		Public Structure RGBQUAD
			Public rgbBlue As Byte
			Public rgbGreen As Byte
			Public rgbRed As Byte
			Public rgbReserved As Byte
		End Structure

		Public Structure BITMAPINFO
			Public bmiHeader As BITMAPINFOHEADER
			Public bmiColors As RGBQUAD
		End Structure

		Public Declare Auto Function GetDC Lib "user32.dll" (hdc As IntPtr) As IntPtr
		Public Declare Auto Function SaveDC Lib "gdi32.dll" (hdc As IntPtr) As Integer
		Public Declare Auto Function ReleaseDC Lib "user32.dll" (hdc As IntPtr, state As Integer) As Integer
		Public Declare Auto Function CreateCompatibleDC Lib "gdi32.dll" (hDC As IntPtr) As IntPtr

		Public Declare Auto Function SelectObject Lib "gdi32.dll" (hDC As IntPtr, hObject As IntPtr) As IntPtr
		Public Declare Auto Function DeleteObject Lib "gdi32.dll" (hObject As IntPtr) As Boolean
		Public Declare Auto Function DeleteDC Lib "gdi32.dll" (hdc As IntPtr) As Boolean
		Public Declare Auto Function BitBlt Lib "gdi32.dll" (hdc As IntPtr, nXDest As Integer, nYDest As Integer, nWidth As Integer, nHeight As Integer,
															 hdcSrc As IntPtr, nXSrc As Integer, nYSrc As Integer, dwRop As UInteger) As Boolean

		Public Const AC_SRC_ALPHA As Integer = 1
		Public Const AC_SRC_OVER As Integer = 0

		<StructLayout(LayoutKind.Sequential)>
		Public Structure BLENDFUNCTION
			Public BlendOp As Byte
			Public BlendFlags As Byte
			Public SourceConstantAlpha As Byte
			Public AlphaFormat As Byte
		End Structure

		Public Declare Auto Function AlphaBlend Lib "gdi32.dll" Alias "GdiAlphaBlend" (hdc As IntPtr, nXDest As Integer, nYDest As Integer, nWidth As Integer, nHeight As Integer, hdcSrc As IntPtr,
																					   nXSrc As Integer, nYSrc As Integer, blend As BLENDFUNCTION) As Boolean
		Public Declare Unicode Function DrawThemeTextEx Lib "UxTheme.dll" (hTheme As IntPtr, hdc As IntPtr, iPartId As Integer, iStateId As Integer, text As String, iCharCount As Integer,
																		   dwFlags As Integer, ByRef pRect As RECT, ByRef pOptions As DTTOPTS) As Integer
		Public Declare Auto Function DrawThemeText Lib "UxTheme.dll" (hTheme As IntPtr, hdc As IntPtr, iPartId As Integer, iStateId As Integer, text As String, iCharCount As Integer,
																	  dwFlags1 As Integer, dwFlags2 As Integer, ByRef pRect As RECT) As Integer
		Public Declare Auto Function CreateDIBSection Lib "gdi32.dll" (hdc As IntPtr, ByRef pbmi As BITMAPINFO, iUsage As UInteger, ppvBits As Integer, hSection As IntPtr, dwOffset As UInteger) As IntPtr

		<StructLayout(LayoutKind.Sequential)>
		Public Structure MARGINS
			Public Left As Integer
			Public Right As Integer
			Public Top As Integer
			Public Bottom As Integer
		End Structure

		<DllImport("dwmapi.dll", PreserveSig:=False)> _
		Public Shared Sub DwmExtendFrameIntoClientArea(hwnd As IntPtr, ByRef margins As MARGINS)
		End Sub

		<DllImport("dwmapi.dll", PreserveSig:=False)> _
		Public Shared Function DwmIsCompositionEnabled() As Boolean
		End Function

		Public Const DWM_BB_ENABLE = 1
		Public Const DWM_BB_BLURREGION = 2
		Public Const DWM_BB_TRANSITIONONMAXIMIZED = 4

		<StructLayout(LayoutKind.Sequential)>
		Public Structure DWM_BLURBEHIND
			Public Flags As Integer
			Public Enable As Boolean
			Public RgnBlur As IntPtr
			Public TranstitionOnMaximized As Boolean
		End Structure

		Public Declare Auto Function DwmEnableBlurBehindWindow Lib "dwmapi.dll" (hwnd As IntPtr, ByRef blurBehind As DWM_BLURBEHIND) As IntPtr

		Public Enum DWMWINDOWATTRIBUTE
			DWMWA_NCRENDERING_ENABLED = 1	   ' [get] Is non-client rendering enabled/disabled
			DWMWA_NCRENDERING_POLICY		   ' [set] Non-client rendering policy
			DWMWA_TRANSITIONS_FORCEDISABLED	   ' [set] Potentially enable/forcibly disable transitions
			DWMWA_ALLOW_NCPAINT				   ' [set] Allow contents rendered in the non-client area to be visible on the DWM-drawn frame.
			DWMWA_CAPTION_BUTTON_BOUNDS		   ' [get] Bounds of the caption button area in window-relative space.
			DWMWA_NONCLIENT_RTL_LAYOUT		   ' [set] Is non-client content RTL mirrored
			DWMWA_FORCE_ICONIC_REPRESENTATION  ' [set] Force this window to display iconic thumbnails.
			DWMWA_FLIP3D_POLICY				   ' [set] Designates how Flip3D will treat the window.
			DWMWA_EXTENDED_FRAME_BOUNDS		   ' [get] Gets the extended frame bounds rectangle in screen space
			DWMWA_HAS_ICONIC_BITMAP			   ' [set] Indicates an available bitmap when there is no better thumbnail representation.
			DWMWA_DISALLOW_PEEK				   ' [set] Don't invoke Peek on the window.
			DWMWA_EXCLUDED_FROM_PEEK		   ' [set] LivePreview exclusion information
			DWMWA_LAST
		End Enum

		Public Enum DWMNCRENDERINGPOLICY
			DWMNCRP_USEWINDOWSTYLE ' Enable/disable non-client rendering based on window style
			DWMNCRP_DISABLED	   ' Disabled non-client rendering; window style is ignored
			DWMNCRP_ENABLED		   ' Enabled non-client rendering; window style is ignored
			DWMNCRP_LAST
		End Enum

		Public Declare Auto Function DwmSetWindowAttribute Lib "dwmapi.dll" (hwnd As IntPtr, attr As Integer, ByRef attrVal As Integer, attrSize As Integer) As IntPtr

		Public Enum WindowThemeNonClientAttribute As UInteger
			WTNCA_NODRAWCAPTION = 1
			WTNCA_NODRAWICON = 2
			WTNCA_NOSYSMENU = 4
			WTNCA_NOMIRRORHELP = 8
		End Enum

		<StructLayout(LayoutKind.Sequential)> _
		Public Structure WTA_OPTIONS
			Public Flags As UInteger
			Public Mask As UInteger
		End Structure

		''' <summary>
		''' What Type of Attributes? (Only One is Currently Defined)
		''' </summary>
		Public Enum WindowThemeAttributeType
			WTA_NONCLIENT = 1
		End Enum

		''' <summary>
		''' Set The Window's Theme Attributes
		''' </summary>
		''' <param name="hWnd">The Handle to the Window</param>
		''' <param name="wtype">What Type of Attributes</param>
		''' <param name="attributes">The Attributes to Add/Remove</param>
		''' <param name="size">The Size of the Attributes Struct</param>
		''' <returns>If The Call Was Successful or Not</returns>
		Public Declare Auto Function SetWindowThemeAttribute Lib "UxTheme.dll" (hWnd As IntPtr, wtype As WindowThemeAttributeType, ByRef attributes As WTA_OPTIONS, size As UInteger) As Integer

	End Class

End Class