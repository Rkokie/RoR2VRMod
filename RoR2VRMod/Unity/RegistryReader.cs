using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace VRMod.Unity
{
    public static class RegistryHelper
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegOpenKeyEx(UIntPtr hKey, string subKey, int ulOptions, int samDesired,
            out IntPtr hkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int RegQueryValueEx(IntPtr hKey, string lpValueName, IntPtr lpReserved, out uint lpType,
            StringBuilder lpData, ref uint lpcbData);

        [DllImport("advapi32.dll")]
        private static extern int RegCloseKey(IntPtr hKey);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegEnumValue(IntPtr hKey, uint dwIndex, StringBuilder lpValueName,
            ref uint lpcchValueName,
            IntPtr lpReserved, out uint lpType, IntPtr lpData, ref uint lpcbData);

        private const int KEY_READ = 0x20019;
        private const int ERROR_NO_MORE_ITEMS = 259;
        private static readonly UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002u);

        public class RegistryKeyWrapper : IDisposable
        {
            internal IntPtr _hKey;

            internal RegistryKeyWrapper(IntPtr hKey)
            {
                _hKey = hKey;
            }

            public Dictionary<string, object> GetValues()
            {
                var values = new Dictionary<string, object>();
                uint index = 0;
                uint maxValueNameLength = 16383; // Maximum registry value name length

                while (true)
                {
                    StringBuilder valueName = new StringBuilder((int)maxValueNameLength);
                    uint valueNameLength = maxValueNameLength;
                    uint type;
                    uint dataSize = 0;

                    int result = RegEnumValue(_hKey, index, valueName, ref valueNameLength, IntPtr.Zero, out type,
                        IntPtr.Zero, ref dataSize);

                    if (result == ERROR_NO_MORE_ITEMS)
                        break;

                    if (result == 0)
                    {
                        var data = new StringBuilder((int)dataSize);
                        RegQueryValueEx(_hKey, valueName.ToString(), IntPtr.Zero, out type, data, ref dataSize);

                        // For DWORD values, convert string to int
                        if (type == 4) // REG_DWORD
                        {
                            if (int.TryParse(data.ToString(), out int intValue))
                                values[valueName.ToString()] = intValue;
                        }
                        else
                        {
                            values[valueName.ToString()] = data.ToString();
                        }
                    }

                    index++;
                }

                return values;
            }

            public void Dispose()
            {
                if (_hKey != IntPtr.Zero)
                {
                    RegCloseKey(_hKey);
                    _hKey = IntPtr.Zero;
                }
            }
        }

        public static string GetValue(string fullPath, string valueName, string defaultValue = "")
        {
            string subKey = fullPath;
            if (subKey.StartsWith("HKEY_LOCAL_MACHINE\\", StringComparison.OrdinalIgnoreCase))
            {
                subKey = subKey.Substring("HKEY_LOCAL_MACHINE\\".Length);
            }

            using (var key = OpenSubKey(subKey))
            {
                if (key == null) return defaultValue;

                uint type;
                uint dataSize = 0;
                int result = RegQueryValueEx(key._hKey, valueName, IntPtr.Zero, out type, null, ref dataSize);
                if (result != 0) return defaultValue;

                var data = new StringBuilder((int)dataSize);
                result = RegQueryValueEx(key._hKey, valueName, IntPtr.Zero, out type, data, ref dataSize);
                return result != 0 ? defaultValue : data.ToString();
            }
        }

        public static RegistryKeyWrapper OpenSubKey(string subKey, bool writable = false)
        {
            IntPtr hKey;
            int result = RegOpenKeyEx(HKEY_LOCAL_MACHINE, subKey, 0, KEY_READ, out hKey);
            return result == 0 ? new RegistryKeyWrapper(hKey) : null;
        }
    }
}