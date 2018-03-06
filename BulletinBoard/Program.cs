using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace BulletinBoard
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool firstInstance;
            Mutex singleRunMutex = new Mutex(true, "WCBulletinBoard", out firstInstance);
            if (!firstInstance)
            {
                ShowErrorMessage("The Bulletin Board program is already running. Look for the icon on your task bar.");
                return;
            }
            if (args.Length == 0)
            {
                ShowErrorMessage("Usage: BulletinBoard <rootfolder>");
                return;
            }
            if (!System.IO.Directory.Exists(args[0]))
            {
                ShowErrorMessage("First argument is not a folder path");
                return;
            }
            MainForm main = new MainForm();
            main.RootFolder = args[0];
            Application.Run(main);
        }

        private static void ShowErrorMessage(string msg)
        {
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
