using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace MiToolz
{
    class ConfigManager
    {
        readonly string Path;
        readonly string EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        //ini file management >
        public ConfigManager(string IniPath = null)
        {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
        }

        public string IniRead(string Key, string Section = null)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void IniWrite(string Key, string Value, string Section = null)
        {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        public void IniDeleteKey(string Key, string Section = null)
        {
            IniWrite(Key, null, Section ?? EXE);
        }

        public void IniDeleteSection(string Section = null)
        {
            IniWrite(null, null, Section ?? EXE);
        }

        public bool IniKeyExists(string Key, string Section = null)
        {
            return IniRead(Key, Section).Length > 0;
        }

        //registry key management >
        public string RegReadKeyValue(string subKey, string key)

        {
            string str = string.Empty;

            using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(subKey))

            {
                if (registryKey != null)
                {
                    str = registryKey.GetValue(key).ToString();
                    registryKey.Close();
                }
            }
            return str;
        }

        //xml file management >
        public string XmlRead(string FullXmlFilePath)
        {
            string str = string.Empty;

            XDocument doc = XDocument.Load(FullXmlFilePath);
            var selectors = from elements in doc.Elements("profile").Elements("info")
                            select elements;

            foreach (var element in selectors)
            {
                str = element.Element("profile_name").Value;
            }

            return str;
        }
    }
}
