using ImageMagick;
using ImageMagick.Drawing;
using Microsoft.VisualBasic;
using SixLabors.Fonts;
using SixLabors.Fonts.Unicode;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using ToolGood.Words;

namespace PrintBatchMaster
{
    public partial class Form1 : Form
    {
        private string configPath = Path.Combine(Application.StartupPath, "app_settings_v3.json");

        public Form1()
        {
            InitializeComponent();
            BindEvents();
            this.Load += (s, e) => LoadConfig();
            comboBox1.Items.Add("左下");
            comboBox1.Items.Add("左上");
            comboBox1.Items.Add("中下");
            comboBox1.Items.Add("中上");
            comboBox1.Items.Add("右下");
            comboBox1.Items.Add("右上");            
            comboBox1.SelectedIndex = 0;
        }

        private void BindEvents()
        {
            btnBrowseSource.Click += (s, e) => SelectFolder(txtSourceDir);
            btnBrowseOutput.Click += (s, e) => SelectFolder(txtOutputDir);
            btnStart.Click += async (s, e) => await StartProcessing();
        }

        private void SelectFolder(TextBox tb)
        {
            using (var fbd = new FolderBrowserDialog())
                if (fbd.ShowDialog() == DialogResult.OK) tb.Text = fbd.SelectedPath;
        }

        private async Task StartProcessing()
        {
            if (!Directory.Exists(txtSourceDir.Text)) { MessageBox.Show("請選擇有效的來源資料夾"); return; }
            SaveConfig();
            btnStart.Enabled = false;
            txtLog.Clear();
            Log(">>> 批次任務開始...");

            var extensions = new[] { ".png", ".tif", ".tiff", ".jpg", ".jpeg" };
            var files = Directory.EnumerateFiles(txtSourceDir.Text, "*.*", SearchOption.AllDirectories)
                                 .Where(f => extensions.Contains(Path.GetExtension(f).ToLower())).ToList();

            progressBar1.Maximum = files.Count;
            progressBar1.Value = 0;
            // 先取得「選取的目錄」本身的名稱 (例如：CustomerName)
            string rootDirectoryName = new DirectoryInfo(txtSourceDir.Text).Name;
            object lockObj = new object();
            // 使用計數器來生成流水號
            int counter = 1;
            await Task.Run(() =>
            {
                foreach (var f in files)
                {
                    try
                    {
                        // 1. 取得相對路徑與子資料夾文字
                        string relPath = Path.GetRelativePath(txtSourceDir.Text, f);
                        string subFolder = Path.GetDirectoryName(relPath);
                        string subFolderPart = string.IsNullOrEmpty(subFolder) ? "" : subFolder.Replace(Path.DirectorySeparatorChar, '-') + "-";
                        string fileNameOnly = Path.GetFileNameWithoutExtension(f);
                        //自動跳過_ruler結尾的檔案
                        if (fileNameOnly.EndsWith("_ruler"))
                        {
                            // 執行相關邏輯
                            continue;
                        }
                        string extension = Path.GetExtension(f);

                        // 2. 組合標籤文字：『CustomerName』A範本-1_123_Gray_M
                        string label = $"『{rootDirectoryName}』{subFolderPart}{fileNameOnly}";

                        // 3. 組合「不分層」的輸出路徑，並加上流水號
                        // 格式：001_『CustomerName』A範本-1_123_Gray_M.png
                        string newFileName = $"{subFolderPart}{fileNameOnly}_{counter:D3}{extension}";
                        string outPath = Path.Combine(txtOutputDir.Text, newFileName);

                        // 4. 執行圖片處理
                        ProcessImage(f, outPath, label);

                        Log($"[OK] {counter:D3} - {newFileName}");
                        counter++;
                    }
                    catch (Exception ex) { Log($"[ERR] {ex.Message}"); }
                    finally { this.Invoke(new Action(() => progressBar1.Value++)); }
                }
                //Parallel.ForEach(files, f =>
                //{
                //    try
                //    {
                //        // 2. 更新計數器與 Log (需要鎖，避免多人同時改同一個變數)
                //        lock (lockObj)
                //        {
                //            // 1. 取得相對路徑與子資料夾文字
                //            string relPath = Path.GetRelativePath(txtSourceDir.Text, f);
                //            string subFolder = Path.GetDirectoryName(relPath);
                //            string subFolderPart = string.IsNullOrEmpty(subFolder) ? "" : subFolder.Replace(Path.DirectorySeparatorChar, '-') + "-";
                //            string fileNameOnly = Path.GetFileNameWithoutExtension(f);
                //            string extension = Path.GetExtension(f);

                //            // 2. 組合標籤文字：『CustomerName』A範本-1_123_Gray_M
                //            string label = $"『{rootDirectoryName}』{subFolderPart}{fileNameOnly}";

                //            // 3. 組合「不分層」的輸出路徑，並加上流水號
                //            // 格式：001_『CustomerName』A範本-1_123_Gray_M.png
                //            string newFileName = $"{subFolderPart}{fileNameOnly}_{counter:D5}{extension}";
                //            string outPath = Path.Combine(txtOutputDir.Text, newFileName);

                //            // 4. 執行圖片處理
                //            ProcessImage(f, outPath, label);



                //            int currentCount = counter++;
                //            Log($"[OK] {currentCount:D3} - {newFileName}");
                //        }
                //    }
                //    catch (Exception ex) { Log($"[ERR] {ex.Message}"); }
                //    finally
                //    {
                //        // 更新進度條也要 Invoke
                //        this.Invoke(new Action(() => progressBar1.Value++));
                //    }
                //});
            });
            Log(">>> 完成！");
            btnStart.Enabled = true;
        }

        private void ProcessImage(string input, string output, string text)
        {
            double dpiX, dpiY; // 1. 先使用 System.Drawing 讀取檔案標頭來確認解析度
            using (var tempImg = System.Drawing.Image.FromFile(input)) { dpiX = tempImg.HorizontalResolution; dpiY = tempImg.VerticalResolution; }

            using (var img = new MagickImage(input))
            {
                // 2. 如果 System.Drawing 抓到的是 0 或過低的值，改用 ImageMagick 判斷
                if (dpiX <= 0)
                {
                    if (img.Density.X > 0) { dpiX = img.Density.Units == DensityUnit.PixelsPerCentimeter ? img.Density.X * 2.54 : img.Density.X; dpiY = img.Density.Units == DensityUnit.PixelsPerCentimeter ? img.Density.Y * 2.54 : img.Density.Y; }
                    else
                    { // 3. 真的都抓不到，強制設定為 300 (印刷常用標準)
                        dpiX = 300; dpiY = 300;
                    }
                }
                               // 2. 計算邊距像素
                int pT = MmToPx((double)numPadTop.Value, dpiY);
                int pB = MmToPx((double)numPadBottom.Value, dpiY);
                int pL = MmToPx((double)numPadLeft.Value, dpiX);
                int pR = MmToPx((double)numPadRight.Value, dpiX);

                // 修正點 1 & 2：使用 uint 轉型並明確指定寬高
                uint canvasW = (uint)(img.Width + pL + pR);
                uint canvasH = (uint)(img.Height + pT + pB);

                // 修正點 1：改用這種方式建立透明畫布，避免 byte[] 轉換錯誤
                using (var canvas = new MagickImage(MagickColors.Transparent, canvasW, canvasH))
                {
                    canvas.Density = new Density(dpiX, dpiY, DensityUnit.PixelsPerInch);

                    // 修正點 2：Composite 的坐標如果是 int，通常沒問題，但寬高建議轉型
                    canvas.Composite(img, (int)pL, (int)pT, CompositeOperator.Over);

                    var draw = new Drawables();
                    MagickColor theme = rdoWhite.Checked ? MagickColors.White :
                                        rdoBlack.Checked ? MagickColors.Black :
                                        MagickColors.DimGray;

                    // 修正點 3：long 轉 int
                    int cx = (int)(canvas.Width / 2);
                    int margin = MmToPx(0, dpiX);
                    int gap = MmToPx((double)numSafeGap.Value, dpiY);

                    draw.StrokeColor(theme).StrokeWidth((double)numLineWidth.Value);

                    // 定位線邏輯
                    int tEnd = (int)pT - gap;
                    if (tEnd > margin) draw.Line(cx, margin, cx, tEnd);

                    int bStart = (int)(pT + img.Height) + gap;
                    int bEnd = (int)canvas.Height - margin;
                    if (bEnd > bStart) draw.Line(cx, bStart, cx, bEnd);

                    // 修正點 2：Rectangle 的參數也加上 (uint) 或確保是 double/int
                    draw.StrokeWidth((double)numLineWidth.Value)
                        .FillColor(MagickColors.Transparent)
                        .Rectangle((double)margin, (double)margin, (double)(canvas.Width - margin), (double)(canvas.Height - margin));
                    if (chkAddText.Checked)
                    {
                        // 4. 繪製文字
                        //string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msjhl.ttc");
                        // 優先使用思源黑體最細版，沒有的話用微軟細體
                        //string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "SourceHanSans-ExtraLight.otf");
                        //設定一個純英文的暫存路徑 (目標)                       
                        string safeTempPath = Path.Combine(Path.GetTempPath(), "Chogokuboso Gothic.ttf");
                        string localFontPath = Path.Combine(Application.StartupPath, "Fonts", "Chogokuboso Gothic.ttf");
                        try
                        {
                           
                            File.Copy(localFontPath, safeTempPath, true);
                            localFontPath = safeTempPath;
                        }
                        catch (Exception ex)
                        {
                            
                        }
                        string fontPath = File.Exists(localFontPath) ? localFontPath : "C:\\Windows\\Fonts\\msjhl.ttc";
            

                        // 檢查檔案是否存在，如果沒有就退回標準版
                        if (!File.Exists(fontPath))
                        {
                            fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msjh.ttc");
                        }

                        // 注意：FontPointSize 直接給 Point 值即可
                        double userFontSizePt = (double)numFontSize.Value;

                        var 文字起始位置 = Gravity.South;
                        // 或者使用更簡潔的 Lambda 寫法 (推薦)：
                        this.Invoke(() => {
                            switch (comboBox1.SelectedItem + "")
                            {
                                case "左上":
                                    文字起始位置 = Gravity.Northwest;
                                    break;
                                case "中上":
                                    文字起始位置 = Gravity.North;
                                    break;
                                case "右上":
                                    文字起始位置 = Gravity.Northeast;
                                    break;
                                case "左下":
                                    文字起始位置 = Gravity.Southwest;
                                    break;
                                case "中下":
                                    文字起始位置 = Gravity.South;
                                    break;
                                case "右下":
                                    文字起始位置 = Gravity.Southeast;
                                    break;
                                default:
                                    文字起始位置 = Gravity.Southwest;
                                    break;
                            }
                        });

                        MagickColor txttheme = rdotxtWhite.Checked ? MagickColors.White :
                                        rdotxtBlack.Checked ? MagickColors.Black :
                                        MagickColors.DimGray;
                        string newText = "";

                        // 1. 建立獨立的字型集合 (不會與 Windows 內建字型衝突)
                        SixLabors.Fonts.FontCollection collection = new SixLabors.Fonts.FontCollection();
                        // 2. 載入指定的 TTF 檔案
                        var family = collection.Add(localFontPath);
                        // 3. 建立字型實體 (大小設為 12 即可，不影響字形有無的判斷)
                        var font = family.CreateFont(12);
                        // 建立測量選項
                        var options = new TextOptions(font);
                        foreach (char c in text)
                        {

                            // 實務上通常會略過空白、換行等排版字元
                            if (char.IsWhiteSpace(c))
                            {
                                continue;
                            }
                            // 將 char 轉換為 CodePoint (支援 Unicode 擴展區)
                            CodePoint codePoint = new CodePoint(c);
                            // 檢查字體內部是否包含該字形的資料
                            bool isSupported = font.TryGetGlyphs(codePoint, out _);
                            if (isSupported)
                            {
                                // 測量單一字元的實際渲染尺寸
                                FontRectangle size = TextMeasurer.MeasureSize(c.ToString(), options);
                                Console.WriteLine($"文字寬度: {size.Width}, 文字高度: {size.Height}");
                                if (size.Width == 2.578125 && size.Height == 2.578125)
                                {
                                    string pinyin = WordsHelper.GetPinyin(c.ToString());
                                    newText += pinyin;
                                }
                                else
                                {
                                    newText += c.ToString();
                                }                             
                            }
                            else
                            {
                                string pinyin = WordsHelper.GetPinyin(c.ToString());
                                newText += pinyin;
                            }                          

                        }
                        
                        draw.Font(fontPath)
                            .FontPointSize(userFontSizePt) // 讓 Magick 根據 Density 自動算
                            .FillColor(txttheme)
                            .StrokeColor(MagickColors.Transparent)
                            .StrokeWidth(0)                                
                            .TextEncoding(System.Text.Encoding.UTF8)// (選用) 設定文字編碼，通常 Magick.NET 會自動處理 UTF-8
                            .Gravity(文字起始位置) // 若改為 Northwest，y 位移請從上面算
                            .Text(margin + MmToPx((double)numTextX.Value, dpiX),
                                  margin + MmToPx((double)numTextY.Value, dpiY),
                                  newText);
                    }
                    canvas.Draw(draw);

                    // 存檔前強制設定密度
                    canvas.Settings.Compression = CompressionMethod.LZW;
                    canvas.Write(output);
                }
            }
        }
        

        private int MmToPx(double mm, double dpi) => (int)Math.Round((mm / 25.4) * dpi);
        private void Log(string m) { if (txtLog.InvokeRequired) this.Invoke(new Action(() => Log(m))); else { txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {m}\n"); txtLog.ScrollToCaret(); } }

        private void SaveConfig()
        {
            var s = new { S = txtSourceDir.Text, O = txtOutputDir.Text, T = numPadTop.Value, B = numPadBottom.Value, L = numPadLeft.Value, R = numPadRight.Value, X = numTextX.Value, Y = numTextY.Value, W = rdoWhite.Checked, Bl = rdoBlack.Checked, TW = rdotxtWhite.Checked, TBl = rdotxtBlack.Checked, FS = numFontSize.Value, Gap = numSafeGap.Value, LW= numLineWidth.Value };
            File.WriteAllText(configPath, JsonSerializer.Serialize(s));
        }

        private void LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                int paddingdef = 5;
                numPadTop.Value = paddingdef;
                numPadTop.Text = paddingdef.ToString(); // 強制刷新畫面顯示
                numPadBottom.Value = paddingdef;
                numPadBottom.Text = paddingdef.ToString(); // 強制刷新畫面顯示
                numPadLeft.Value = paddingdef;
                numPadLeft.Text = paddingdef.ToString(); // 強制刷新畫面顯示
                numPadRight.Value = paddingdef;
                numPadRight.Text = paddingdef.ToString(); // 強制刷新畫面顯示

                numLineWidth.Value = 1;
                numLineWidth.Text = 1.ToString();

                numTextX.Value = 0;
                numTextX.Text = 0.ToString(); // 強制刷新畫面顯示
                numTextY.Value = 0;
                numTextY.Text = 0.ToString(); // 強制刷新畫面顯示


                numFontSize.Value = 12;
                numFontSize.Text = 12.ToString(); // 強制刷新畫面顯示

                numSafeGap.Value = 2;
                numSafeGap.Text = 2.ToString(); // 強制刷新畫面顯示

                return; 
            }

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(configPath)))
                {
                    var r = doc.RootElement;

                    // 1. 路徑載入
                    txtSourceDir.Text = r.TryGetProperty("S", out var s) ? s.GetString() : "";
                    txtOutputDir.Text = r.TryGetProperty("O", out var o) ? o.GetString() : "";

                    // 2. 數值載入 (使用自定義方法確保 Value 與 Text 同步)
                    SetNumericValue(numPadTop, "T", r);
                    SetNumericValue(numPadBottom, "B", r);
                    SetNumericValue(numPadLeft, "L", r);
                    SetNumericValue(numPadRight, "R", r);
                    SetNumericValue(numLineWidth, "LW", r);
                    SetNumericValue(numTextX, "X", r);
                    SetNumericValue(numTextY, "Y", r);
                    SetNumericValue(numFontSize, "FS", r, 12m); // 預設 12pt
                    SetNumericValue(numSafeGap, "Gap", r, 2m); // 預設 2mm

                    // 3. 狀態載入
                    rdoWhite.Checked = r.TryGetProperty("W", out var w) && w.GetBoolean();
                    rdoBlack.Checked = r.TryGetProperty("Bl", out var bl) && bl.GetBoolean(); 

                    rdotxtWhite.Checked = r.TryGetProperty("TW", out var tw) && tw.GetBoolean();
                    rdotxtBlack.Checked = r.TryGetProperty("TBl", out var tbl) && tbl.GetBoolean();
                }
            }
            catch (Exception ex)
            {
                Log($"設定載入失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 統一設定 NumericUpDown 的值，並強制刷新 Text 屬性解決顯示問題
        /// </summary>
        private void SetNumericValue(NumericUpDown nm, string propName, JsonElement root, decimal defaultValue = 0)
        {
            try
            {
                decimal val = defaultValue;
                if (root.TryGetProperty(propName, out var prop))
                {
                    val = prop.GetDecimal();
                }

                // 確保數值在控制項允許的範圍內
                decimal clampedValue = Math.Clamp(val, nm.Minimum, nm.Maximum);

                nm.Value = clampedValue;
                nm.Text = clampedValue.ToString(); // 強制刷新畫面顯示
            }
            catch { }
        }

    }
}