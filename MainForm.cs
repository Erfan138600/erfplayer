using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ErfPlayer
{
    public partial class MainForm : Form
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;
        private Timer progressTimer;
        private Timer animationTimer;
        private List<string> playlist;
        private List<string> originalPlaylist;
        private int currentTrackIndex;
        private bool isPlaying;
        private bool isPaused;
        private bool isShuffled;
        private bool isRepeating;
        private bool isRepeatingOne;
        private Random random;
        private float animationPhase;
        private Color[] gradientColors;
        private int currentColorIndex;
        private bool isVideoMode;
        private Panel videoPanel;
        private WebBrowser videoPlayer;
        private bool isPersianLanguage = true; // Default to Persian
        private Process videoProcess;
        private bool isFullscreen = false;
        private FormWindowState previousWindowState;

        // UI Controls
        private Panel mainPanel;
        private Panel controlPanel;
        private Panel playlistPanel;
        private ListBox playlistListBox;
        private Button playButton;
        private Button pauseButton;
        private Button stopButton;
        private Button nextButton;
        private Button previousButton;
        private Button addMusicButton;
        private Button addFolderButton;
        private Button removeMusicButton;
        private Button shuffleButton;
        private Button repeatButton;
        private Button clearPlaylistButton;
        private Button savePlaylistButton;
        private Button loadPlaylistButton;
        private Button videoModeButton;
        private Button languageButton;
        private Button fullscreenButton;
        private TrackBar volumeTrackBar;
        private TrackBar progressTrackBar;
        private Label currentTimeLabel;
        private Label totalTimeLabel;
        private Label nowPlayingLabel;
        private Label volumeLabel;
        private Panel equalizerPanel;
        private Panel[] equalizerBars;
        private PictureBox albumArtBox;
        private Label statusLabel;
        private Label creditLabel;

        public MainForm()
        {
            InitializeComponent();
            InitializeAudio();
            SetupEventHandlers();
            InitializeAnimations();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form properties
            this.Text = isPersianLanguage ? "ErfPlayer - پخش کننده موسیقی و فیلم 🎵" : "ErfPlayer - Music & Video Player 🎵";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(20, 20, 30);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.Icon = SystemIcons.Application;
            this.AllowDrop = true;
            this.WindowState = FormWindowState.Normal;

            // Try to load custom application icon from executable directory
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string icoPath = Path.Combine(exeDir, "logo.ico");
                if (File.Exists(icoPath))
                {
                    this.Icon = new Icon(icoPath);
                }
                else
                {
                    // Fallback: generate icon from runtime logo
                    using (var bmp = CreateLogoBitmap(64))
                    {
                        this.Icon = Icon.FromHandle(bmp.GetHicon());
                    }
                }
            }
            catch { /* ignore icon load failures */ }

            // Initialize gradient colors
            gradientColors = new Color[]
            {
                Color.FromArgb(255, 100, 150), // Pink
                Color.FromArgb(100, 200, 255), // Blue
                Color.FromArgb(100, 255, 150), // Green
                Color.FromArgb(255, 200, 100), // Orange
                Color.FromArgb(200, 100, 255), // Purple
                Color.FromArgb(255, 255, 100), // Yellow
                Color.FromArgb(255, 100, 100)  // Red
            };

            // Main panel
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 40),
                Padding = new Padding(15)
            };
            this.Controls.Add(mainPanel);

            // Album art box / Video panel
            albumArtBox = new PictureBox
            {
                Location = new Point(20, 20),
                Size = new Size(200, 200),
                BackColor = Color.FromArgb(50, 50, 60),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            mainPanel.Controls.Add(albumArtBox);

            // Video panel (initially hidden)
            videoPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(200, 200),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            mainPanel.Controls.Add(videoPanel);

            // Video player (WebBrowser for HTML5 video)
            videoPlayer = new WebBrowser
            {
                Dock = DockStyle.Fill,
                IsWebBrowserContextMenuEnabled = false,
                WebBrowserShortcutsEnabled = false,
                ScriptErrorsSuppressed = true
            };
            videoPanel.Controls.Add(videoPlayer);

            // Try to load logo image into album art box if available
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string logoPng = Path.Combine(exeDir, "logo.png");
                if (File.Exists(logoPng))
                {
                    albumArtBox.Image = Image.FromFile(logoPng);
                }
                else
                {
                    albumArtBox.Image = CreateLogoBitmap(200);
                }
            }
            catch { /* ignore image load failures */ }

            // Now playing label
            nowPlayingLabel = new Label
            {
                Text = isPersianLanguage ? "🎵 در حال پخش: هیچ فایلی انتخاب نشده" : "🎵 Now Playing: No track selected",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 255),
                AutoSize = true,
                Location = new Point(240, 30)
            };
            mainPanel.Controls.Add(nowPlayingLabel);

            // Status label
            statusLabel = new Label
            {
                Text = isPersianLanguage ? "آماده پخش موسیقی! 🎶" : "Ready to play music! 🎶",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 150, 150),
                AutoSize = true,
                Location = new Point(240, 60)
            };
            mainPanel.Controls.Add(statusLabel);

            // Equalizer panel
            equalizerPanel = new Panel
            {
                Location = new Point(240, 90),
                Size = new Size(300, 120),
                BackColor = Color.FromArgb(40, 40, 50),
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add(equalizerPanel);

            // Create equalizer bars
            equalizerBars = new Panel[16];
            for (int i = 0; i < 16; i++)
            {
                equalizerBars[i] = new Panel
                {
                    Location = new Point(10 + i * 18, 10),
                    Size = new Size(15, 10),
                    BackColor = gradientColors[i % gradientColors.Length]
                };
                equalizerPanel.Controls.Add(equalizerBars[i]);
            }

            // Playlist panel
            playlistPanel = new Panel
            {
                Location = new Point(20, 240),
                Size = new Size(960, 300),
                BackColor = Color.FromArgb(40, 40, 50),
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add(playlistPanel);

            // Playlist label
            var playlistLabel = new Label
            {
                Text = isPersianLanguage ? "🎼 لیست پخش" : "🎼 Playlist",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                AutoSize = true
            };
            playlistPanel.Controls.Add(playlistLabel);

            // Playlist listbox
            playlistListBox = new ListBox
            {
                Location = new Point(15, 45),
                Size = new Size(600, 240),
                BackColor = Color.FromArgb(50, 50, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                SelectionMode = SelectionMode.One
            };
            playlistPanel.Controls.Add(playlistListBox);

            // Control buttons panel
            var buttonPanel = new Panel
            {
                Location = new Point(630, 45),
                Size = new Size(320, 240),
                BackColor = Color.FromArgb(45, 45, 55)
            };
            playlistPanel.Controls.Add(buttonPanel);

            // Add music button
            addMusicButton = CreateStyledButton(isPersianLanguage ? "🎵 افزودن موسیقی" : "🎵 Add Music", new Point(10, 10), new Size(140, 40), Color.FromArgb(0, 150, 255));
            buttonPanel.Controls.Add(addMusicButton);

            // Add folder button
            addFolderButton = CreateStyledButton(isPersianLanguage ? "📁 افزودن پوشه" : "📁 Add Folder", new Point(160, 10), new Size(140, 40), Color.FromArgb(0, 200, 100));
            buttonPanel.Controls.Add(addFolderButton);

            // Remove music button
            removeMusicButton = CreateStyledButton(isPersianLanguage ? "🗑️ حذف" : "🗑️ Remove", new Point(10, 60), new Size(140, 40), Color.FromArgb(255, 100, 100));
            buttonPanel.Controls.Add(removeMusicButton);

            // Clear playlist button
            clearPlaylistButton = CreateStyledButton(isPersianLanguage ? "🧹 پاک کردن همه" : "🧹 Clear All", new Point(160, 60), new Size(140, 40), Color.FromArgb(255, 150, 50));
            buttonPanel.Controls.Add(clearPlaylistButton);

            // Save playlist button
            savePlaylistButton = CreateStyledButton(isPersianLanguage ? "💾 ذخیره" : "💾 Save", new Point(10, 160), new Size(140, 40), Color.FromArgb(100, 200, 255));
            buttonPanel.Controls.Add(savePlaylistButton);

            // Load playlist button
            loadPlaylistButton = CreateStyledButton(isPersianLanguage ? "📂 بارگذاری" : "📂 Load", new Point(160, 160), new Size(140, 40), Color.FromArgb(255, 100, 200));
            buttonPanel.Controls.Add(loadPlaylistButton);

            // Video mode button
            videoModeButton = CreateStyledButton(isPersianLanguage ? "🎬 حالت ویدیو" : "🎬 Video Mode", new Point(10, 210), new Size(140, 40), Color.FromArgb(150, 50, 200));
            buttonPanel.Controls.Add(videoModeButton);

            // Language button
            languageButton = CreateStyledButton(isPersianLanguage ? "🌐 English" : "🌐 فارسی", new Point(160, 210), new Size(140, 40), Color.FromArgb(100, 150, 200));
            buttonPanel.Controls.Add(languageButton);

            // Shuffle button
            shuffleButton = CreateStyledButton(isPersianLanguage ? "🔀 تصادفی" : "🔀 Shuffle", new Point(10, 110), new Size(140, 40), Color.FromArgb(200, 100, 255));
            buttonPanel.Controls.Add(shuffleButton);

            // Repeat button
            repeatButton = CreateStyledButton(isPersianLanguage ? "🔁 تکرار" : "🔁 Repeat", new Point(160, 110), new Size(140, 40), Color.FromArgb(255, 200, 100));
            buttonPanel.Controls.Add(repeatButton);

            // Control panel
            controlPanel = new Panel
            {
                Location = new Point(20, 600),
                Size = new Size(1160, 120),
                BackColor = Color.FromArgb(40, 40, 50),
                BorderStyle = BorderStyle.FixedSingle
            };
            mainPanel.Controls.Add(controlPanel);

            // Credit label at the very bottom of the window
            creditLabel = new Label
            {
                Text = "برنامه نویسی = عرفان علیخانی",
                Dock = DockStyle.Bottom,
                Height = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.FromArgb(25, 25, 35),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                RightToLeft = RightToLeft.Yes
            };
            this.Controls.Add(creditLabel);
            creditLabel.BringToFront();

            // Progress trackbar
            progressTrackBar = new TrackBar
            {
                Location = new Point(20, 15),
                Size = new Size(1120, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                BackColor = Color.FromArgb(50, 50, 60),
                TickStyle = TickStyle.None
            };
            controlPanel.Controls.Add(progressTrackBar);

            // Time labels
            currentTimeLabel = new Label
            {
                Text = "00:00",
                Location = new Point(20, 65),
                Size = new Size(60, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            controlPanel.Controls.Add(currentTimeLabel);

            totalTimeLabel = new Label
            {
                Text = "00:00",
                Location = new Point(880, 65),
                Size = new Size(60, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            controlPanel.Controls.Add(totalTimeLabel);

            // Control buttons - Much larger and more visible
            previousButton = CreateControlButton("⏮", new Point(50, 70), new Size(80, 50), Color.FromArgb(100, 100, 150));
            controlPanel.Controls.Add(previousButton);

            playButton = CreateControlButton("▶", new Point(150, 70), new Size(80, 50), Color.FromArgb(0, 200, 100));
            controlPanel.Controls.Add(playButton);

            pauseButton = CreateControlButton("⏸", new Point(250, 70), new Size(80, 50), Color.FromArgb(255, 150, 50));
            controlPanel.Controls.Add(pauseButton);

            stopButton = CreateControlButton("⏹", new Point(350, 70), new Size(80, 50), Color.FromArgb(255, 100, 100));
            controlPanel.Controls.Add(stopButton);

            nextButton = CreateControlButton("⏭", new Point(450, 70), new Size(80, 50), Color.FromArgb(100, 100, 150));
            controlPanel.Controls.Add(nextButton);

            // Fullscreen button
            fullscreenButton = CreateControlButton("⛶", new Point(550, 70), new Size(80, 50), Color.FromArgb(150, 50, 200));
            controlPanel.Controls.Add(fullscreenButton);

            // Volume control
            volumeLabel = new Label
            {
                Text = isPersianLanguage ? "🔊 صدا:" : "🔊 Volume:",
                Location = new Point(650, 80),
                Size = new Size(80, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            controlPanel.Controls.Add(volumeLabel);

            volumeTrackBar = new TrackBar
            {
                Location = new Point(740, 75),
                Size = new Size(200, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(50, 50, 60),
                TickStyle = TickStyle.None
            };
            controlPanel.Controls.Add(volumeTrackBar);

            this.ResumeLayout(false);
        }

        private Button CreateStyledButton(string text, Point location, Size size, Color backColor)
        {
            var button = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, backColor.R + 30),
                Math.Min(255, backColor.G + 30),
                Math.Min(255, backColor.B + 30)
            );
            return button;
        }

        private Bitmap CreateLogoBitmap(int size)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Background
                g.Clear(Color.FromArgb(30, 30, 40));

                // Gradient circle
                var rect = new Rectangle(8, 8, size - 16, size - 16);
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(rect);
                    using (var pgb = new PathGradientBrush(path))
                    {
                        pgb.CenterColor = Color.FromArgb(255, 120, 70);
                        pgb.SurroundColors = new[] { Color.FromArgb(255, 60, 150) };
                        g.FillEllipse(pgb, rect);
                    }
                }

                // Inner ring
                using (var pen = new Pen(Color.FromArgb(255, 230, 230, 230), Math.Max(2, size / 40f)))
                {
                    g.DrawEllipse(pen, rect);
                }

                // Musical note
                float noteW = size * 0.26f;
                float noteH = size * 0.42f;
                float noteX = size * 0.44f;
                float noteY = size * 0.28f;

                using (var pen = new Pen(Color.White, Math.Max(4, size / 22f)) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
                using (var brush = new SolidBrush(Color.White))
                {
                    // Stem
                    g.DrawLine(pen, noteX, noteY + noteH, noteX, noteY);
                    g.DrawLine(pen, noteX, noteY, noteX + noteW, noteY + size * 0.08f);

                    // Note heads
                    float r = size * 0.09f;
                    g.FillEllipse(brush, noteX - r, noteY + noteH - r, r * 2, r * 2);
                    g.FillEllipse(brush, noteX + noteW - r, noteY + size * 0.08f + noteH * 0.35f - r, r * 2, r * 2);
                }
            }
            return bmp;
        }

        private Button CreateControlButton(string text, Point location, Size size, Color backColor)
        {
            var button = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            button.FlatAppearance.BorderSize = 2;
            button.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, backColor.R + 50),
                Math.Min(255, backColor.G + 50),
                Math.Min(255, backColor.B + 50)
            );
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(
                Math.Max(0, backColor.R - 30),
                Math.Max(0, backColor.G - 30),
                Math.Max(0, backColor.B - 30)
            );
            return button;
        }

        private void InitializeAudio()
        {
            playlist = new List<string>();
            originalPlaylist = new List<string>();
            currentTrackIndex = -1;
            isPlaying = false;
            isPaused = false;
            isShuffled = false;
            isRepeating = false;
            isRepeatingOne = false;
            random = new Random();
            currentColorIndex = 0;

            progressTimer = new Timer
            {
                Interval = 1000
            };
            progressTimer.Tick += ProgressTimer_Tick;

            animationTimer = new Timer
            {
                Interval = 50
            };
            animationTimer.Tick += AnimationTimer_Tick;

            // Initialize button states
            UpdateButtonStates();
        }

        private void InitializeAnimations()
        {
            animationPhase = 0;
            animationTimer.Start();
        }

        private void SetupEventHandlers()
        {
            playButton.Click += PlayButton_Click;
            pauseButton.Click += PauseButton_Click;
            stopButton.Click += StopButton_Click;
            nextButton.Click += NextButton_Click;
            previousButton.Click += PreviousButton_Click;
            addMusicButton.Click += AddMusicButton_Click;
            addFolderButton.Click += AddFolderButton_Click;
            removeMusicButton.Click += RemoveMusicButton_Click;
            clearPlaylistButton.Click += ClearPlaylistButton_Click;
            savePlaylistButton.Click += SavePlaylistButton_Click;
            loadPlaylistButton.Click += LoadPlaylistButton_Click;
            shuffleButton.Click += ShuffleButton_Click;
            repeatButton.Click += RepeatButton_Click;
            videoModeButton.Click += VideoModeButton_Click;
            languageButton.Click += LanguageButton_Click;
            fullscreenButton.Click += FullscreenButton_Click;
            playlistListBox.SelectedIndexChanged += PlaylistListBox_SelectedIndexChanged;
            playlistListBox.DoubleClick += PlaylistListBox_DoubleClick;
            progressTrackBar.MouseDown += ProgressTrackBar_MouseDown;
            progressTrackBar.MouseUp += ProgressTrackBar_MouseUp;
            volumeTrackBar.ValueChanged += VolumeTrackBar_ValueChanged;

            // Drag and drop
            this.DragEnter += MainForm_DragEnter;
            this.DragDrop += MainForm_DragDrop;
            playlistListBox.DragEnter += MainForm_DragEnter;
            playlistListBox.DragDrop += MainForm_DragDrop;

            // Keyboard shortcuts
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            if (playlist.Count == 0) return;

            if (currentTrackIndex == -1)
            {
                currentTrackIndex = 0;
            }

            if (isPaused)
            {
                ResumePlayback();
            }
            else
            {
                PlayCurrentTrack();
            }
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                PausePlayback();
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            StopPlayback();
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            if (playlist.Count == 0) return;

            if (isShuffled)
            {
                currentTrackIndex = random.Next(playlist.Count);
            }
            else
            {
                currentTrackIndex = (currentTrackIndex + 1) % playlist.Count;
            }
            PlayCurrentTrack();
        }

        private void PreviousButton_Click(object sender, EventArgs e)
        {
            if (playlist.Count == 0) return;

            if (isShuffled)
            {
                currentTrackIndex = random.Next(playlist.Count);
            }
            else
            {
                currentTrackIndex = currentTrackIndex == 0 ? playlist.Count - 1 : currentTrackIndex - 1;
            }
            PlayCurrentTrack();
        }

        private void AddMusicButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Media Files|*.mp3;*.wav;*.flac;*.aac;*.m4a;*.wma;*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm|Audio Files|*.mp3;*.wav;*.flac;*.aac;*.m4a;*.wma|Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm|MP3 Files|*.mp3|WAV Files|*.wav|FLAC Files|*.flac|All Files|*.*";
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    AddFilesToList(openFileDialog.FileNames);
                }
            }
        }

        private void AddFolderButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a folder containing music files";
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string[] mediaFiles = Directory.GetFiles(folderDialog.SelectedPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => IsMediaFile(file)).ToArray();
                    
                    if (mediaFiles.Length > 0)
                    {
                        AddFilesToList(mediaFiles);
                        statusLabel.Text = $"Added {mediaFiles.Length} files from folder! 🎵";
                    }
                    else
                    {
                        MessageBox.Show("No media files found in the selected folder.", "No Media Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private bool IsAudioFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".mp3" || extension == ".wav" || extension == ".flac" || 
                   extension == ".aac" || extension == ".m4a" || extension == ".wma";
        }

        private bool IsVideoFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".mp4" || extension == ".avi" || extension == ".mkv" || 
                   extension == ".mov" || extension == ".wmv" || extension == ".flv" || extension == ".webm";
        }

        private bool IsMediaFile(string filePath)
        {
            return IsAudioFile(filePath) || IsVideoFile(filePath);
        }

        private void AddFilesToList(string[] files)
        {
            int addedCount = 0;
            foreach (string fileName in files)
            {
                if (!playlist.Contains(fileName))
                {
                    playlist.Add(fileName);
                    originalPlaylist.Add(fileName);
                    addedCount++;
                }
            }
            UpdatePlaylist();
            statusLabel.Text = $"Added {addedCount} new files! 🎶";
        }

        private void RemoveMusicButton_Click(object sender, EventArgs e)
        {
            if (playlistListBox.SelectedIndex >= 0)
            {
                int selectedIndex = playlistListBox.SelectedIndex;
                playlist.RemoveAt(selectedIndex);
                originalPlaylist.RemoveAt(selectedIndex);
                
                if (currentTrackIndex == selectedIndex)
                {
                    StopPlayback();
                    currentTrackIndex = -1;
                }
                else if (currentTrackIndex > selectedIndex)
                {
                    currentTrackIndex--;
                }
                
                UpdatePlaylist();
                statusLabel.Text = "Track removed from playlist! 🗑️";
            }
        }

        private void ClearPlaylistButton_Click(object sender, EventArgs e)
        {
            if (playlist.Count > 0)
            {
                var result = MessageBox.Show("Are you sure you want to clear the entire playlist?", "Clear Playlist", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    StopPlayback();
                    playlist.Clear();
                    originalPlaylist.Clear();
                    currentTrackIndex = -1;
                    UpdatePlaylist();
                    statusLabel.Text = "Playlist cleared! 🧹";
                }
            }
        }

        private void ShuffleButton_Click(object sender, EventArgs e)
        {
            if (playlist.Count > 0)
            {
                isShuffled = !isShuffled;
                if (isShuffled)
                {
                    ShufflePlaylist();
                    shuffleButton.BackColor = Color.FromArgb(255, 200, 100);
                    statusLabel.Text = "Shuffle mode ON! 🔀";
                }
                else
                {
                    RestoreOriginalOrder();
                    shuffleButton.BackColor = Color.FromArgb(200, 100, 255);
                    statusLabel.Text = "Shuffle mode OFF! 🔀";
                }
            }
        }

        private void SavePlaylistButton_Click(object sender, EventArgs e)
        {
            if (playlist.Count == 0)
            {
                MessageBox.Show("No tracks to save!", "Empty Playlist", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Playlist Files|*.m3u|All Files|*.*";
                saveDialog.DefaultExt = "m3u";
                saveDialog.FileName = "MyPlaylist.m3u";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (StreamWriter writer = new StreamWriter(saveDialog.FileName))
                        {
                            writer.WriteLine("#EXTM3U");
                            foreach (string filePath in playlist)
                            {
                                writer.WriteLine(filePath);
                            }
                        }
                        statusLabel.Text = $"Playlist saved to {Path.GetFileName(saveDialog.FileName)}! 💾";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving playlist: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void LoadPlaylistButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Playlist Files|*.m3u;*.pls|All Files|*.*";
                openDialog.Title = "Load Playlist";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var newPlaylist = new List<string>();
                        using (StreamReader reader = new StreamReader(openDialog.FileName))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                line = line.Trim();
                                if (!line.StartsWith("#") && !string.IsNullOrEmpty(line) && File.Exists(line))
                                {
                                    newPlaylist.Add(line);
                                }
                            }
                        }

                        if (newPlaylist.Count > 0)
                        {
                            playlist = newPlaylist;
                            originalPlaylist = new List<string>(newPlaylist);
                            currentTrackIndex = -1;
                            UpdatePlaylist();
                            statusLabel.Text = $"Loaded {newPlaylist.Count} tracks from playlist! 📂";
                        }
                        else
                        {
                            MessageBox.Show("No valid tracks found in the playlist file.", "Empty Playlist", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading playlist: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void RepeatButton_Click(object sender, EventArgs e)
        {
            if (isRepeatingOne)
            {
                isRepeating = false;
                isRepeatingOne = false;
                repeatButton.BackColor = Color.FromArgb(255, 200, 100);
                statusLabel.Text = "Repeat mode OFF! 🔁";
            }
            else if (isRepeating)
            {
                isRepeating = false;
                isRepeatingOne = true;
                repeatButton.BackColor = Color.FromArgb(255, 100, 200);
                statusLabel.Text = "Repeat ONE mode ON! 🔁";
            }
            else
            {
                isRepeating = true;
                isRepeatingOne = false;
                repeatButton.BackColor = Color.FromArgb(100, 255, 100);
                statusLabel.Text = "Repeat ALL mode ON! 🔁";
            }
        }

        private void VideoModeButton_Click(object sender, EventArgs e)
        {
            isVideoMode = !isVideoMode;
            
            if (isVideoMode)
            {
                videoModeButton.BackColor = Color.FromArgb(255, 150, 50);
                videoModeButton.Text = isPersianLanguage ? "🎵 حالت صدا" : "🎵 Audio Mode";
                albumArtBox.Visible = false;
                videoPanel.Visible = true;
                statusLabel.Text = isPersianLanguage ? "حالت ویدیو فعال! 🎬" : "Video mode ON! 🎬";
            }
            else
            {
                videoModeButton.BackColor = Color.FromArgb(150, 50, 200);
                videoModeButton.Text = isPersianLanguage ? "🎬 حالت ویدیو" : "🎬 Video Mode";
                albumArtBox.Visible = true;
                videoPanel.Visible = false;
                statusLabel.Text = isPersianLanguage ? "حالت صدا فعال! 🎵" : "Audio mode ON! 🎵";
            }
        }

        private void LanguageButton_Click(object sender, EventArgs e)
        {
            isPersianLanguage = !isPersianLanguage;
            UpdateLanguage();
        }

        private void FullscreenButton_Click(object sender, EventArgs e)
        {
            ToggleFullscreen();
        }

        private void ToggleFullscreen()
        {
            if (!isFullscreen)
            {
                // Enter fullscreen
                previousWindowState = this.WindowState;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                isFullscreen = true;
                fullscreenButton.Text = "⛶";
                statusLabel.Text = isPersianLanguage ? "حالت تمام صفحه فعال! ⛶" : "Fullscreen mode ON! ⛶";
            }
            else
            {
                // Exit fullscreen
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = previousWindowState;
                isFullscreen = false;
                fullscreenButton.Text = "⛶";
                statusLabel.Text = isPersianLanguage ? "حالت عادی فعال! ⛶" : "Normal mode ON! ⛶";
            }
        }

        private void UpdateLanguage()
        {
            // Update form title
            this.Text = isPersianLanguage ? "ErfPlayer - پخش کننده موسیقی و فیلم 🎵" : "ErfPlayer - Music & Video Player 🎵";
            
            // Update labels
            nowPlayingLabel.Text = isPersianLanguage ? "🎵 در حال پخش: هیچ فایلی انتخاب نشده" : "🎵 Now Playing: No track selected";
            statusLabel.Text = isPersianLanguage ? "آماده پخش موسیقی! 🎶" : "Ready to play music! 🎶";
            volumeLabel.Text = isPersianLanguage ? "🔊 صدا:" : "🔊 Volume:";
            
            // Update button texts
            addMusicButton.Text = isPersianLanguage ? "🎵 افزودن موسیقی" : "🎵 Add Music";
            addFolderButton.Text = isPersianLanguage ? "📁 افزودن پوشه" : "📁 Add Folder";
            removeMusicButton.Text = isPersianLanguage ? "🗑️ حذف" : "🗑️ Remove";
            clearPlaylistButton.Text = isPersianLanguage ? "🧹 پاک کردن همه" : "🧹 Clear All";
            savePlaylistButton.Text = isPersianLanguage ? "💾 ذخیره" : "💾 Save";
            loadPlaylistButton.Text = isPersianLanguage ? "📂 بارگذاری" : "📂 Load";
            shuffleButton.Text = isPersianLanguage ? "🔀 تصادفی" : "🔀 Shuffle";
            repeatButton.Text = isPersianLanguage ? "🔁 تکرار" : "🔁 Repeat";
            languageButton.Text = isPersianLanguage ? "🌐 English" : "🌐 فارسی";
            
            // Update video mode button
            if (isVideoMode)
            {
                videoModeButton.Text = isPersianLanguage ? "🎵 حالت صدا" : "🎵 Audio Mode";
            }
            else
            {
                videoModeButton.Text = isPersianLanguage ? "🎬 حالت ویدیو" : "🎬 Video Mode";
            }
            
            // Update playlist label
            var playlistLabel = playlistPanel.Controls.OfType<Label>().FirstOrDefault();
            if (playlistLabel != null)
            {
                playlistLabel.Text = isPersianLanguage ? "🎼 لیست پخش" : "🎼 Playlist";
            }
        }

        private void ShufflePlaylist()
        {
            var shuffledList = playlist.OrderBy(x => random.Next()).ToList();
            playlist = shuffledList;
            UpdatePlaylist();
        }

        private void RestoreOriginalOrder()
        {
            playlist = new List<string>(originalPlaylist);
            UpdatePlaylist();
        }

        private void PlaylistListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (playlistListBox.SelectedIndex >= 0)
            {
                currentTrackIndex = playlistListBox.SelectedIndex;
            }
        }

        private void PlaylistListBox_DoubleClick(object sender, EventArgs e)
        {
            if (playlistListBox.SelectedIndex >= 0)
            {
                currentTrackIndex = playlistListBox.SelectedIndex;
                PlayCurrentTrack();
            }
        }

        private void ProgressTrackBar_MouseDown(object sender, MouseEventArgs e)
        {
            progressTimer.Stop();
        }

        private void ProgressTrackBar_MouseUp(object sender, MouseEventArgs e)
        {
            if (audioFile != null)
            {
                double ratio = (double)progressTrackBar.Value / progressTrackBar.Maximum;
                audioFile.Position = (long)(audioFile.Length * ratio);
            }
            progressTimer.Start();
        }

        private void VolumeTrackBar_ValueChanged(object sender, EventArgs e)
        {
            if (outputDevice != null)
            {
                outputDevice.Volume = (float)volumeTrackBar.Value / 100f;
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            string[] mediaFiles = files.Where(IsMediaFile).ToArray();
            
            if (mediaFiles.Length > 0)
            {
                AddFilesToList(mediaFiles);
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Check for Ctrl+Key combinations first
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.O:
                        AddMusicButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.D:
                        AddFolderButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.S:
                        SavePlaylistButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.L:
                        LoadPlaylistButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.R:
                        RepeatButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.H:
                        ShuffleButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.V:
                        VideoModeButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.Delete:
                        RemoveMusicButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.N:
                        ClearPlaylistButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                // Single key shortcuts
                switch (e.KeyCode)
                {
                    case Keys.Space:
                        if (isPlaying) PauseButton_Click(this, EventArgs.Empty);
                        else PlayButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.Left:
                        PreviousButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.Right:
                        NextButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.Escape:
                        StopButton_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.Enter:
                        if (playlistListBox.SelectedIndex >= 0)
                        {
                            PlaylistListBox_DoubleClick(this, EventArgs.Empty);
                        }
                        e.Handled = true;
                        break;
                    case Keys.Up:
                        if (playlistListBox.SelectedIndex > 0)
                        {
                            playlistListBox.SelectedIndex--;
                        }
                        e.Handled = true;
                        break;
                    case Keys.Down:
                        if (playlistListBox.SelectedIndex < playlistListBox.Items.Count - 1)
                        {
                            playlistListBox.SelectedIndex++;
                        }
                        e.Handled = true;
                        break;
                    case Keys.Home:
                        if (playlistListBox.Items.Count > 0)
                        {
                            playlistListBox.SelectedIndex = 0;
                        }
                        e.Handled = true;
                        break;
                    case Keys.End:
                        if (playlistListBox.Items.Count > 0)
                        {
                            playlistListBox.SelectedIndex = playlistListBox.Items.Count - 1;
                        }
                        e.Handled = true;
                        break;
                    case Keys.F11:
                        ToggleFullscreen();
                        e.Handled = true;
                        break;
                }
            }
        }

        private void PlayCurrentTrack()
        {
            if (currentTrackIndex < 0 || currentTrackIndex >= playlist.Count) return;

            StopPlayback();

            try
            {
                string filePath = playlist[currentTrackIndex];
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                nowPlayingLabel.Text = isPersianLanguage ? $"🎵 در حال پخش: {fileName}" : $"🎵 Now Playing: {fileName}";
                playlistListBox.SelectedIndex = currentTrackIndex;

                if (IsVideoFile(filePath) && isVideoMode)
                {
                    PlayVideo(filePath);
                }
                else
                {
                    PlayAudio(filePath);
                }

                isPlaying = true;
                isPaused = false;
                progressTimer.Start();
                UpdateButtonStates();
                statusLabel.Text = isVideoMode ? 
                    (isPersianLanguage ? "در حال پخش ویدیو! 🎬" : "Playing video! 🎬") : 
                    (isPersianLanguage ? "در حال پخش موسیقی! 🎶" : "Playing music! 🎶");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error playing file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error playing file! ❌";
            }
        }

        private void PlayAudio(string filePath)
        {
            audioFile = new AudioFileReader(filePath);
            outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);
            outputDevice.Volume = (float)volumeTrackBar.Value / 100f;
            outputDevice.Play();
        }

        private void PlayVideo(string filePath)
        {
            try
            {
                // Try to use VLC first (if available)
                string vlcPath = FindVLC();
                if (!string.IsNullOrEmpty(vlcPath))
                {
                    PlayVideoWithVLC(filePath, vlcPath);
                    return;
                }

                // Fallback to Windows Media Player
                PlayVideoWithWMP(filePath);
            }
            catch
            {
                // If all else fails, use HTML5 video
                PlayVideoWithHTML5(filePath);
            }
        }

        private string FindVLC()
        {
            string[] possiblePaths = {
                @"C:\Program Files\VideoLAN\VLC\vlc.exe",
                @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe",
                @"C:\Program Files\VLC\vlc.exe",
                @"C:\Program Files (x86)\VLC\vlc.exe"
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        private void PlayVideoWithVLC(string filePath, string vlcPath)
        {
            try
            {
                if (videoProcess != null && !videoProcess.HasExited)
                {
                    videoProcess.Kill();
                    videoProcess.Dispose();
                }

                videoProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = vlcPath,
                        Arguments = $"--intf dummy --extraintf http --http-password vlc --http-port 8080 --fullscreen --no-video-title-show \"{filePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                videoProcess.Start();

                // Load VLC web interface
                string html = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { margin: 0; padding: 0; background: black; }
                        iframe { width: 100%; height: 100%; border: none; }
                    </style>
                </head>
                <body>
                    <iframe src=""http://localhost:8080"" allowfullscreen></iframe>
                </body>
                </html>";

                videoPlayer.DocumentText = html;
            }
            catch
            {
                PlayVideoWithWMP(filePath);
            }
        }

        private void PlayVideoWithWMP(string filePath)
        {
            try
            {
                // Use Windows Media Player via COM
                string html = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ margin: 0; padding: 0; background: black; }}
                        object {{ width: 100%; height: 100%; }}
                    </style>
                </head>
                <body>
                    <object classid=""CLSID:6BF52A52-394A-11d3-B153-00C04F79FAA6"" width=""100%"" height=""100%"">
                        <param name=""URL"" value=""{filePath.Replace("\\", "/")}"">
                        <param name=""autoStart"" value=""true"">
                        <param name=""uiMode"" value=""full"">
                    </object>
                </body>
                </html>";

                videoPlayer.DocumentText = html;
            }
            catch
            {
                PlayVideoWithHTML5(filePath);
            }
        }

        private void PlayVideoWithHTML5(string filePath)
        {
            // Create HTML5 video player as fallback
            string html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ margin: 0; padding: 0; background: black; }}
                    video {{ width: 100%; height: 100%; object-fit: contain; }}
                </style>
            </head>
            <body>
                <video controls autoplay>
                    <source src=""file:///{filePath.Replace("\\", "/")}"" type=""video/mp4"">
                    Your browser does not support the video tag.
                </video>
            </body>
            </html>";

            videoPlayer.DocumentText = html;
        }

        private void ResumePlayback()
        {
            if (outputDevice != null)
            {
                outputDevice.Play();
                isPlaying = true;
                isPaused = false;
                progressTimer.Start();
                UpdateButtonStates();
                statusLabel.Text = isPersianLanguage ? "ادامه پخش! ▶️" : "Resumed playing! ▶️";
            }
        }

        private void PausePlayback()
        {
            if (outputDevice != null)
            {
                outputDevice.Pause();
                isPlaying = false;
                isPaused = true;
                progressTimer.Stop();
                UpdateButtonStates();
                statusLabel.Text = isPersianLanguage ? "متوقف شد! ⏸️" : "Paused! ⏸️";
            }
        }

        private void StopPlayback()
        {
            if (outputDevice != null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
            }

            if (audioFile != null)
            {
                audioFile.Dispose();
                audioFile = null;
            }

            if (videoPlayer != null && isVideoMode)
            {
                videoPlayer.DocumentText = "";
            }

            // Stop video process if running
            if (videoProcess != null && !videoProcess.HasExited)
            {
                try
                {
                    videoProcess.Kill();
                    videoProcess.Dispose();
                }
                catch { }
                videoProcess = null;
            }

            isPlaying = false;
            isPaused = false;
            progressTimer.Stop();
            progressTrackBar.Value = 0;
            currentTimeLabel.Text = "00:00";
            UpdateButtonStates();
            statusLabel.Text = isPersianLanguage ? "متوقف شد! ⏹️" : "Stopped! ⏹️";
        }

        private void UpdatePlaylist()
        {
            playlistListBox.Items.Clear();
            for (int i = 0; i < playlist.Count; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(playlist[i]);
                string prefix = i == currentTrackIndex && isPlaying ? "▶ " : 
                               i == currentTrackIndex ? "⏸ " : "  ";
                playlistListBox.Items.Add($"{prefix}{fileName}");
            }
        }

        private void UpdateButtonStates()
        {
            playButton.Enabled = playlist.Count > 0 && (!isPlaying || isPaused);
            pauseButton.Enabled = isPlaying && !isPaused;
            stopButton.Enabled = isPlaying || isPaused;
            nextButton.Enabled = playlist.Count > 1;
            previousButton.Enabled = playlist.Count > 1;
            shuffleButton.Enabled = playlist.Count > 1;
            repeatButton.Enabled = playlist.Count > 0;
            videoModeButton.Enabled = playlist.Count > 0;
        }

        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (audioFile != null && outputDevice != null)
            {
                if (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    double progress = (double)audioFile.Position / audioFile.Length;
                    progressTrackBar.Value = (int)(progress * progressTrackBar.Maximum);

                    currentTimeLabel.Text = FormatTime(audioFile.CurrentTime);
                    totalTimeLabel.Text = FormatTime(audioFile.TotalTime);
                }
                else if (outputDevice.PlaybackState == PlaybackState.Stopped && isPlaying)
                {
                    // Track finished
                    if (isRepeatingOne)
                    {
                        PlayCurrentTrack();
                    }
                    else if (isRepeating)
                    {
                        NextButton_Click(this, EventArgs.Empty);
                    }
                    else if (currentTrackIndex < playlist.Count - 1)
                    {
                        NextButton_Click(this, EventArgs.Empty);
                    }
                    else
                    {
                        StopPlayback();
                        statusLabel.Text = "Playlist finished! 🎵";
                    }
                }
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            animationPhase += 0.1f;
            if (animationPhase > Math.PI * 2) animationPhase = 0;

            // Animate equalizer bars
            if (equalizerBars != null && isPlaying)
            {
                for (int i = 0; i < equalizerBars.Length; i++)
                {
                    float height = 10 + (float)(Math.Sin(animationPhase + i * 0.5) * 50);
                    height = Math.Max(10, Math.Min(100, height));
                    equalizerBars[i].Height = (int)height;
                    equalizerBars[i].Location = new Point(equalizerBars[i].Location.X, 120 - (int)height);
                }
            }

            // Animate background colors
            if (isPlaying)
            {
                currentColorIndex = (currentColorIndex + 1) % gradientColors.Length;
                nowPlayingLabel.ForeColor = gradientColors[currentColorIndex];
            }
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopPlayback();
            progressTimer?.Dispose();
            animationTimer?.Dispose();
            
            // Clean up video process
            if (videoProcess != null && !videoProcess.HasExited)
            {
                try
                {
                    videoProcess.Kill();
                    videoProcess.Dispose();
                }
                catch { }
            }
            
            base.OnFormClosing(e);
        }
    }
}