using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Panacea.Classes
{
    public static class Defaults
    {
        public static Thickness MainGridIn = new Thickness(0, 30, 0, 20);
        public static Thickness MainGridOut = new Thickness(454, 30, -454, 20);
        public static Thickness TraceLBIn = new Thickness(242, 29, 10, 53);
        public static Thickness TraceLBOut = new Thickness(465, 29, -213, 53);
        public static Thickness PingLBasicIn = new Thickness(77, 29, 0, 53);
        public static Thickness PingLBasicOut = new Thickness(549, 29, -260, 53);
        public static Color enabledColor = Color.FromArgb(100, 0, 129, 24);
        public static Color disabledColor = Color.FromArgb(100, 160, 0, 0);
        public static Brush WinEnableButtonColorOn = new SolidColorBrush(enabledColor);
        public static Brush WinEnableButtonColorOff = new SolidColorBrush(disabledColor);
    }
}
