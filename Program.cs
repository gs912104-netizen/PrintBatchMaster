using System;
using System.IO;
using System.Windows.Forms;
using ImageMagick;

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
            // === ImageMagick 全域資源限制 ===
            // 目的：載入大型 TIFF 時不會把整張圖塞進 RAM，超過上限自動 spill 到磁碟暫存檔。
            try
            {
                // 取目前可用的實體記憶體當基準（保守一點，留一半給 .NET / Forms / OS）
                ulong totalRam = (ulong)GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
                ulong memBudget = totalRam / 2;
                if (memBudget < 512UL * 1024 * 1024) memBudget = 512UL * 1024 * 1024; // 至少 512MB

                ResourceLimits.Memory = memBudget;          // pixel cache 使用記憶體上限（超過會自動 spill 到磁碟）
                ResourceLimits.Disk = 16UL * 1024 * 1024 * 1024; // 最多用 16GB 磁碟暫存
                ResourceLimits.Thread = (ulong)Environment.ProcessorCount;

                // 暫存檔放到使用者 Temp（純英文路徑，避免中文路徑造成 Magick 失敗）
                string tempDir = Path.Combine(Path.GetTempPath(), "PrintBatchMaster_MagickTmp");
                Directory.CreateDirectory(tempDir);
                MagickNET.SetTempDirectory(tempDir);
            }
            catch
            {
                // 設定失敗就用預設值，不阻擋啟動
            }

            // 啟用 Windows 視覺化樣式
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 啟動 Form1 (請確保您的命名空間與類別名稱正確)
            Application.Run(new Form1());
        }
    }
}