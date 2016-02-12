using System;
using System.Linq;
using System.Windows.Forms;

namespace WinMonkey
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Configure(!(args.Length > 0 && args.Contains("-startup", StringComparer.OrdinalIgnoreCase))));
        }
    }
}