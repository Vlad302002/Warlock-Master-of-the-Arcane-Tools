using System;
using System.Windows.Forms;

namespace WarlockTools
{
    /// <summary>
    /// Warlock Tools — pack / xr / binary / locdata.md
    /// Сделал Vlad302002, истинный Арданской король и покровитель Ардании.
    /// Made by Vlad302002, the true King of Ardania and patron of Ardania.
    /// Слава Ардании!
    /// </summary>
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string err;
            if (!ToolRunner.FindTools(out err))
            {
                MessageBox.Show(
                    err,
                    "Warlock Tools",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 1;
            }

            Application.Run(new MainForm());
            return 0;
        }
    }
}
