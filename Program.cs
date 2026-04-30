using System;
using System.Windows.Forms;

namespace PrintBatchMaster
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 啟用 Windows 視覺化樣式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 啟動 Form1 (請確保您的命名空間與類別名稱正確)
            Application.Run(new Form1());
        }
    }
}