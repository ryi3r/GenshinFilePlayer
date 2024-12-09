namespace GenshinFilePlayer
{
    public partial class FindWindow : Form
    {
        public Dictionary<TreeNode, (string, string)> Entries = [];

        public FindWindow()
        {
            InitializeComponent();
        }

        private async void OnFindClick(object sender, EventArgs e)
        {
            using var loadWindow = new LoadingWindow()
            {
                Text = "Searching...",
            };
            loadWindow.label.Text = $"Searching...";
            loadWindow.Show();

            treeView.Nodes.Clear();

            var isCaseSensitive = findOnList.GetItemChecked(0);
            var byName = findOnList.GetItemChecked(1);
            var byHash = findOnList.GetItemChecked(2);

            var keyword = isCaseSensitive ? whatToFindBox.Text : whatToFindBox.Text.ToLowerInvariant();

            var iter = new List<AudioTree<Pck>> { MainWindow.GetInstance()!.Tree };

            await Task.Run(async () =>
            {
                var found = 0;
                var searched = 0;
                var mutex = new Mutex();

                while (iter.Count > 0)
                {
                    var tree = iter[^1];
                    iter.RemoveAt(iter.Count - 1);

                    var tasks = new List<Task>();

                    foreach (var value in tree.Children.Values)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            foreach (var (fsKey, fsValue) in value.FileSystem)
                            {
                                var alreadyFound = false;
                                if (byName)
                                {
                                    if ((isCaseSensitive ? fsValue.Name! : fsValue.Name!.ToLower()).Contains(keyword))
                                    {
                                        var curObj = Invoke(() => treeView.Nodes.Add($"Match by name: {fsValue.Name}"));
                                        Entries.Add(curObj, (value.LocalPath, fsKey));
                                        alreadyFound = true;
                                    }
                                }
                                if (byHash)
                                {
                                    if ((isCaseSensitive ? fsValue.SongHash! : fsValue.SongHash!.ToLower()).Contains(keyword) && !alreadyFound)
                                    {
                                        var curObj = Invoke(() => treeView.Nodes.Add($"Match by SHA512: {fsValue.Name} (Hash: {fsValue.SongHash}"));
                                        Entries.Add(curObj, (value.LocalPath, fsKey));
                                        alreadyFound = true;
                                    }
                                }
                                mutex.WaitOne();
                                try
                                {
                                    if (alreadyFound)
                                        found++;
                                    searched++;
                                }
                                finally
                                {
                                    mutex.ReleaseMutex();
                                }
                            }
                        }));
                    }
                    foreach (var task in tasks)
                    {
                        await task;
                        task.Dispose();
                    }
                    tasks.Clear();
                    Invoke(() => loadWindow.label.Text = $"Searching... Found {found} matches, searched on {searched} entries.");
                }
            });

            loadWindow.Close();
            loadWindow.Dispose();
        }

        private void OnTreeViewDoubleClick(object sender, EventArgs e)
        {
            var entry = Entries[treeView.SelectedNode!];
            var inst = MainWindow.GetInstance()!;
            foreach (var (key, value) in inst.MusicResolvers)
            {
                if (value.Item1 == entry.Item1 && value.Item2 == entry.Item2)
                {
                    inst.treeView.SelectedNode = key;
                    inst.treeView.Select();
                    inst.BringToFront();
                    break;
                }
            }
        }
    }
}
