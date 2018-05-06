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
            string archiveDirPath = /*System.CurrentFolder.*/GetFullPath("Archive");
            if (!Directory.Exists(archiveDirPath))
                Directory.CreateDirectory(archiveDirPath);
            return archiveDirPath;
        }

        public string GetConfigDirPath()
        {
            string configDirPath = /*System.CurrentFolder.*/GetFullPath("Config");
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
                    LoadFolder();
            }
            finally
            {
                Monitor.Exit(System.LockObject);
            }
        }

        public void LoadFolder()
        {
            SetTabName();
            LoadColumnDefs();
            ConfigureColumns();
            LoadFiles();
        }

        public void SetTabName()
        {
            int noteCount = GetNoteCount();
            Tab.Text = this.LabelText + (noteCount > 0 ? " (" + noteCount + ")" : "");
        }

        public int GetNoteCount()
        {
            int noteCount = 0;
            DirectoryInfo dir = new DirectoryInfo(GetFullPath());
            foreach (FileInfo file in dir.EnumerateFiles())
            {
                if (IsNoteFile(file))
                {
                    noteCount++;
                }
            }
            return noteCount;
        }

        private void LoadColumnDefs()
        {
            ExtraColumns = new List<ExtraColumnDef>();
            string configFileName = GetConfigFilePath("ColumnDefs.txt");
            if (File.Exists(configFileName))
            {
                using (TextReader reader = new StreamReader(configFileName))
                {
                    for (;;)
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                            break;
                        int colonIndex = line.IndexOf(":");
                        if (colonIndex > 0)
                        {
                            int columnWidth;
                            if (int.TryParse(line.Substring(colonIndex + 1).Trim(), out columnWidth))
                            {
                                ExtraColumnDef extraDef = new ExtraColumnDef(line.Substring(0, colonIndex).Trim(), columnWidth);
                                ExtraColumns.Add(extraDef);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Column definition error for folder \"" + LabelText + "\": All lines must be <field name>:<width>");
                        }
                    }
                }
            }
            else
            {
                ExtraColumns.Add(new ExtraColumnDef("Assigned To", 110));
                ExtraColumns.Add(new ExtraColumnDef("Due Date", 110));
                ExtraColumns.Add(new ExtraColumnDef("Created On", 110));
                ExtraColumns.Add(new ExtraColumnDef("Updated On", 110));
            }
        }

        private void ConfigureColumns()
        {
            int otherColumnWidths = 0;
            LvwFiles.Columns.Clear();
            foreach(ExtraColumnDef extraDef in ExtraColumns)
            {
                otherColumnWidths += extraDef.Width;
            }
            LvwFiles.Columns.Add("Description", LvwFiles.ClientSize.Width - otherColumnWidths - 10);
            foreach(ExtraColumnDef extraDef in ExtraColumns)
            {
                LvwFiles.Columns.Add(extraDef.FieldName, extraDef.Width);
            }
        }

        private List<ExtraColumnDef> ExtraColumns;

        private class ExtraColumnDef
        {
            public ExtraColumnDef(string fieldName, int width)
            {
                FieldName = fieldName;
                NormalizedName = FieldName.ToLower().Replace(" ", "");
                Width = width;
            }

            public readonly string FieldName;
            public readonly string NormalizedName;
            public readonly int Width;
        }

        private void LoadFiles()
        {
            LvwFiles.Items.Clear();
            Files = new List<NoteFile>();
            DirectoryInfo dir = new DirectoryInfo(GetFullPath());
            foreach(FileInfo file in dir.EnumerateFiles())
            {
                if (IsNoteFile(file))
                {
                    NoteFile noteFile = new NoteFile(System, this);
                    noteFile.BareFileName = file.Name;
                    noteFile.LabelText = file.Name;
                    noteFile.CreatedAt = file.CreationTime;
                    noteFile.ModifiedAt = file.LastWriteTime;
                    noteFile.Load();
                    List<string> columnValues = new List<string>();
                    columnValues.Add(noteFile.LabelText);
                    foreach(ExtraColumnDef extraDef in ExtraColumns)
                    {
                        switch (extraDef.NormalizedName)
                        {
                            case "createdon":
                                columnValues.Add(noteFile.CreatedAt.ToString("MM/dd/yy hh:mmtt"));
                                break;
                            case "updatedon":
                                columnValues.Add(noteFile.ModifiedAt.ToString("MM/dd/yy hh:mmtt"));
                                break;
                            default:
                                string dataValue;
                                if (noteFile.DataFields.TryGetValue(extraDef.NormalizedName, out dataValue))
                                {
                                    columnValues.Add(dataValue);
                                }
                                else
                                {
                                    columnValues.Add("");
                                }
                                break;
                        }
                    }
                    ListViewItem item = new ListViewItem(columnValues.ToArray());
                    item.Tag = noteFile;
                    LvwFiles.Items.Add(item);
                }
            }
            NeedsRefresh = false;
        }

        private bool IsNoteFile(FileInfo file)
        {
            return file.Extension.ToLower() == ".txt";
        }
    }
}
