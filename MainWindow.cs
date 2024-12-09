using System.Collections;
using LibVLCSharp.Shared;
using Ookii.Dialogs.WinForms;

namespace GenshinFilePlayer
{
    public class AudioTree<T>
    {
        public readonly Dictionary<string, T> Children = [];
        public readonly List<AudioTree<T>> Items = [];
        public string Name = "";
        public bool IsActualFolder;

        public AudioTree<T>? Parent;

        // TODO: Allow moving around Children (aka. custom object placement)
    }

    public partial class MainWindow : Form
    {
        static MainWindow? Instance = null;

        public readonly Dictionary<string, dynamic> Folders = [];
        public readonly Dictionary<string, Pck> Pcks = [];
        public readonly Dictionary<TreeNode, string> Resolvers = [];

        public readonly Dictionary<TreeNode, (string, string)> MusicResolvers = [];

        public static readonly HashSet<TreeNode> ActualFolders = [];

        static readonly LibVLC LibVlc = new();
        readonly MediaPlayer MediaPlayer = new(LibVlc);
        StreamMediaInput? StreamMediaInput;
        MemoryStream? AudioStream;

        public string? SelectedSong;
        public string? LastSelectedPck;

        public string? PlayingSong;
        public string? PlayingSelectedPck;

        public bool AudioIsPlaying;
        public bool AudioIsPaused;

        public Mutex Mutex = new();

        public readonly AudioTree<Pck> Tree = new();

        public bool ModifyMediaPlayer = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        public static MainWindow? GetInstance()
        {
            return Instance;
        }

        private void OnWindowLoad(object sender, EventArgs e)
        {
            Instance = this;

            treeView.TreeViewNodeSorter = new NodeSorter();

            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(50);
                    try
                    {
                        Invoke(() => OnTimerTick());
                    }
                    catch (ObjectDisposedException)
                    {
                        return; // This means that the MainWindow was closed. Stopping.
                    }
                }
            });
        }

        private void SelectTreeView(object sender, TreeViewEventArgs e)
        {
            var isMusicResolver = false;
            (string, string) keyLong = ("", "");
            if (!Resolvers.TryGetValue(treeView.SelectedNode!, out string? key))
            {
                if (!MusicResolvers.TryGetValue(treeView.SelectedNode!, out keyLong))
                    return;
                isMusicResolver = true;
            }
            var pck = Pcks[isMusicResolver ? keyLong.Item1 : key!];
            //var node = Folders[key];

            SelectedSong = null;

            if (!pck.IsParsed)
            {
                pck.Read();
                foreach (var (fsKey, fsValue) in pck.FileSystem)
                {
                    var objPath = key!;
                    foreach (var folder in fsValue.Folders)
                    {
                        var lastObjPath = objPath;
                        if (objPath.Length > 0)
                            objPath += "/";
                        objPath += folder;
                        if (!Folders.ContainsKey(objPath))
                            Folders.Add(objPath, Folders[lastObjPath].Nodes.Add(folder));
                    }
                    var curObj = Folders[objPath].Nodes.Add(fsValue.Name);
                    Folders.Add($"{objPath}/{fsValue.Name}", curObj);
                    MusicResolvers.Add(curObj, (key, fsKey));
                }
            }
            else if (isMusicResolver)
            {
                SelectedSong = keyLong.Item2;
                LastSelectedPck = keyLong.Item1;
            }
        }

        private void OnTimerTick()
        {
            if (!MediaPlayer.IsPlaying && AudioIsPlaying && !AudioIsPaused)
            {
                MediaPlayer.Stop();
                MediaPlayer.Play();
            }
            playButton.Text = AudioIsPlaying && SelectedSong == PlayingSong && LastSelectedPck == PlayingSelectedPck ? "Stop" : "Play";
            pauseButton.Text = AudioIsPaused ? "Resume" : "Pause";

            if (PlayingSong == null && SelectedSong == null)
            {
                panelHandle.Enabled = false;
                return;
            }
            panelHandle.Enabled = true;

            var pck = Pcks[(SelectedSong == PlayingSong && LastSelectedPck == PlayingSelectedPck ? (PlayingSelectedPck ?? LastSelectedPck) : LastSelectedPck)!];
            var song = pck.FileSystem[(SelectedSong == PlayingSong && LastSelectedPck == PlayingSelectedPck ? (PlayingSong ?? SelectedSong) : SelectedSong)!];

            //MediaPlayer.Position
            var songLength = MediaPlayer.Length;
            var possibleName = song.GetPossibleName();

            var lastModifyMediaPlayer = ModifyMediaPlayer;
            ModifyMediaPlayer = false;
            songData.Text = $"{(possibleName ?? song.Name)}: {(songLength == -1 || (SelectedSong != PlayingSong || LastSelectedPck != PlayingSelectedPck) ?
                    "--:--" : TimeMsToString((long)(MediaPlayer.Position * songLength)))} / {(songLength == -1 || (SelectedSong != PlayingSong || LastSelectedPck != PlayingSelectedPck) ?
                    "--:--" : TimeMsToString(songLength))}\r\nSHA512: {song.SongHash}";
            songPosition.Maximum = (int)Math.Max(Math.Floor(songLength / 100.0d), 0.0d);
            songPosition.Value = (int)(Math.Max(Math.Floor(songLength / 100.0d), 0.0d) * Math.Max(MediaPlayer.Position, 0));
            ModifyMediaPlayer = lastModifyMediaPlayer;
        }

        public static string TimeMsToString(long time)
        {
            var sec = (int)Math.Floor(time / 1000.0d) % 60;
            var min = (int)Math.Floor(time / 60000.0d) % 60;
            var hour = (int)Math.Floor(time / 3600000.0d) % 24;
            var day = (int)Math.Floor(time / 86400000.0d);

            if (day > 0)
                return $"{day:00}:{hour:00}:{min:00}:{sec:00}";
            if (hour > 0)
                return $"{hour:00}:{min:00}:{sec:00}";
            return $"{min:00}:{sec:00}";
        }

        private void OnPlayClick(object sender, EventArgs e)
        {
            var isStopped = false;
            if (PlayingSong != null && PlayingSelectedPck != null && SelectedSong == PlayingSong && LastSelectedPck == PlayingSelectedPck)
            {
                MediaPlayer.Stop();
                MediaPlayer.Media?.Dispose();
                StreamMediaInput?.Dispose();
                AudioStream?.Dispose();
                StreamMediaInput = null;
                AudioStream = null;
                MediaPlayer.Media = null;
                PlayingSong = null;
                PlayingSelectedPck = null;
                AudioIsPlaying = false;
                isStopped = true;
            }
            if (!isStopped)
            {
                AudioIsPlaying = true;
                AudioIsPaused = false;
                if (MediaPlayer.IsPlaying)
                    MediaPlayer.Stop();

                PlayingSong = SelectedSong!;
                PlayingSelectedPck = LastSelectedPck!;
                #region Clean up
                MediaPlayer.Media?.Dispose();
                MediaPlayer.Media = null;
                StreamMediaInput?.Dispose();
                StreamMediaInput = null;
                AudioStream?.Dispose();
                #endregion
                AudioStream = new MemoryStream(Pcks[LastSelectedPck!].FileSystem[SelectedSong!].GetWav());
                StreamMediaInput = new StreamMediaInput(AudioStream);
                MediaPlayer.Media = new Media(LibVlc, StreamMediaInput);

                MediaPlayer.Play();
            }
        }

        private void SongPositionOnValueChange(object sender, EventArgs e)
        {
            if (!ModifyMediaPlayer)
                return; // TODO: Does this actually work?
            if (MediaPlayer.IsPlaying && MediaPlayer.IsSeekable && songPosition.Value != (int)(songPosition.Maximum * Math.Max(MediaPlayer.Position, 0)))
                MediaPlayer.SeekTo(TimeSpan.FromMilliseconds(songPosition.Value * 100.0d));
        }

        private void MouseDoubleClickTreeView(object sender, MouseEventArgs e)
        {
            if (treeView.SelectedNode != null)
            {
                if (MusicResolvers.ContainsKey(treeView.SelectedNode))
                    OnPlayClick(sender, e);
            }
        }

        private void OnPauseClick(object sender, EventArgs e)
        {
            if (AudioIsPlaying && MediaPlayer.CanPause)
            {
                AudioIsPaused = !AudioIsPaused;
                MediaPlayer.SetPause(AudioIsPaused);
            }
        }

        private async void OnExportClick(object sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "FASTEST - Wem File (*.wem)|*.wem|FAST - Wav File (*.wav)|*.wav|SLOW - Ogg File (*.ogg)|*.ogg"
            };
            var pck = Pcks[(SelectedSong == PlayingSong && LastSelectedPck == PlayingSelectedPck ? (PlayingSelectedPck ?? LastSelectedPck) : LastSelectedPck)!];
            var song = pck.FileSystem[(SelectedSong == PlayingSong && LastSelectedPck == PlayingSelectedPck ? (PlayingSong ?? SelectedSong) : SelectedSong)!];
            dialog.FileName = song.Name ?? song.GetPossibleName()!;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                await using var f = dialog.OpenFile();

                using var loadWindow = new LoadingWindow()
                {
                    Text = "Saving file...",
                };
                loadWindow.label.Text = $"Saving file \"{dialog.FileName}\"...";
                loadWindow.Show();

                await Task.Run(() =>
                {
                    switch (dialog.FilterIndex)
                    {
                        case 1: // Wem
                            f.Write(song.GetRaw());
                            break;
                        case 2: // Wav
                            f.Write(song.GetWav());
                            break;
                        case 3: // Ogg
                            f.Write(song.GetOgg());
                            break;
                    }
                });
                f.Flush();
                f.Close();
                loadWindow.Close();
            }
        }

        private void OnGoToFileClick(object sender, EventArgs e)
        {
            if (PlayingSelectedPck == null || PlayingSong == null)
                return;
            foreach (var (key, value) in MusicResolvers)
            {
                if (value.Item1 == PlayingSelectedPck && value.Item2 == PlayingSong)
                {
                    treeView.SelectedNode = key;
                    treeView.Select();
                    break;
                }
            }
        }

        private async void OnExportAllClick(object sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "FASTEST - Wem Files (*.wem)|*|FAST - Wav File (*.wav)|*|SLOW - Ogg Files (*.ogg)|*",
                Title = "Select folder to save..."
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {

                using var loadWindow = new LoadingWindow()
                {
                    Text = "Saving file...",
                };
                loadWindow.label.Text = $"Preparing folder \"{dialog.FileName}\"...";
                loadWindow.Show();

                await Task.Run(async () =>
                {
                    var folders = new Dictionary<AudioTree<Pck>, string>();
                    var queue = new List<AudioTree<Pck>> { Tree };
                    var targetExt = "";
                    switch (dialog.FilterIndex)
                    {
                        case 2: // Wav
                            targetExt = ".wav";
                            break;
                        case 3: // Ogg
                            targetExt = ".ogg";
                            break;
                    }
                    var filesProcessed = 0;
                    while (queue.Count > 0)
                    {
                        var tree = queue[0];
                        queue.RemoveAt(0);
                        var fPath = $"{(tree.Parent == null ? Path.GetFullPath(dialog.FileName) : folders[tree.Parent])}/{tree.Name}/";
                        folders[tree] = fPath;
                        if (!Directory.Exists(fPath))
                            Directory.CreateDirectory(fPath);
                        foreach (var item in tree.Items)
                            queue.Add(item);
                        foreach (var (_, value) in tree.Children)
                        {
                            var basePath = $"{fPath}{value.Name}/";
                            if (!Directory.Exists(basePath))
                                Directory.CreateDirectory(basePath);
                            var tasks = new List<Task>();
                            foreach (var fsValue in value.FileSystem.Values)
                            {
                                var nfPath = basePath;
                                foreach (var dir in fsValue.Folders)
                                {
                                    nfPath += $"/{dir}";
                                    if (!Directory.Exists(nfPath))
                                        Directory.CreateDirectory(nfPath);
                                }
                                tasks.Add(Task.Run(() =>
                                {
                                    {
                                        using var f = File.Create($"{nfPath}/{fsValue.Name}{targetExt}");
                                        switch (dialog.FilterIndex)
                                        {
                                            case 1: // Wem
                                                f.Write(fsValue.GetRaw());
                                                break;
                                            case 2: // Wav
                                                f.Write(fsValue.GetWav());
                                                break;
                                            case 3: // Ogg
                                                f.Write(fsValue.GetOgg());
                                                break;
                                        }
                                        f.Flush();
                                        f.Close();
                                    }
                                    {
                                        using var f = File.CreateText($"{nfPath}/{fsValue.Name}.hash");
                                        f.Write(fsValue.SongHash!);
                                        f.Flush();
                                        f.Close();
                                    }
                                    filesProcessed++;
                                    Invoke(() => loadWindow.label.Text = $"Saving \"{fsValue.Name}{targetExt}\", {filesProcessed} file(s) processed...");
                                }));
                            }
                            foreach (var task in tasks)
                            {
                                await task;
                                task.Dispose();
                            }
                            tasks.Clear();
                        }
                    }
                });
                loadWindow.Close();
            }
        }

        private async void OnLoadFolderClick(object sender, EventArgs e)
        {
            using var dialog = new VistaFolderBrowserDialog()
            {
                Description = "Select a folder...",
                UseDescriptionForTitle = true,
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                using var loadWindow = new LoadingWindow()
                {
                    Text = "Loading folder...",
                };
                loadWindow.label.Text = $"Loading folder \"{dialog.SelectedPath}\"...";
                loadWindow.Show();
                await Task.Run(async () =>
                {
                    await LoadFolder(Path.GetFullPath(dialog.SelectedPath), loadWindow);
                    Invoke(() => loadWindow.label.Text = "Reloading tree layout...");
                    await ReloadTree();
                });
                loadWindow.Close();
            }
        }

        private async void OnLoadFileClick(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog()
            {
                Title = "Select a file...",
            };
            if (dialog.ShowDialog() == DialogResult.OK && Path.GetExtension(dialog.FileName) == ".pck")
            {
                using var loadWindow = new LoadingWindow()
                {
                    Text = "Loading file...",
                };
                loadWindow.label.Text = $"Loading file \"{dialog.FileName}\"...";
                loadWindow.Show();
                await Task.Run(async () =>
                {
                    var path = Path.GetFullPath(dialog.FileName);
                    LoadFile(Path.GetFileName(path), "", path, Tree);
                    Invoke(() => loadWindow.label.Text = "Reloading tree layout...");
                    await ReloadTree();
                });
                loadWindow.Close();
            }
        }

        private void OnFindClick(object sender, EventArgs e)
        {
            var findWindow = new FindWindow();
            findWindow.Show();
            findWindow.FormClosed += (object? sender, FormClosedEventArgs e) => findWindow.Dispose();
        }

        public async Task LoadFolder(string basePath, LoadingWindow? loadWindow = null)
        {
            var filesLeft = 0;
            var data = new List<(string, string, string, AudioTree<Pck>)>();
            var folders = new List<(string, string, string, AudioTree<Pck>)>();
            //var basePath = $"{Directory.GetParent(Environment.ProcessPath!)!.FullName}/AudioAssets";
            {
                var dirData = new DirectoryInfo(basePath);
                foreach (var _file in dirData.GetFiles())
                {
                    data.Add(("", _file.Name, "", Tree));
                    filesLeft++;
                }
                foreach (var _file in dirData.GetDirectories())
                    data.Add(("", _file.Name, "", Tree));
            }
            var tasks = new List<Task>();
            while (folders.Count > 0 || data.Count > 0)
            {
                foreach (var (path, file, _, tree) in folders)
                {
                    void updateFiles()
                    {
                        if (loadWindow != null)
                            loadWindow.label.Text = $"Loading \"{(path.Length > 0 ? (path + "/") : "")}{file}\", {filesLeft} file(s) left...";
                    }
                    var fPath = $"{basePath}/{path}/{file}";
                    foreach (var task in tasks)
                    {
                        await task;
                        task.Dispose();
                    }
                    tasks.Clear();
                    var dirData = new DirectoryInfo(fPath);
                    var nTree = new AudioTree<Pck> { Parent = tree, Name = file, IsActualFolder = true };
                    tree.Items.Add(nTree);
                    foreach (var _file in dirData.GetFiles())
                    {
                        data.Add(($"{(path.Length > 0 ? (path + "/") : "")}{file}", _file.Name, file, nTree));
                        Mutex.WaitOne();
                        filesLeft++;
                        Mutex.ReleaseMutex();
                    }
                    foreach (var _file in dirData.GetDirectories())
                        data.Add(($"{(path.Length > 0 ? (path + "/") : "")}{file}", _file.Name, file, nTree));
                    Invoke(updateFiles);
                }
                folders.Clear();
                while (data.Count > 0)
                {
                    var (path, file, lFile, tree) = data[^1];
                    var fPath = $"{basePath}/{path}/{file}";
                    data.RemoveAt(data.Count - 1);
                    //Log.Write($"{path}, {file}, {lFile}");
                    //TreeNode curObj = Invoke(() => Folders[$"{path}"].Nodes.Add(file));
                    //Folders.Add($"{(path.Length > 0 ? (path + "/") : "")}{file}", curObj);
                    if (Directory.Exists(fPath))
                    {
                        //tree.IsActualFolder = true;
                        folders.Add((path, file, lFile, tree));
                        //ActualFolders.Add(curObj);
                    }
                    else if (File.Exists(fPath) && Path.GetExtension(fPath) == ".pck")
                    {
                        void updateFiles()
                        {
                            if (loadWindow != null)
                                loadWindow.label.Text = $"Loading \"{(path.Length > 0 ? (path + "/") : "")}{file}\", {filesLeft} file(s) left...";
                        }
                        tasks.Add(Task.Run(() =>
                        {
                            LoadFile(file, lFile, fPath, tree);
                            Mutex.WaitOne();
                            filesLeft--;
                            Mutex.ReleaseMutex();
                            Invoke(updateFiles);
                        }));
                    }
                }
            }
            foreach (var task in tasks)
            {
                await task;
                task.Dispose();
            }
            tasks.Clear();
        }

        public void LoadFile(string file, string lFile, string fPath, AudioTree<Pck> tree)
        {
            var pck = new Pck(fPath)
            {
                Name = file,
                LocalPath = $"{(lFile.Length > 0 ? (lFile + "/") : "")}{file}",
            };
            // Do not cache when reading Pck files, this operation is very ram expensive!
            pck.Read();
            Mutex.WaitOne();
            try
            {
                Pcks.Add(pck.LocalPath, pck);
                //Resolvers.Add(curObj, pck.LocalPath);
                tree.Children.Add(file, pck);
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
        }

        public void WriteLog(string message)
        {
            Invoke(() =>
            {
                var split = (logTextBox.Text + (logTextBox.Text.Length > 0 ? "\r\n" : "") + message).Split("\r\n");
                // The horizontal scrollbar takes 1 line away, so from 9 we transition to 8 lines
                logTextBox.Text = string.Join("\r\n", split[Math.Max(split.Length - 8, 0)..split.Length]);
                //logTextBox.Select(logTextBox.Text.Length, 0); // Do we need this?
            });
        }

        public async Task ReloadTree()
        {
            Folders.Clear();
            Folders.Add("", treeView);

            MusicResolvers.Clear();
            Invoke(() => treeView.Nodes.Clear());

            var target = new List<AudioTree<Pck>> { Tree };
            var tasks = new List<Task>();

            while (target.Count > 0)
            {
                var tree = target[0];
                target.RemoveAt(0);

                dynamic? curObj = null;
                if (tree.Parent != null)
                {
                    curObj = Invoke(() =>
                    {
                        var val = Folders[tree.Parent.Name].Nodes.Add(tree.Name); // Does this need to go in a Mutex?
                        // Mutexing this kills the program, quite literally, uhh. TODO: Fix this?
                        /*Mutex.WaitOne();
                        try*/
                        {
                            if (tree.IsActualFolder && !ActualFolders.Contains(val))
                                ActualFolders.Add(val); // TODO: Fix this because for SOME reason it doesn't work :D
                                                        // NOTE: For some reason it fixed itself after threading it up, huh.
                            if (!Folders.ContainsKey(tree.Name))
                                Folders.Add(tree.Name, val);
                        }
                        /*finally
                        {
                            Mutex.ReleaseMutex();
                        }*/
                        return val;
                    });
                }
                else
                    curObj = Folders[tree.Name];

                //Log.Write($"Loading tree: {tree.Name}");
                foreach (var (key, value) in tree.Children)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        /*Log.Write($"Processing file: {key}");
                        var timer = new Stopwatch();
                        timer.Start();*/
                        /*if (tree.Parent != null) // Make sure it really is a TreeNode, not TreeView
                            Resolvers.Add(curObj, value.LocalPath);*/
                        Mutex.WaitOne();
                        try
                        {
                            if (!Folders.ContainsKey(value.LocalPath))
                                Folders.Add(value.LocalPath, Invoke(() => curObj.Nodes.Add(value.Name)));
                        }
                        finally
                        {
                            Mutex.ReleaseMutex();
                        }
                        foreach (var (fsKey, fsValue) in value.FileSystem)
                        {
                            //Log.Write($"Processing tree child file: {fsKey}");
                            var objPath = value.LocalPath;
                            foreach (var folder in fsValue.Folders)
                            {
                                var lastObjPath = objPath;
                                if (objPath.Length > 0)
                                    objPath += "/";
                                objPath += folder;
                                Mutex.WaitOne();
                                try
                                {
                                    if (!Folders.ContainsKey(objPath))
                                        Folders.Add(objPath, Invoke(() => Folders[lastObjPath].Nodes.Add(folder)));
                                }
                                finally
                                {
                                    Mutex.ReleaseMutex();
                                }
                            }
                            var treeObj = Invoke(() => Folders[objPath].Nodes.Add(fsValue.Name)); // Does this need to go in a Mutex?
                            //Folders.Add($"{objPath}/{fsValue.Name}", treeObj);
                            Mutex.WaitOne();
                            try
                            {
                                MusicResolvers.Add(treeObj, (value.LocalPath, fsKey));
                            }
                            finally
                            {
                                Mutex.ReleaseMutex();
                            }
                        }
                        /*timer.Stop();
                        Log.Write($"Time taken to process file {key}: {timer.Elapsed.ToString(@"hh\:mm\:ss\.fff")}");*/
                    }));
                }

                foreach (var subTree in tree.Items)
                    target.Add(subTree);
            }

            foreach (var task in tasks)
            {
                await task;
                task.Dispose();
            }
            tasks.Clear();
        }

        private async void OnClearAllClick(object sender, EventArgs e)
        {
            using var loadWindow = new LoadingWindow()
            {
                Text = "Clearing data...",
            };
            loadWindow.label.Text = $"Clearing data...";
            loadWindow.Show();
            await Task.Run(async () =>
            {
                SelectedSong = null;
                PlayingSong = null;
                PlayingSelectedPck = null;
                LastSelectedPck = null;
                Folders.Clear();
                Pcks.Clear();
                Tree.Items.Clear();
                Tree.Children.Clear();
                AudioIsPlaying = true;
                AudioIsPaused = false;
                if (MediaPlayer.IsPlaying)
                    MediaPlayer.Stop();
                #region Clean up
                MediaPlayer.Media?.Dispose();
                MediaPlayer.Media = null;
                StreamMediaInput?.Dispose();
                StreamMediaInput = null;
                AudioStream?.Dispose();
                AudioStream = null;
                #endregion
                Folders.Clear();
                Resolvers.Clear();
                MusicResolvers.Clear();
                ActualFolders.Clear();
                Pck.ClearHashes();
                Invoke(() => loadWindow.label.Text = "Reloading tree layout...");
                await ReloadTree();
                // Uhhh, yeah.
                GC.WaitForPendingFinalizers();
                GC.Collect();
            });
            loadWindow.Close();
        }
    }

    public class NodeSorter : IComparer
    {
        public int Compare(object? x, object? y)
        {
            var tx = x as TreeNode;
            var ty = y as TreeNode;

            var isFolderX = MainWindow.ActualFolders.Contains(tx!);
            var isFolderY = MainWindow.ActualFolders.Contains(ty!);

            if (isFolderX && !isFolderY)
                return -1;
            if (!isFolderX && isFolderY)
                return 1;

            return NaturalCompare(tx!.Text, ty!.Text);
        }

        private static int NaturalCompare(string str1, string str2)
        {
            int i = 0, ii = 0;

            while (i < str1.Length && i < str2.Length)
            {
                char char1 = str1[i];
                char char2 = str2[i];

                if (char.IsDigit(char1) && char.IsDigit(char2))
                {
                    var num1 = long.Parse(ExtractNumber(str1, ref i));
                    var num2 = long.Parse(ExtractNumber(str2, ref ii));

                    if (num1 != num2)
                        return num1.CompareTo(num2);
                }
                else
                {
                    int result = char1.CompareTo(char2);
                    if (result != 0)
                        return result;

                    i++;
                    ii++;
                }
            }

            return str1.Length.CompareTo(str2.Length);
        }

        private static string ExtractNumber(string str, ref int index)
        {
            int start = index;
            while (index < str.Length && char.IsDigit(str[index]))
                index++;
            return str[start..index];
        }
    }
}
