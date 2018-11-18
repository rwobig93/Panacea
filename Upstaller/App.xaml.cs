using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Upstaller
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool argUpdate = false;
            bool argBeta = false;
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i].ToLower() == "/update")
                {
                    argUpdate = true;
                    argBeta = false;
                }
                else if (e.Args[i].ToLower() == "/update /beta")
                {
                    argUpdate = true;
                    argBeta = true;
                }
                else
                {
                    argUpdate = false;
                    argBeta = false;
                }
            }

            MainWindow window = new Upstaller.MainWindow();
            window.argUpdate = argUpdate;
            window.argBeta = argBeta;
            window.Show();
        }
    }
}
