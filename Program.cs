using System;
using System.Windows.Forms;

namespace MouseHzTest
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            MouseHz.Run();
        }
    }
}
