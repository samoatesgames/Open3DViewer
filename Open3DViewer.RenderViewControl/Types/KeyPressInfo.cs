using System.Collections.Generic;

namespace Open3DViewer.RenderViewControl.Types
{
    public class KeyPressInfo
    {
        public const char KeyHome = (char)1;
        public const char KeyEnd = (char)2;

        public static readonly IReadOnlyDictionary<System.Windows.Input.Key, char> KeyMap 
            = new Dictionary<System.Windows.Input.Key, char>
        {
            { System.Windows.Input.Key.Add, '+' },
            { System.Windows.Input.Key.OemPlus, '+' },
            { System.Windows.Input.Key.Subtract, '-' },
            { System.Windows.Input.Key.OemMinus, '-' },
            { System.Windows.Input.Key.Home, KeyHome },
            { System.Windows.Input.Key.End, KeyEnd }
        };

        public char Key { get; }

        public KeyPressInfo(char key)
        {
            Key = key;
        }
    }
}
