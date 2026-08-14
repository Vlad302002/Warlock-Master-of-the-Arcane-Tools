using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace WarlockTools
{
    public sealed class MainForm : Form
    {
        static readonly Color Bg = Color.FromArgb(32, 34, 40);
        static readonly Color PanelBg = Color.FromArgb(42, 45, 52);
        static readonly Color InputBg = Color.FromArgb(28, 30, 36);
        static readonly Color Border = Color.FromArgb(64, 70, 82);
        static readonly Color Accent = Color.FromArgb(196, 140, 72);
        static readonly Color AccentHover = Color.FromArgb(214, 160, 90);
        static readonly Color Success = Color.FromArgb(80, 200, 120);
        static readonly Color Danger = Color.FromArgb(230, 90, 90);
        static readonly Color TextMain = Color.FromArgb(230, 232, 238);
        static readonly Color TextMuted = Color.FromArgb(150, 156, 170);

        ComboBox cmbLang;
        Button btnHelp;
        ComboBox cmbAction;
        TextBox txtInput;
        TextBox txtOutput;
        Button btnBrowseInput;
        Button btnBrowseOutput;
        Button btnRun;
        Button btnOpenOut;
        Button btnUnpackAll;
        RichTextBox logBox;
        Label lblTitle;
        Label lblSubtitle;
        Label lblInput;
        Label lblOutput;
        Label lblAction;
        Label lblDropHint;
        Panel dropPanel;
        ProgressBar progress;
        BackgroundWorker worker;
        bool applyingLang;
        readonly List<string> extraInputs = new List<string>();

        public MainForm()
        {
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(680, 560);
            Size = new Size(760, 640);
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Bg;
            ForeColor = TextMain;
            DoubleBuffered = true;
            AllowDrop = true;

            BuildUi();
            Wire();
            ApplyUi();
            Log(UiLang.T("Ready1"), TextMuted);
            Log(UiLang.T("Ready2"), TextMuted);
            Log(UiLang.T("Ready3"), TextMuted);
            Log(UiLang.Tf("GameRoot", ToolRunner.GameRoot()), TextMuted);
            Log("tools: " + ToolRunner.ToolsDir, TextMuted);
        }

        void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(18, 16, 18, 16),
                BackColor = Bg
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var titleRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                BackColor = Bg
            };
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));

            var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Bg };
            lblTitle = new Label
            {
                Font = new Font("Segoe UI Semibold", 16f),
                ForeColor = TextMain,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(0, 2, 8, 0)
            };
            lblSubtitle = new Label
            {
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(190, 194, 204),
                AutoSize = false,
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Padding = new Padding(2, 0, 8, 0)
            };
            titlePanel.Controls.Add(lblSubtitle);
            titlePanel.Controls.Add(lblTitle);
            titleRow.Controls.Add(titlePanel, 0, 0);

            btnHelp = MakeFlowSecondary("");
            btnHelp.Dock = DockStyle.Top;
            btnHelp.Height = 28;
            btnHelp.Margin = new Padding(4, 8, 4, 0);
            titleRow.Controls.Add(btnHelp, 1, 0);

            cmbLang = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = InputBg,
                ForeColor = TextMain,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Margin = new Padding(8, 8, 0, 0)
            };
            cmbLang.Items.Add("Русский");
            cmbLang.Items.Add("English");
            titleRow.Controls.Add(cmbLang, 2, 0);
            root.Controls.Add(titleRow, 0, 0);

            dropPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PanelBg,
                Margin = new Padding(0, 4, 0, 8),
                AllowDrop = true,
                Cursor = Cursors.Hand
            };
            dropPanel.Paint += DropPanel_Paint;
            lblDropHint = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 10.5f),
                BackColor = Color.Transparent
            };
            dropPanel.Controls.Add(lblDropHint);
            root.Controls.Add(dropPanel, 0, 1);

            var actRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                BackColor = Bg
            };
            actRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            actRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            lblAction = MakeFieldLabel("");
            actRow.Controls.Add(lblAction, 0, 0);
            cmbAction = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = InputBg,
                ForeColor = TextMain,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                Margin = new Padding(0, 4, 0, 4)
            };
            actRow.Controls.Add(cmbAction, 1, 0);
            root.Controls.Add(actRow, 0, 2);

            var paths = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2,
                BackColor = Bg
            };
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            paths.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            paths.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            paths.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            lblInput = MakeFieldLabel("");
            paths.Controls.Add(lblInput, 0, 0);
            txtInput = MakeTextBox();
            paths.Controls.Add(txtInput, 1, 0);
            btnBrowseInput = MakeSecondaryButton("");
            paths.Controls.Add(btnBrowseInput, 2, 0);

            lblOutput = MakeFieldLabel("");
            paths.Controls.Add(lblOutput, 0, 1);
            txtOutput = MakeTextBox();
            paths.Controls.Add(txtOutput, 1, 1);
            btnBrowseOutput = MakeSecondaryButton("");
            paths.Controls.Add(btnBrowseOutput, 2, 1);
            root.Controls.Add(paths, 0, 3);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Bg,
                Padding = new Padding(0, 4, 0, 0)
            };
            btnRun = MakePrimaryButton("");
            btnRun.Width = 150;
            btnOpenOut = MakeFlowSecondary("");
            btnOpenOut.Width = 140;
            btnUnpackAll = MakeFlowSecondary("");
            btnUnpackAll.Width = 150;
            actions.Controls.Add(btnRun);
            actions.Controls.Add(btnOpenOut);
            actions.Controls.Add(btnUnpackAll);
            root.Controls.Add(actions, 0, 4);

            var logHost = new Panel { Dock = DockStyle.Fill, BackColor = Bg, Padding = new Padding(0, 4, 0, 0) };
            progress = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 10,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 0
            };
            logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = InputBg,
                ForeColor = TextMain,
                Font = new Font("Consolas", 9f),
                DetectUrls = false,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            var logFrame = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Border,
                Padding = new Padding(1),
                Margin = new Padding(0, 8, 0, 0)
            };
            logFrame.Controls.Add(logBox);
            logHost.Controls.Add(logFrame);
            logHost.Controls.Add(progress);
            root.Controls.Add(logHost, 0, 5);
        }

        void FillActions()
        {
            int keep = cmbAction.SelectedIndex;
            cmbAction.Items.Clear();
            cmbAction.Items.Add(UiLang.T("ActUnpack"));
            cmbAction.Items.Add(UiLang.T("ActPack"));
            cmbAction.Items.Add(UiLang.T("ActBin2Xml"));
            cmbAction.Items.Add(UiLang.T("ActXml2Bin"));
            cmbAction.Items.Add(UiLang.T("ActXr2Xml"));
            cmbAction.Items.Add(UiLang.T("ActXml2Xr"));
            cmbAction.Items.Add(UiLang.T("ActMd2Txt"));
            cmbAction.Items.Add(UiLang.T("ActTxt2Md"));
            if (keep >= 0 && keep < cmbAction.Items.Count)
                cmbAction.SelectedIndex = keep;
            else
                cmbAction.SelectedIndex = 0;
        }

        void ApplyUi()
        {
            applyingLang = true;
            Text = UiLang.T("WinTitle");
            lblTitle.Text = UiLang.T("Title");
            lblSubtitle.Text = UiLang.T("Subtitle");
            lblAction.Text = UiLang.T("Action");
            lblInput.Text = UiLang.T("Input");
            lblOutput.Text = UiLang.T("Output");
            btnBrowseInput.Text = UiLang.T("Browse");
            btnBrowseOutput.Text = UiLang.T("Browse");
            btnRun.Text = UiLang.T("Run");
            btnOpenOut.Text = UiLang.T("OpenOut");
            btnUnpackAll.Text = UiLang.T("UnpackAll");
            btnHelp.Text = UiLang.T("Help");
            lblDropHint.Text = UiLang.T("DropHint");
            FillActions();
            int idx = (int)UiLang.Current;
            if (idx >= 0 && idx < cmbLang.Items.Count)
                cmbLang.SelectedIndex = idx;
            applyingLang = false;
        }

        Label MakeFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = TextMuted
            };
        }

        TextBox MakeTextBox()
        {
            return new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = InputBg,
                ForeColor = TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 6, 8, 6)
            };
        }

        Button MakePrimaryButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                BackColor = Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10f),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = AccentHover;
            return b;
        }

        Button MakeSecondaryButton(string text)
        {
            var b = new Button
            {
                Text = text,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = PanelBg,
                ForeColor = TextMain,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 6, 0, 6)
            };
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 60, 72);
            return b;
        }

        Button MakeFlowSecondary(string text)
        {
            var b = MakeSecondaryButton(text);
            b.Dock = DockStyle.None;
            b.Height = 36;
            b.Margin = new Padding(0, 0, 10, 0);
            return b;
        }

        void Wire()
        {
            cmbLang.SelectedIndexChanged += (s, e) =>
            {
                if (applyingLang) return;
                if (cmbLang.SelectedIndex < 0) return;
                var next = (UiLanguage)cmbLang.SelectedIndex;
                if (next == UiLang.Current) return;
                UiLang.Current = next;
                ApplyUi();
                Log(UiLang.T("LangChanged"), TextMuted);
            };

            cmbAction.SelectedIndexChanged += (s, e) =>
            {
                if (applyingLang) return;
                if (!string.IsNullOrWhiteSpace(txtInput.Text))
                    txtOutput.Text = ToolRunner.SuggestOutput(txtInput.Text.Trim(), CurrentAction());
            };

            btnBrowseInput.Click += (s, e) => BrowseInput();
            btnBrowseOutput.Click += (s, e) => BrowseOutput();
            btnRun.Click += (s, e) => RunCurrent();
            btnUnpackAll.Click += (s, e) => RunUnpackAll();
            btnOpenOut.Click += (s, e) => OpenOutput();
            btnHelp.Click += (s, e) =>
            {
                MessageBox.Show(this, UiLang.T("HelpText"), UiLang.T("Help"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            dropPanel.Click += (s, e) => BrowseInput();
            lblDropHint.Click += (s, e) => BrowseInput();
            dropPanel.DragEnter += Drop_DragEnter;
            dropPanel.DragDrop += Drop_DragDrop;
            DragEnter += Drop_DragEnter;
            DragDrop += Drop_DragDrop;

            worker = new BackgroundWorker { WorkerReportsProgress = true };
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += (s, e) =>
            {
                string line = e.UserState as string;
                if (!string.IsNullOrEmpty(line))
                    Log(line, TextMuted);
            };
            worker.RunWorkerCompleted += Worker_Done;
        }

        ActionKind CurrentAction()
        {
            int i = cmbAction.SelectedIndex;
            if (i < 0) i = 0;
            return (ActionKind)i;
        }

        void SetAction(ActionKind kind)
        {
            int i = (int)kind;
            if (i >= 0 && i < cmbAction.Items.Count)
                cmbAction.SelectedIndex = i;
        }

        void SetInputs(string[] paths)
        {
            extraInputs.Clear();
            if (paths == null || paths.Length == 0)
                return;
            txtInput.Text = paths[0];
            for (int i = 1; i < paths.Length; i++)
                extraInputs.Add(paths[i]);

            ActionKind kind = ToolRunner.GuessAction(paths[0]);
            SetAction(kind);
            txtOutput.Text = ToolRunner.SuggestOutput(paths[0], kind);
            Log(UiLang.Tf("Guessed", cmbAction.Text), Accent);
            if (paths.Length > 1)
                Log(UiLang.Tf("Batch", paths.Length), TextMuted);
        }

        void Drop_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        void Drop_DragDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0)
                return;
            SetInputs(files);
        }

        void DropPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = dropPanel.ClientRectangle;
            rect.Width -= 1;
            rect.Height -= 1;
            using (var pen = new Pen(Border, 1.5f) { DashStyle = DashStyle.Dash })
                g.DrawRectangle(pen, rect);
        }

        void BrowseInput()
        {
            if (CurrentAction() == ActionKind.PackFolder)
            {
                using (var fbd = new FolderBrowserDialog { Description = UiLang.T("PickFolder") })
                {
                    if (!string.IsNullOrWhiteSpace(txtInput.Text) && Directory.Exists(txtInput.Text))
                        fbd.SelectedPath = txtInput.Text;
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                        SetInputs(new[] { fbd.SelectedPath });
                }
                return;
            }

            using (var ofd = new OpenFileDialog
            {
                Title = UiLang.T("PickAny"),
                Filter = UiLang.T("FilterAny"),
                CheckFileExists = true,
                Multiselect = true
            })
            {
                if (!string.IsNullOrWhiteSpace(txtInput.Text))
                {
                    try
                    {
                        ofd.InitialDirectory = Path.GetDirectoryName(txtInput.Text);
                        ofd.FileName = Path.GetFileName(txtInput.Text);
                    }
                    catch { }
                }
                if (ofd.ShowDialog(this) == DialogResult.OK)
                    SetInputs(ofd.FileNames);
            }
        }

        void BrowseOutput()
        {
            ActionKind kind = CurrentAction();
            if (kind == ActionKind.UnpackPack)
            {
                using (var fbd = new FolderBrowserDialog { Description = UiLang.T("OpenOut") })
                {
                    if (!string.IsNullOrWhiteSpace(txtOutput.Text))
                    {
                        try { fbd.SelectedPath = txtOutput.Text; } catch { }
                    }
                    if (fbd.ShowDialog(this) == DialogResult.OK)
                        txtOutput.Text = fbd.SelectedPath;
                }
                return;
            }

            using (var sfd = new SaveFileDialog
            {
                Title = UiLang.T("SaveAs"),
                OverwritePrompt = true
            })
            {
                if (!string.IsNullOrWhiteSpace(txtOutput.Text))
                {
                    try
                    {
                        sfd.InitialDirectory = Path.GetDirectoryName(txtOutput.Text);
                        sfd.FileName = Path.GetFileName(txtOutput.Text);
                    }
                    catch { }
                }
                if (sfd.ShowDialog(this) == DialogResult.OK)
                    txtOutput.Text = sfd.FileName;
            }
        }

        void OpenOutput()
        {
            string p = txtOutput.Text.Trim();
            if (string.IsNullOrEmpty(p))
            {
                Log(UiLang.T("NoOutPath"), Danger);
                return;
            }
            string dir = p;
            if (File.Exists(p))
                dir = Path.GetDirectoryName(p);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                Log(UiLang.T("OutMissing"), Danger);
                return;
            }
            Process.Start("explorer.exe", dir);
        }

        void RunCurrent()
        {
            if (worker.IsBusy)
            {
                Log(UiLang.T("Busy"), Danger);
                return;
            }

            string input = txtInput.Text.Trim();
            string output = txtOutput.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                Log(UiLang.T("NeedInput"), Danger);
                return;
            }
            if (string.IsNullOrEmpty(output))
            {
                Log(UiLang.T("NeedOutput"), Danger);
                return;
            }

            var jobs = new List<Tuple<ActionKind, string, string>>();
            ActionKind kind = CurrentAction();
            jobs.Add(Tuple.Create(kind, input, output));
            foreach (string extra in extraInputs)
            {
                string out2 = ToolRunner.SuggestOutput(extra, ToolRunner.GuessAction(extra));
                jobs.Add(Tuple.Create(ToolRunner.GuessAction(extra), extra, out2));
            }

            foreach (var j in jobs)
            {
                if (File.Exists(j.Item3) || (Directory.Exists(j.Item3) && Directory.GetFileSystemEntries(j.Item3).Length > 0))
                {
                    var ans = MessageBox.Show(this, UiLang.Tf("Overwrite", j.Item3),
                        UiLang.T("Confirm"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (ans == DialogResult.Cancel)
                    {
                        Log(UiLang.T("Cancelled"), TextMuted);
                        return;
                    }
                    if (ans == DialogResult.No)
                        return;
                    break;
                }
            }

            StartJobs(jobs);
        }

        void RunUnpackAll()
        {
            if (worker.IsBusy)
            {
                Log(UiLang.T("Busy"), Danger);
                return;
            }

            string game = ToolRunner.GameRoot();
            string[] packs = Directory.GetFiles(game, "*.pack");
            if (packs.Length == 0)
            {
                Log(UiLang.T("NoPacks"), Danger);
                return;
            }

            string destRoot = ToolRunner.ExtractRoot();
            var ans = MessageBox.Show(this,
                UiLang.Tf("ConfirmAll", game, destRoot),
                UiLang.T("Confirm"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (ans != DialogResult.Yes)
            {
                Log(UiLang.T("Cancelled"), TextMuted);
                return;
            }

            var jobs = new List<Tuple<ActionKind, string, string>>();
            foreach (string pack in packs)
            {
                string name = Path.GetFileNameWithoutExtension(pack);
                jobs.Add(Tuple.Create(ActionKind.UnpackPack, pack, Path.Combine(destRoot, name)));
            }
            StartJobs(jobs);
        }

        void StartJobs(List<Tuple<ActionKind, string, string>> jobs)
        {
            btnRun.Enabled = false;
            btnUnpackAll.Enabled = false;
            progress.MarqueeAnimationSpeed = 30;
            worker.RunWorkerAsync(jobs);
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var jobs = (List<Tuple<ActionKind, string, string>>)e.Argument;
            int ok = 0;
            int fail = 0;
            string lastOut = null;
            foreach (var j in jobs)
            {
                worker.ReportProgress(0, UiLang.Tf("Work", j.Item2));
                var r = ToolRunner.Run(j.Item1, j.Item2, j.Item3, line => worker.ReportProgress(0, line));
                if (r.Ok)
                {
                    ok++;
                    lastOut = r.OutputPath;
                    worker.ReportProgress(0, UiLang.Tf("Ok", r.OutputPath));
                }
                else
                {
                    fail++;
                    worker.ReportProgress(0, UiLang.Tf("Fail", r.Message));
                }
            }
            e.Result = Tuple.Create(ok, fail, lastOut);
        }

        void Worker_Done(object sender, RunWorkerCompletedEventArgs e)
        {
            btnRun.Enabled = true;
            btnUnpackAll.Enabled = true;
            progress.MarqueeAnimationSpeed = 0;
            progress.Value = 0;
            if (e.Error != null)
            {
                Log(UiLang.Tf("Fail", e.Error.Message), Danger);
                return;
            }
            var t = (Tuple<int, int, string>)e.Result;
            if (!string.IsNullOrEmpty(t.Item3))
                txtOutput.Text = t.Item3;
        }

        void Log(string text, Color color)
        {
            if (logBox.IsDisposed) return;
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.SelectionColor = color;
            logBox.AppendText(text + Environment.NewLine);
            logBox.SelectionColor = logBox.ForeColor;
            logBox.ScrollToCaret();
        }
    }
}
