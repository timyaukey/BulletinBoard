using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BulletinBoard
{
    public class NoteSystem
    {
        public List<NoteFolder> Folders;
        public bool NeedsRefresh;

        public readonly string RootFolder;
        public readonly TabControl MainTab;
        public NoteFolder CurrentFolder;
        private StartTimerDelegate _StartRefreshTimer;
        private EventHandler _ItemDoubleClickHandler;
        private FileSystemWatcher _Watcher;
        public object LockObject;

        public delegate void StartTimerDelegate();

        public NoteSystem(string rootFolder, TabControl mainTab, 
            StartTimerDelegate startRefreshTimer, EventHandler itemDoubleClickHandler)
        {
            RootFolder = rootFolder;
            MainTab = mainTab;
            _StartRefreshTimer = startRefreshTimer;
            _ItemDoubleClickHandler = itemDoubleClickHandler;
            LockObject = new object();
        }

        public string GetFullPath(string relativeName)
        {
            return Path.Combine(RootFolder, relativeName);
        }

        public void RequestRefresh()
        {
            NeedsRefresh = true;
        }

        public void RefreshIfNeeded()
        {
            Monitor.Enter(this.LockObject);
            try
            {
                if (NeedsRefresh)
                {
                    LoadFolders();
                    if (Folders.Count > 0)
                    {
                        CurrentFolder = Folders[0];
                        CurrentFolder.LoadFiles();
                    }
                }
                else
                {
                    TabPage tabPage = MainTab.SelectedTab;
                    if (tabPage != null)
                    {
                        CurrentFolder = (NoteFolder)tabPage.Tag;
                        if (CurrentFolder.NeedsRefresh)
                            CurrentFolder.LoadFiles();
                    }
                }
            }
            finally
            {
                Monitor.Exit(this.LockObject);
            }
        }

        public void RefreshCurrentFolder()
        {
            CurrentFolder.RequestRefresh();
            CurrentFolder.RefreshIfNeeded();
        }

        public void LoadFolders()
        {
            int tabIndex = 0;
            MainTab.TabPages.Clear();
            MainTab.Controls.Clear();
            Folders = new List<NoteFolder>();
            DirectoryInfo rootInfo = new DirectoryInfo(RootFolder);
            foreach(DirectoryInfo tabFolder in rootInfo.EnumerateDirectories())
            {
                string dirName = Path.GetFileName(tabFolder.FullName);

                TabPage tabPage = new TabPage();
                string labelText = dirName;
                int dashIndex = labelText.IndexOf("-");
                if (dashIndex > 0)
                {
                    int dummySortKey;
                    if (int.TryParse(labelText.Substring(0, dashIndex), out dummySortKey))
                    {
                        labelText = labelText.Substring(dashIndex + 1);
                    }
                }
                tabPage.Text = labelText;
                //tabPage.Size = new System.Drawing.Size(500, 400);
                //tabPage.BorderStyle = BorderStyle.None;
                tabPage.TabIndex = tabIndex++;

                ListView lvw = new ListView();
                lvw.View = View.Details;
                tabPage.Controls.Add(lvw);
                MainTab.Controls.Add(tabPage);
                lvw.Dock = DockStyle.Fill;
                lvw.BorderStyle = BorderStyle.None;
                lvw.FullRowSelect = true;
                lvw.HideSelection = false;
                lvw.DoubleClick += _ItemDoubleClickHandler;
                lvw.Visible = true;

                int otherColumnWidths = 110 + 110;
                lvw.Columns.Add("Description", lvw.ClientSize.Width - otherColumnWidths - 10);
                lvw.Columns.Add("Created On", 110);
                lvw.Columns.Add("Updated On", 110);
                //lvw.Location = new System.Drawing.Point(5, 5);
                //lvw.Size = new System.Drawing.Size(500, 300);

                NoteFolder folder = new NoteFolder(this);
                tabPage.Tag = folder;
                folder.System = this;
                folder.DirName = dirName;
                folder.LabelText = labelText;
                folder.Tab = tabPage;
                folder.LvwFiles = lvw;
                folder.RequestRefresh();
                Folders.Add(folder);
            }
            NeedsRefresh = false;
        }

        public void StartWatching()
        {
            _Watcher = new FileSystemWatcher(RootFolder);
            _Watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.FileName;
            _Watcher.IncludeSubdirectories = true;
            _Watcher.Changed += FileWatcherNotification;
            _Watcher.Created += FileWatcherNotification;
            _Watcher.Deleted += FileWatcherNotification;
            _Watcher.Renamed += FileWatcherNotification;
            _Watcher.EnableRaisingEvents = true;
        }

        public void FileWatcherNotification(object sender, FileSystemEventArgs e)
        {
            RequestRefreshOfMatchingFolder(e.Name, e.ChangeType);
        }

        public void FileWatcherNotification(object sender, RenamedEventArgs e)
        {
            RequestRefreshOfMatchingFolder(e.OldName, e.ChangeType);
        }

        private void RequestRefreshOfMatchingFolder(string relativePath, WatcherChangeTypes changeType)
        {
            int slashIndex = relativePath.IndexOf("\\");
            if (changeType == WatcherChangeTypes.Changed)
            {
                // "Changed" events are fired for many reasons, generally for the parent
                // folder when something happens to a file. We only care about "Changed"
                // events for files.
                if (slashIndex > 0)
                {
                    string tabDirName = relativePath.Substring(0, slashIndex);
                    foreach (NoteFolder folder in Folders)
                    {
                        if (tabDirName.Equals(folder.DirName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            folder.RequestRefresh();
                            break;
                        }
                    }
                }
            }
            else
            {
                // Change types "Created", "Deleted", "Renamed".
                if (slashIndex > 0)
                {
                    // This event refers to something INSIDE one of the tab folders.
                    string tabDirName = relativePath.Substring(0, slashIndex);
                    foreach (NoteFolder folder in Folders)
                    {
                        if (tabDirName.Equals(folder.DirName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            folder.RequestRefresh();
                            break;
                        }
                    }
                }
                else
                {
                    // A tab folder was created, renamed or deleted.
                    RequestRefresh();
                }
            }
            MainTab.Invoke(_StartRefreshTimer);
        }
    }
}
