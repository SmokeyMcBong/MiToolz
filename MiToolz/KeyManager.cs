using System.Windows.Forms;

namespace MiToolz
{
    internal static class KeyManager
    {
        private static string ControlKey()
        {
            return "^";
        }

        private static string ShiftKey()
        {
            return "+";
        }

        private static string AltKey()
        {
            return "%";
        }

        internal static void HotKeySender(string keyPressModifier, string keyPress)
        {
            var keyPressCombo = "";

            if (keyPress != "--")
            {
                if (keyPressModifier == "--")
                {
                    keyPressCombo = "{" + keyPress + "}";
                }
                if (keyPressModifier != "--")
                {
                    if (keyPressModifier == "Ctrl")
                    {
                        keyPressModifier = ControlKey();
                    }
                    if (keyPressModifier == "Shft")
                    {
                        keyPressModifier = ShiftKey();
                    }
                    if (keyPressModifier == "Alt")
                    {
                        keyPressModifier = AltKey();
                    }
                    keyPressCombo = "(" + keyPressModifier + "{" + keyPress + "}" + ")";
                }
            }
            SendKeys.SendWait(keyPressCombo);
        }
    }
}
