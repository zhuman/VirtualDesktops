Imports Microsoft.Win32

Public Module RegistryUtils

	''' <summary>
	''' Recursively copies a key and all of its values.
	''' </summary>
	''' <param name="origKey"></param>
	''' <param name="newKey"></param>
	''' <remarks></remarks>
	Public Sub CopyKey(origKey As RegistryKey, newKey As RegistryKey)
		For Each n In origKey.GetValueNames
			newKey.SetValue(n, origKey.GetValue(n))
		Next
		For Each s In origKey.GetSubKeyNames
			Using oldSubKey = origKey.OpenSubKey(s), newSubKey = newKey.CreateSubKey(s)
				CopyKey(oldSubKey, newSubKey)
			End Using
		Next
	End Sub

End Module
