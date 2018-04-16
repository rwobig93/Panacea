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
        public static Color enabledColor = Color.FromArgb(100, 0, 129, 24);
        public static Color disabledColor = Color.FromArgb(100, 160, 0, 0);
        public static Brush WinEnableButtonColorOn = new SolidColorBrush(enabledColor);
        public static Brush WinEnableButtonColorOff = new SolidColorBrush(disabledColor);
    }
}
