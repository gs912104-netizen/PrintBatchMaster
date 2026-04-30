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

        // 定位線水平對齊基準：以「原始圖」的左/中/右為基準，不含 padding 邊框
        private enum LineHAlign { Left, Center, Right }

        // 處理參數打包：避免在背景執行緒內反覆 Invoke 讀 UI，並讓 ProcessImage 變成純函式
        private class ProcessOptions
        {
            public int PadTopMm, PadBottomMm, PadLeftMm, PadRightMm;
            public double LineWidth;
            public double SafeGapMm;
            public double FontSizePt;
            public double TextXMm, TextYMm;
            public bool AddText;
            public MagickColor LineColor = MagickColors.DimGray;
            public MagickColor TextColor = MagickColors.DimGray;
            public Gravity TextGravity = Gravity.Southwest;
            public LineHAlign LinePos = LineHAlign.Center;
        }

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

            // 定位線水平位置（相對於「原始圖」的左/中/右，不含 padding）
            cmbLinePos.Items.Add("置中");
            cmbLinePos.Items.Add("靠左");
            cmbLinePos.Items.Add("靠右");
            cmbLinePos.SelectedIndex = 0;
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

        /// <summary>
        /// 在 UI 執行緒上一次性把所有設定讀出來，背景執行緒就不必再 Invoke 取值。
        /// </summary>
        private ProcessOptions ReadUiOptions()
        {
            var opt = new ProcessOptions
            {
                PadTopMm = (int)numPadTop.Value,
                PadBottomMm = (int)numPadBottom.Value,
                PadLeftMm = (int)numPadLeft.Value,
                PadRightMm = (int)numPadRight.Value,
                LineWidth = (double)numLineWidth.Value,
                SafeGapMm = (double)numSafeGap.Value,
                FontSizePt = (double)numFontSize.Value,
                TextXMm = (double)numTextX.Value,
                TextYMm = (double)numTextY.Value,
                AddText = chkAddText.Checked,
                LineColor = rdoWhite.Checked ? MagickColors.White :
                            rdoBlack.Checked ? MagickColors.Black :
                            MagickColors.DimGray,
                TextColor = rdotxtWhite.Checked ? MagickColors.White :
                            rdotxtBlack.Checked ? MagickColors.Black :
                            MagickColors.DimGray,
                TextGravity = (comboBox1.SelectedItem + "") switch
                {
                    "左上" => Gravity.Northwest,
                    "中上" => Gravity.North,
                    "右上" => Gravity.Northeast,
                    "左下" => Gravity.Southwest,
                    "中下" => Gravity.South,
                    "右下" => Gravity.Southeast,
                    _ => Gravity.Southwest
                },
                LinePos = (cmbLinePos.SelectedItem + "") switch
                {
                    "靠左" => LineHAlign.Left,
                    "靠右" => LineHAlign.Right,
                    _ => LineHAlign.Center
                }
            };
            return opt;
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

            // 預先把 UI 狀態抓到區域變數，迴圈內就不必每張圖都 Invoke
            ProcessOptions opt = ReadUiOptions();

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
                        ProcessImage(f, outPath, label, opt);

                        // 每張大圖處理完強制釋放 LOH 上的像素緩衝，避免連續批次累積到 OOM
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        Log($"[OK] {counter:D3} - {newFileName}");
                        counter++;
                    }
                    catch (OutOfMemoryException oom)
                    {
                        Log($"[OOM] {Path.GetFileName(f)} 記憶體不足，已跳過。建議降低圖片尺寸或關閉其他程式。({oom.Message})");
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                    }
                    catch (Exception ex) { Log($"[ERR] {Path.GetFileName(f)}: {ex.Message}"); }
                    finally { this.Invoke(new Action(() => progressBar1.Value++)); }
                }
            });
            Log(">>> 完成！");
            btnStart.Enabled = true;
        }

        private void ProcessImage(string input, string output, string text, ProcessOptions opt)
        {
            // === DPI 解析：用 MagickImageInfo 只讀檔案 header，不解壓像素 ===
            // 比起 System.Drawing.Image.FromFile(會把整張 TIFF 解到記憶體) 大幅降低尖峰用量
            double dpiX = 0, dpiY = 0;
            try
            {
                var info = new MagickImageInfo(input);
                if (info.Density != null && info.Density.X > 0)
                {
                    dpiX = info.Density.Units == DensityUnit.PixelsPerCentimeter ? info.Density.X * 2.54 : info.Density.X;
                    dpiY = info.Density.Units == DensityUnit.PixelsPerCentimeter ? info.Density.Y * 2.54 : info.Density.Y;
                }
            }
            catch { /* 解析失敗就走 fallback */ }
            if (dpiX <= 0) { dpiX = 300; dpiY = 300; }

            // === 讀檔讀取設定：TIFF 強制只讀第一頁，並告知預期 Density 讓 Magick 不要再猜 ===
            var readSettings = new MagickReadSettings
            {
                Density = new Density(dpiX, dpiY, DensityUnit.PixelsPerInch),
                FrameIndex = 0,
                FrameCount = 1
            };

            // === 計算邊距像素（不需 img 也能算）===
            int pT = MmToPx(opt.PadTopMm, dpiY);
            int pB = MmToPx(opt.PadBottomMm, dpiY);
            int pL = MmToPx(opt.PadLeftMm, dpiX);
            int pR = MmToPx(opt.PadRightMm, dpiX);

            // 先用 MagickImageInfo 只讀 header 取得寬高（不解壓像素），決定 canvas 尺寸
            int imgW, imgH;
            try
            {
                var info2 = new MagickImageInfo(input);
                imgW = (int)info2.Width;
                imgH = (int)info2.Height;
            }
            catch
            {
                // 萬一某些 TIFF header 讀不到尺寸，再 fallback 完整載入一次
                using (var probe = new MagickImage(input, readSettings))
                {
                    imgW = (int)probe.Width;
                    imgH = (int)probe.Height;
                }
            }

            uint canvasW = (uint)(imgW + pL + pR);
            uint canvasH = (uint)(imgH + pT + pB);

            // === 建 canvas（透明），記憶體裡只有「canvas」一份大像素緩衝 ===
            using (var canvas = new MagickImage(MagickColors.Transparent, canvasW, canvasH))
            {
                canvas.Density = new Density(dpiX, dpiY, DensityUnit.PixelsPerInch);

                // 巢狀 using：原圖在 Composite 完成後立刻 Dispose，記憶體只在這個區段內短暫佔兩份
                using (var img = new MagickImage(input, readSettings))
                {
                    canvas.Composite(img, (int)pL, (int)pT, CompositeOperator.Over);
                } // img 立刻釋放，後續繪製只剩 canvas 在記憶體

                // === 繪製定位線與外框 ===
                var draw = new Drawables();

                // 定位線水平位置：以「原始圖」的左/中/右為基準（不含 padding 邊框）
                // - 靠左：原圖左邊緣，x = pL
                // - 置中：原圖正中央，x = pL + imgW / 2
                // - 靠右：原圖右邊緣，x = pL + imgW
                int cx = opt.LinePos switch
                {
                    LineHAlign.Left => pL,
                    LineHAlign.Right => pL + imgW,
                    _ => pL + imgW / 2
                };
                int margin = MmToPx(0, dpiX);
                int gap = MmToPx(opt.SafeGapMm, dpiY);

                draw.StrokeColor(opt.LineColor).StrokeWidth(opt.LineWidth);

                int tEnd = pT - gap;
                if (tEnd > margin) draw.Line(cx, margin, cx, tEnd);

                int bStart = pT + imgH + gap;
                int bEnd = (int)canvas.Height - margin;
                if (bEnd > bStart) draw.Line(cx, bStart, cx, bEnd);

                draw.StrokeWidth(opt.LineWidth)
                    .FillColor(MagickColors.Transparent)
                    .Rectangle((double)margin, (double)margin, (double)((int)canvas.Width - margin), (double)((int)canvas.Height - margin));

                if (opt.AddText)
                {
                    // 字型：優先用內附超極細黑體；先複製到純英文 Temp 路徑避免中文路徑造成 Magick 失敗
                    string safeTempPath = Path.Combine(Path.GetTempPath(), "Chogokuboso Gothic.ttf");
                    string localFontPath = Path.Combine(Application.StartupPath, "Fonts", "Chogokuboso Gothic.ttf");
                    try
                    {
                        if (File.Exists(localFontPath))
                        {
                            File.Copy(localFontPath, safeTempPath, true);
                            localFontPath = safeTempPath;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[Font] 複製字型失敗：{ex.Message}");
                    }
                    string fontPath = File.Exists(localFontPath) ? localFontPath : "C:\\Windows\\Fonts\\msjhl.ttc";
                    if (!File.Exists(fontPath))
                    {
                        fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msjh.ttc");
                    }

                    // 用 SixLabors.Fonts 偵測字形是否存在，沒有的字改成拼音輸出避免「豆腐字」
                    string newText = ConvertUnsupportedToPinyin(text, localFontPath);

                    draw.Font(fontPath)
                        .FontPointSize(opt.FontSizePt)
                        .FillColor(opt.TextColor)
                        .StrokeColor(MagickColors.Transparent)
                        .StrokeWidth(0)
                        .TextEncoding(System.Text.Encoding.UTF8)
                        .Gravity(opt.TextGravity)
                        .Text(margin + MmToPx(opt.TextXMm, dpiX),
                              margin + MmToPx(opt.TextYMm, dpiY),
                              newText);
                }

                canvas.Draw(draw);

                // 存檔前強制設定密度與壓縮
                canvas.Settings.Compression = CompressionMethod.LZW;
                canvas.Write(output);
            }
        }

        /// <summary>
        /// 把字型不支援的字元轉成拼音（避免印出豆腐字 □）。
        /// </summary>
        private string ConvertUnsupportedToPinyin(string text, string fontFilePath)
        {
            if (!File.Exists(fontFilePath)) return text; // 字型找不到就原樣輸出

            var collection = new SixLabors.Fonts.FontCollection();
            var family = collection.Add(fontFilePath);
            var font = family.CreateFont(12);
            var options = new TextOptions(font);

            var sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c)) continue;

                CodePoint codePoint = new CodePoint(c);
                bool isSupported = font.TryGetGlyphs(codePoint, out _);
                if (isSupported)
                {
                    FontRectangle size = TextMeasurer.MeasureSize(c.ToString(), options);
                    if (size.Width == 2.578125 && size.Height == 2.578125)
                    {
                        // 缺字符 fallback 尺寸 → 用拼音
                        sb.Append(WordsHelper.GetPinyin(c.ToString()));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    sb.Append(WordsHelper.GetPinyin(c.ToString()));
                }
            }
            return sb.ToString();
        }

        private int MmToPx(double mm, double dpi) => (int)Math.Round((mm / 25.4) * dpi);
        private void Log(string m) { if (txtLog.InvokeRequired) this.Invoke(new Action(() => Log(m))); else { txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {m}\n"); txtLog.ScrollToCaret(); } }

        private void SaveConfig()
        {
            var s = new { S = txtSourceDir.Text, O = txtOutputDir.Text, T = numPadTop.Value, B = numPadBottom.Value, L = numPadLeft.Value, R = numPadRight.Value, X = numTextX.Value, Y = numTextY.Value, W = rdoWhite.Checked, Bl = rdoBlack.Checked, TW = rdotxtWhite.Checked, TBl = rdotxtBlack.Checked, FS = numFontSize.Value, Gap = numSafeGap.Value, LW= numLineWidth.Value, LP = cmbLinePos.SelectedIndex, TP = comboBox1.SelectedIndex, AT = chkAddText.Checked };
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

                    // 4. 定位線水平位置
                    if (r.TryGetProperty("LP", out var lp) && lp.TryGetInt32(out int lpIdx)
                        && lpIdx >= 0 && lpIdx < cmbLinePos.Items.Count)
                    {
                        cmbLinePos.SelectedIndex = lpIdx;
                    }

                    // 5. 文字起始位置
                    if (r.TryGetProperty("TP", out var tp) && tp.TryGetInt32(out int tpIdx)
                        && tpIdx >= 0 && tpIdx < comboBox1.Items.Count)
                    {
                        comboBox1.SelectedIndex = tpIdx;
                    }

                    // 6. 是否加字
                    if (r.TryGetProperty("AT", out var at)) chkAddText.Checked = at.GetBoolean();
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
