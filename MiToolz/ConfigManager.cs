using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace MiToolz
{
    internal class ConfigManager
    {
        private readonly string _path;
        private readonly string _exe = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string @default, StringBuilder retVal, int size, string filePath);

        //ini file management ... >
        public ConfigManager(string iniPath = null)
        {
            _path = new FileInfo(iniPath ?? _exe + ".ini").FullName;
        }

        public string IniRead(string key, string section = null)
        {
            var retVal = new StringBuilder(255);
            GetPrivateProfileString(section ?? _exe, key, "", retVal, 255, _path);
            return retVal.ToString();
        }

        public void IniWrite(string key, string value, string section = null)
        {
            WritePrivateProfileString(section ?? _exe, key, value, _path);
        }

        //registry key management ... >
        public static string RegReadKeyValue(string subKey, string key)

        {
            var str = string.Empty;

            using (var registryKey = Registry.CurrentUser.OpenSubKey(subKey))

            {
                if (registryKey == null) return str;
                str = registryKey.GetValue(key).ToString();
                registryKey.Close();
            }
            return str;
        }

        //xml file management ... >
        public static string XmlRead(string fullXmlFilePath)
        {
            var str = string.Empty;

            var doc = XDocument.Load(fullXmlFilePath);
            var selectors = from elements in doc.Elements("profile").Elements("info")
                            select elements;

            foreach (var element in selectors)
            {
                str = element.Element("profile_name")?.Value;
            }
            return str;
        }
    }
}
