using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BulletinBoard
{
    public class NoteFolder
    {
        public NoteSystem System;
        public List<NoteFile> Files;
        public string LabelText;
        public string DirName;
        public bool NeedsRefresh;
        public TabPage Tab;
        public ListView LvwFiles;

        public NoteFolder(NoteSystem system)
        {
            System = system;
        }

        public string GetFullPath(string relativeName)
        {
            return System.GetFullPath(Path.Combine(DirName, relativeName));
        }

        public string GetFullPath()
        {
            return System.GetFullPath(DirName);
        }

        public string GetArchiveDirPath()
        {
            string archiveDirPath = System.CurrentFolder.GetFullPath("Archive");
            if (!Directory.Exists(archiveDirPath))
                Directory.CreateDirectory(archiveDirPath);
            return archiveDirPath;
        }

        public string GetConfigDirPath()
        {
            string configDirPath = System.CurrentFolder.GetFullPath("Config");
            if (!Directory.Exists(configDirPath))
                Directory.CreateDirectory(configDirPath);
            return configDirPath;
        }

        public string GetConfigFilePath(string fileName)
        {
            return Path.Combine(GetConfigDirPath(), fileName);
        }

        public void RequestRefresh()
        {
            NeedsRefresh = true;
        }

        public void RefreshIfNeeded()
        {
            Monitor.Enter(System.LockObject);
            try
            {
                if (NeedsRefresh)
                    LoadFiles();
            }
            finally
            {
                Monitor.Exit(System.LockObject);
            }
        }
        
        public void LoadFiles()
        {
            LvwFiles.Items.Clear();
            Files = new List<NoteFile>();
            DirectoryInfo dir = new DirectoryInfo(GetFullPath());
            foreach(FileInfo file in dir.EnumerateFiles())
            {
                if (file.Extension.ToLower()==".txt")
                {
                    NoteFile noteFile = new NoteFile(System, this);
                    noteFile.BareFileName = file.Name;
                    noteFile.LabelText = file.Name;
                    noteFile.CreatedAt = file.CreationTime;
                    noteFile.ModifiedAt = file.LastWriteTime;
                    ListViewItem item = new ListViewItem(new string[] {
                        noteFile.LabelText,
                        noteFile.CreatedAt.ToString("MM/dd/yy hh:mmtt"),
                        noteFile.ModifiedAt.ToString("MM/dd/yy hh:mmtt")
                    });
                    item.Tag = noteFile;
                    LvwFiles.Items.Add(item);
                }
            }
            NeedsRefresh = false;
        }
    }
}
