using System;
using System.Windows;

namespace Certify.UI.Utils
{
    public class Helpers
    {
        public static void LaunchBrowser(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

            }
            catch (Exception)
            {
                MessageBox.Show("Não foi possível iniciar um navegador para " + url);
            }
        }
    }
}
