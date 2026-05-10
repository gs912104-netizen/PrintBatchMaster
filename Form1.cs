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

        // 跨 ProcessImage 記憶「已 log 過的 DPI」，避免每張圖都重複 log；StartProcessing 會清空
        private HashSet<int> _loggedDpis = new HashSet<int>();

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
            // 文字水平對齊（在外框下方延伸區內）：靠左 / 置中 / 靠右
            public LineHAlign TextAlign = LineHAlign.Left;
            // 文字下推距離（mm）：從圖片區底部往下推多少 mm 才放文字
            public double TextOffsetMm;
            public LineHAlign LinePos = LineHAlign.Center;
            // 批次開始時預先準備好的內附字型路徑（複製到 Temp 純英文路徑後）；不加字時為 null
            public string? FontPath;
        }

        public Form1()
        {
            InitializeComponent();
            BindEvents();
            this.Load += (s, e) => LoadConfig();
            // 文字水平對齊（文字會被放在外框下方延伸區，永遠在框外）
            comboBox1.Items.Add("靠左");
            comboBox1.Items.Add("置中");
            comboBox1.Items.Add("靠右");
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

            // 顏色選擇器：開 ColorDialog → 寫回 TextBox(#RRGGBB) + 更新色塊
            btnPickLineColor.Click += (s, e) => PickColor(txtLineColor, pnlLineColor);
            btnPickTextColor.Click += (s, e) => PickColor(txtTextColor, pnlTextColor);

            // TextBox 直接打字 → 嘗試解析 hex 即時刷新色塊
            txtLineColor.TextChanged += (s, e) => RefreshColorPanel(txtLineColor, pnlLineColor);
            txtTextColor.TextChanged += (s, e) => RefreshColorPanel(txtTextColor, pnlTextColor);
        }

        private void PickColor(TextBox tb, Panel preview)
        {
            using (var cd = new ColorDialog { FullOpen = true, AnyColor = true })
            {
                // 預設值用目前 TextBox 內的色碼
                if (TryParseHex(tb.Text, out var current))
                {
                    cd.Color = current;
                }
                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    tb.Text = $"#{cd.Color.R:X2}{cd.Color.G:X2}{cd.Color.B:X2}";
                    preview.BackColor = cd.Color;
                }
            }
        }

        private void RefreshColorPanel(TextBox tb, Panel preview)
        {
            if (TryParseHex(tb.Text, out var c)) preview.BackColor = c;
        }

        /// <summary>
        /// 嘗試把 #RRGGBB / RRGGBB / #RGB / 標準色名 (e.g. "Red") 解析成 System.Drawing.Color。
        /// </summary>
        private bool TryParseHex(string text, out System.Drawing.Color color)
        {
            color = System.Drawing.Color.DimGray;
            if (string.IsNullOrWhiteSpace(text)) return false;
            try
            {
                string s = text.Trim();
                if (!s.StartsWith("#") && System.Text.RegularExpressions.Regex.IsMatch(s, "^[0-9A-Fa-f]{3,8}$"))
                {
                    s = "#" + s;
                }
                color = System.Drawing.ColorTranslator.FromHtml(s);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// 把 TextBox 的色碼字串轉成 ImageMagick 用的 MagickColor；無法解析時回傳 fallback。
        /// 用 hex 字串建構子，Q8/Q16 版皆相容。
        /// </summary>
        private MagickColor ParseMagickColor(string text, MagickColor fallback)
        {
            if (TryParseHex(text, out var c))
            {
                try
                {
                    return new MagickColor($"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}");
                }
                catch
                {
                    return fallback;
                }
            }
            return fallback;
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
                LineColor = ParseMagickColor(txtLineColor.Text, MagickColors.DimGray),
                TextColor = ParseMagickColor(txtTextColor.Text, MagickColors.DimGray),
                TextAlign = (comboBox1.SelectedItem + "") switch
                {
                    "靠左" => LineHAlign.Left,
                    "置中" => LineHAlign.Center,
                    "靠右" => LineHAlign.Right,
                    _ => LineHAlign.Left
                },
                TextOffsetMm = (double)numTextOffset.Value,
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

            // 強制用內附字型：批次開始前先檢查並複製一次，找不到就整批中止（避免每張圖都失敗）
            if (opt.AddText)
            {
                try
                {
                    opt.FontPath = EnsureBundledFont("Chogokuboso Gothic.ttf");
                }
                catch (FileNotFoundException ex)
                {
                    Log($"[FATAL] {ex.Message}");
                    MessageBox.Show(ex.Message, "字型錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnStart.Enabled = true;
                    return;
                }
            }

            // 線寬單位是 mm，每個 DPI 第一次出現時 log 一次「實際輸出 px、實際 mm、誤差」
            _loggedDpis.Clear();

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

            // 圖片區（含 padding）的底部 Y 座標。外框只佔此區域；文字區在這之下延伸
            int imageBoxBottom = imgH + pT + pB;

            // 文字區高度計算：使用者填的「下推 mm」+ 字級高度 + 安全裕度
            int textOffsetPx = MmToPx(opt.TextOffsetMm, dpiY);
            // 提早算 textOffsetX/Y（使用者額外的 X/Y 微調偏移），canvas 高度也要納入 Y 偏移
            int textOffsetX = MmToPx(opt.TextXMm, dpiX);
            int textOffsetY = MmToPx(opt.TextYMm, dpiY);

            // 用 ImageMagick 的 FontTypeMetrics 精確查 ascent/descent，避免用粗略估算
            // ascent = 文字基線到頂端的距離；descent = 基線到下伸字底的距離
            double fontPxF = opt.FontSizePt / 72.0 * dpiY;
            double ascentPx = fontPxF * 0.85;   // 預設估算（拿不到 metrics 時用）
            double descentPx = fontPxF * 0.20;
            if (opt.AddText && !string.IsNullOrEmpty(opt.FontPath))
            {
                try
                {
                    using (var dummy = new MagickImage(MagickColors.Transparent, 1, 1))
                    {
                        dummy.Settings.Font = opt.FontPath;
                        dummy.Settings.FontPointsize = opt.FontSizePt;
                        // 量「Mjyg」這種同時有上伸字 (M) 跟下伸字 (j/y/g) 的字串能拿到完整 ascent/descent
                        var metrics = dummy.FontTypeMetrics("Mjyg");
                        if (metrics != null)
                        {
                            ascentPx = metrics.Ascent;
                            descentPx = Math.Abs(metrics.Descent);
                        }
                    }
                }
                catch { /* metrics 拿不到就用預設估算 */ }
            }
            int fontHeightPx = (int)Math.Ceiling(ascentPx + descentPx);
            // 文字區高度 = 下推距離 + 使用者 Y 偏移 + 字級高度
            // textOffsetY 為負時（向上偏）取 0，canvas 不縮，避免文字裁切（圖片區會被覆蓋是用戶選擇）
            int textAreaH = opt.AddText
                ? Math.Max(0, textOffsetPx + textOffsetY) + fontHeightPx
                : 0;

            uint canvasW = (uint)(imgW + pL + pR);
            uint canvasH = (uint)(imageBoxBottom + textAreaH);

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
                // 設計：完全繞過 ImageMagick 的 Drawables（path/stroke/fill 系統），
                // 改用 Composite 把「純色 N×M 像素塊」直接貼到 canvas 上。
                // 為什麼這樣才對稱：
                //   1. Drawables 的 stroke 會 anti-alias，亞像素 stroke 在印刷上會變淡或消失。
                //   2. 即使用 fill rectangle，path-based 渲染對「邊界 0 起算」vs「貼到 W/H」處理不對稱，
                //      左上會多 1 像素的淡邊，視覺上比右下厚。
                //   3. Composite 是整塊像素覆蓋，沒有 path、沒有 anti-alias、沒有 stroke。
                //      四邊都是「精確 N×M 整數像素的純色」，絕對對稱。

                int W = (int)canvas.Width;
                // 注意：外框跟定位線只繪製在「圖片區」(高度 = imageBoxBottom)；
                //       canvas 下半部的文字區留給文字使用，不畫外框與定位線。
                int H = imageBoxBottom;

                // 線寬：使用者輸入單位是 px（直接的像素數），round 到整數最小 1。
                // 1 px = 最細的單像素線，無論 DPI 高低視覺上都是最細的可見線。
                // 例：使用者填 0.1 ~ 0.5 都會 round 到 1px；填 1 是 1px；填 2.5 round 到 3px。
                int strokePx = Math.Max(1, (int)Math.Round(opt.LineWidth));
                int leftHalf = strokePx / 2;        // 線中心左/上半（floor）
                int rightHalf = strokePx - leftHalf; // 線中心右/下半（ceil），保持總寬 = strokePx

                // 每個 DPI 第一次出現時 log 一次：顯示像素線在該 DPI 下印出的物理尺寸
                int dpiKey = (int)Math.Round(dpiX);
                bool firstSeenDpi;
                lock (_loggedDpis) { firstSeenDpi = _loggedDpis.Add(dpiKey); }
                if (firstSeenDpi)
                {
                    double actualMm = strokePx * 25.4 / dpiX;
                    Log($"[線寬] 設定 {strokePx}px，{dpiKey} DPI 圖印出物理寬度 ≈ {actualMm:0.000}mm");
                }

                // 定位線水平位置：以「原始圖」的左/中/右為基準（不含 padding 邊框）
                int cx = opt.LinePos switch
                {
                    LineHAlign.Left => pL,
                    LineHAlign.Right => pL + imgW,
                    _ => pL + imgW / 2
                };
                // margin = 外框與畫布邊緣的內縮距離（mm）。
                // 目前 UI 沒有對應控制項，所以固定 0；保留此變數讓未來易於擴充
                // （想加「外框內縮 X mm」時，把 MmToPx 的第一個參數改成 opt.MarginMm 即可）
                int margin = MmToPx(0, dpiX);
                int gap = MmToPx(opt.SafeGapMm, dpiY);

                // === 改用 Composite 貼純色矩形畫線 ===
                // 為什麼不用 Drawables.Rectangle：
                //   ImageMagick 的 path-based Rectangle 在邊界 anti-alias 行為不對稱
                //   （左上邊 0 起算 vs 右下邊貼到 W/H 邊緣）→ 左上會比右下視覺上厚一點。
                // 改用 new MagickImage(顏色, w, h) + canvas.Composite() 直接貼純色塊：
                //   每塊都是精確 N×M 整數像素的純色，沒有 path/anti-alias/stroke，四邊絕對對稱。

                // 把外框內縮邊距移到區塊外圍變數
                int boxLeft = margin;
                int boxTop = margin;
                int boxW = W - 2 * margin;
                int boxH = H - 2 * margin;

                // === 外框：上下左右四條純色矩形 ===
                // 上邊 (boxLeft, boxTop) 貼一塊 boxW × strokePx
                PasteSolidRect(canvas, opt.LineColor, boxLeft, boxTop, boxW, strokePx);
                // 下邊 (boxLeft, boxTop + boxH - strokePx) 貼一塊 boxW × strokePx
                PasteSolidRect(canvas, opt.LineColor, boxLeft, boxTop + boxH - strokePx, boxW, strokePx);
                // 左邊 (boxLeft, boxTop) 貼一塊 strokePx × boxH
                PasteSolidRect(canvas, opt.LineColor, boxLeft, boxTop, strokePx, boxH);
                // 右邊 (boxLeft + boxW - strokePx, boxTop) 貼一塊 strokePx × boxH
                PasteSolidRect(canvas, opt.LineColor, boxLeft + boxW - strokePx, boxTop, strokePx, boxH);

                // === 上方定位線（從 margin 到 pT - gap，水平中心對齊 cx）===
                int tLineEnd = pT - gap;
                int tLineHeight = tLineEnd - margin;
                if (tLineHeight > 0)
                {
                    PasteSolidRect(canvas, opt.LineColor, cx - leftHalf, margin, strokePx, tLineHeight);
                }

                // === 下方定位線（從 pT + imgH + gap 到 H - margin）===
                int bLineStart = pT + imgH + gap;
                int bLineHeight = (H - margin) - bLineStart;
                if (bLineHeight > 0)
                {
                    PasteSolidRect(canvas, opt.LineColor, cx - leftHalf, bLineStart, strokePx, bLineHeight);
                }

                if (opt.AddText && !string.IsNullOrEmpty(opt.FontPath))
                {
                    // 字型路徑由 StartProcessing 開始前預先準備好（已複製到 Temp 純英文路徑）
                    string fontPath = opt.FontPath;

                    // 用同一個 fontPath 偵測字形是否存在，沒有的字改成拼音輸出避免「豆腐字」
                    string newText = ConvertUnsupportedToPinyin(text, fontPath);

                    // === 文字繪製：用絕對座標，不依賴 Drawables.Gravity ===
                    // Magick.NET 14 的 Drawables.Gravity 對 Text(x, y) 的座標重映射在某些版本下會有問題
                    // （文字被算到 canvas 外導致整個消失），改用絕對座標最穩。
                    //
                    // 步驟：
                    //   1. 用 canvas.FontTypeMetrics 精確量出文字實際渲染的寬度（含 kerning）
                    //   2. 依 TextAlign 算出文字左邊起點 textX
                    //   3. baselineY = 期望文字頂端 Y + ascent

                    // 量文字寬度（canvas 已有正確 density 跟字型設定）
                    canvas.Settings.Font = fontPath;
                    canvas.Settings.FontPointsize = opt.FontSizePt;
                    var textMetrics = canvas.FontTypeMetrics(newText);
                    double textWidthPx = textMetrics != null
                        ? textMetrics.TextWidth 
                        : (newText.Length * opt.FontSizePt * 0.6);

                    // ImageMagick 的 Drawables.Text(x, y) 中 y 是 baseline。
                    // 用 metrics.Ascent 直接補償：實際字型最高字符頂端會在 baseline - ascent 位置，
                    // 也就是 = desiredTextTopY = imageBoxBottom + textOffsetPx + textOffsetY。
                    // 這樣「下推 0」時，文字頂端「精確緊貼外框下方框線」（不會凸進外框內）。
                    int desiredTextTopY = imageBoxBottom + textOffsetPx + textOffsetY;
                    int baselineY = desiredTextTopY + (int)Math.Ceiling(ascentPx);

                    // 自己算 textX：不用 Gravity，直接絕對座標
                    int textWidthInt = (int)Math.Ceiling(textWidthPx);
                    int textX;
                    switch (opt.TextAlign)
                    {
                        case LineHAlign.Right:
                            // 靠右：文字右邊在 (W - margin) 位置，正值 textOffsetX 代表「再向左偏」
                            textX = W - margin - textWidthInt - textOffsetX;
                            break;
                        case LineHAlign.Center:
                            // 置中：文字水平中央在 W/2，左邊在 (W - textWidth) / 2，正值 textOffsetX 向右偏
                            textX = (W - textWidthInt) / 2 + textOffsetX;
                            break;
                        default: // Left
                            // 靠左：文字左邊在 margin 位置，正值 textOffsetX 向右偏
                            textX = margin + textOffsetX;
                            break;
                    }

                    // 詳細 debug log，方便追位置問題
                    int extraH = (int)canvasH - imageBoxBottom;
                    int textTopOffsetFromFrame = textOffsetPx + textOffsetY; // 文字頂離外框底的實際距離
                    Log($"[尺寸] 原圖+padding={canvasW}x{imageBoxBottom} → canvas={canvasW}x{canvasH} (擴+{extraH}px) | 外框底Y={imageBoxBottom}");
                    Log($"[文字] '{newText}' 寬{textWidthInt}px | 文字頂Y={desiredTextTopY} (離外框底 +{textTopOffsetFromFrame}px) | baseline={baselineY} | ascent={ascentPx:0.0} descent={descentPx:0.0} | 起點X={textX}");

                    var draw = new Drawables();
                    draw.Font(fontPath)
                        .FontPointSize(opt.FontSizePt)
                        .FillColor(opt.TextColor)
                        .StrokeColor(MagickColors.Transparent)
                        .StrokeWidth(0)
                        .TextEncoding(System.Text.Encoding.UTF8)
                        .Text(textX, baselineY, newText);

                    canvas.Draw(draw);
                }

                // 存檔前強制設定密度與壓縮
                canvas.Settings.Compression = CompressionMethod.LZW;
                canvas.Write(output);
            }
        }

        /// <summary>
        /// 確保內附字型可被 ImageMagick 讀取：
        ///   1. 從 Application.StartupPath\Fonts\{fontFileName} 找
        ///   2. 複製到使用者 Temp 目錄底下純英文路徑（避免中文 startup path 造成 Magick 載入失敗）
        ///   3. 找不到原檔就 throw，讓上層跳過該圖並 log 錯誤，避免靜默 fallback 到系統字型
        ///      （這是使用者明確要求：強制用內附字型，避免跨環境字型不一致）
        /// </summary>
        private string EnsureBundledFont(string fontFileName)
        {
            string sourcePath = Path.Combine(Application.StartupPath, "Fonts", fontFileName);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    $"找不到內附字型 '{fontFileName}'。預期路徑：{sourcePath}。請確認 Fonts/ 資料夾已隨程式打包。",
                    sourcePath);
            }

            // 複製到 Temp（純英文 + 無空格路徑），避免 ImageMagick 在中文/含空格路徑下讀字型失敗
            // （Magick.NET 14 在某些版本對含空格的字型路徑沒自動 quote，會靜默載入失敗）
            string ext = Path.GetExtension(fontFileName);
            string nameOnly = Path.GetFileNameWithoutExtension(fontFileName).Replace(" ", "");
            string safeTempPath = Path.Combine(Path.GetTempPath(), nameOnly + ext);
            try
            {
                File.Copy(sourcePath, safeTempPath, overwrite: true);
                return safeTempPath;
            }
            catch (Exception ex)
            {
                // 複製失敗（例如 Temp 鎖住）就直接用原路徑試試，至少在純英文 startup path 下能成功
                Log($"[Font] 字型複製到 Temp 失敗，改用原路徑：{ex.Message}");
                return sourcePath;
            }
        }

        /// <summary>
        /// 在 canvas 上 (x, y) 位置貼一塊 width × height 的純色矩形。
        /// 用 Composite 取代 Drawables.Rectangle，避免 path/anti-alias 在邊界導致左上比右下厚的不對稱問題。
        /// 自動 clamp 邊界、忽略尺寸 ≤ 0 的呼叫。
        /// </summary>
        private void PasteSolidRect(MagickImage canvas, MagickColor color, int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0) return;

            int cw = (int)canvas.Width;
            int ch = (int)canvas.Height;

            // clamp 座標與尺寸到 canvas 範圍內，避免 Composite 失敗或溢出
            if (x < 0) { width += x; x = 0; }
            if (y < 0) { height += y; y = 0; }
            if (x + width > cw) width = cw - x;
            if (y + height > ch) height = ch - y;

            if (width <= 0 || height <= 0) return;

            using (var rect = new MagickImage(color, (uint)width, (uint)height))
            {
                canvas.Composite(rect, x, y, CompositeOperator.Over);
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
            // 新版欄位：LC/TC=色碼, LP=定位線位置, TA=文字對齊(取代舊 TP), TO=文字下推mm
            var s = new { S = txtSourceDir.Text, O = txtOutputDir.Text, T = numPadTop.Value, B = numPadBottom.Value, L = numPadLeft.Value, R = numPadRight.Value, X = numTextX.Value, Y = numTextY.Value, LC = txtLineColor.Text, TC = txtTextColor.Text, FS = numFontSize.Value, Gap = numSafeGap.Value, LW = numLineWidth.Value, LP = cmbLinePos.SelectedIndex, TA = comboBox1.SelectedIndex, TO = numTextOffset.Value, AT = chkAddText.Checked };
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

                numTextOffset.Value = 5;
                numTextOffset.Text = 5.ToString(); // 文字下推預設 5mm

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

                    // 3. 顏色載入：優先讀新版 LC/TC 色碼字串；
                    //    若沒有就 fallback 到舊版 W/Bl/TW/TBl 三選一狀態（向後相容舊設定檔）
                    string lineHex = r.TryGetProperty("LC", out var lc) ? (lc.GetString() ?? "") : "";
                    string textHex = r.TryGetProperty("TC", out var tc) ? (tc.GetString() ?? "") : "";
                    if (string.IsNullOrWhiteSpace(lineHex))
                    {
                        bool oldWhite = r.TryGetProperty("W", out var w) && w.GetBoolean();
                        bool oldBlack = r.TryGetProperty("Bl", out var bl) && bl.GetBoolean();
                        lineHex = oldWhite ? "#FFFFFF" : oldBlack ? "#000000" : "#696969";
                    }
                    if (string.IsNullOrWhiteSpace(textHex))
                    {
                        bool oldTW = r.TryGetProperty("TW", out var tw) && tw.GetBoolean();
                        bool oldTBl = r.TryGetProperty("TBl", out var tbl) && tbl.GetBoolean();
                        textHex = oldTW ? "#FFFFFF" : oldTBl ? "#000000" : "#696969";
                    }
                    txtLineColor.Text = lineHex;
                    txtTextColor.Text = textHex;
                    // TextChanged 事件會自動刷新 pnlLineColor / pnlTextColor 的 BackColor

                    // 4. 定位線水平位置
                    if (r.TryGetProperty("LP", out var lp) && lp.TryGetInt32(out int lpIdx)
                        && lpIdx >= 0 && lpIdx < cmbLinePos.Items.Count)
                    {
                        cmbLinePos.SelectedIndex = lpIdx;
                    }

                    // 5. 文字水平對齊：優先讀新版 TA (0~2)；沒有再讀舊版 TP (0~5) 做 mapping
                    if (r.TryGetProperty("TA", out var ta) && ta.TryGetInt32(out int taIdx)
                        && taIdx >= 0 && taIdx < comboBox1.Items.Count)
                    {
                        comboBox1.SelectedIndex = taIdx;
                    }
                    else if (r.TryGetProperty("TP", out var tp) && tp.TryGetInt32(out int tpIdx))
                    {
                        // 舊版 6 選項: 0=左下 1=左上 2=中下 3=中上 4=右下 5=右上
                        // 新版 3 選項: 0=靠左   1=置中     2=靠右
                        int mapped = tpIdx switch
                        {
                            0 or 1 => 0, // 左 → 靠左
                            2 or 3 => 1, // 中 → 置中
                            4 or 5 => 2, // 右 → 靠右
                            _ => 0
                        };
                        if (mapped < comboBox1.Items.Count) comboBox1.SelectedIndex = mapped;
                    }

                    // 6. 文字下推距離 (mm)
                    SetNumericValue(numTextOffset, "TO", r, 5m); // 預設 5mm

                    // 7. 是否加字
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
