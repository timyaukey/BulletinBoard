using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BulletinBoard
{
    public partial class MainForm : Form
    {
        public string RootFolder;
        private NoteSystem _System;
        private bool _Quitting;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _System = new NoteSystem(RootFolder, this.tabMain, 
                StartRefreshTimer, lvwFileList_DoubleClick);
            this.Text = _System.RootFolder;
            _System.RequestRefresh();
            _System.RefreshIfNeeded();
            _System.StartWatching();
        }

        private void tabMain_Selected(object sender, TabControlEventArgs e)
        {
            // Will be null when clearing the tab collection on reload
            if (e.TabPage == null)
            {
                _System.CurrentFolder = null;
                this.menuMain.Enabled = false;
            }
            else
            {
                _System.CurrentFolder = (NoteFolder)e.TabPage.Tag;
                _System.CurrentFolder.RefreshIfNeeded();
                this.menuMain.Enabled = true;
            }
        }

        public void StartRefreshTimer()
        {
            timerRefresh.Enabled = true;
        }

        private void timerRefresh_Tick(object sender, EventArgs e)
        {
            timerRefresh.Enabled = false;
            _System.RefreshIfNeeded();
        }

        private void menuNew_Click(object sender, EventArgs e)
        {
            string newNoteName = FileNameForm.GetValue("Enter name of new note:", (NoteFolder)tabMain.SelectedTab.Tag);
            if (newNoteName == null)
                return;
            Monitor.Enter(_System.LockObject);
            try
            {
                string fullFilePath = _System.CurrentFolder.GetFullPath(newNoteName + ".txt");
                string initialContents;
                try
                {
                    initialContents = File.ReadAllText(_System.CurrentFolder.GetConfigFilePath("InitialContents.txt"));
                }
                catch
                {
                    initialContents = 
                        "Created By: " + Environment.NewLine +
                        "Assigned To: " + Environment.NewLine +
                        "Priority: " + Environment.NewLine +
                        "Due Date: " + Environment.NewLine +
                        "--------------------------------" + Environment.NewLine;
                }
                File.WriteAllText(fullFilePath, initialContents);
                _System.RefreshCurrentFolder();
                EditFile(fullFilePath);
            }
            catch(Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                Monitor.Exit(_System.LockObject);
            }
        }

        private void menuEdit_Click(object sender, EventArgs e)
        {
            Monitor.Enter(_System.LockObject);
            try
            {
                NoteFile file = GetSelectedFile();
                if (file == null)
                    return;
                EditFile(file.GetFullPath());
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                Monitor.Exit(_System.LockObject);
            }
        }

        private void lvwFileList_DoubleClick(object sender, EventArgs e)
        {
            Monitor.Enter(_System.LockObject);
            try
            {
                NoteFile file = GetSelectedFile();
                if (file == null)
                    return;
                EditFile(file.GetFullPath());
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                Monitor.Exit(_System.LockObject);
            }
        }

        private void menuRename_Click(object sender, EventArgs e)
        {
            Monitor.Enter(_System.LockObject);
            try
            {
                if (GetSelectedFile() == null)
                    return;
                string newNoteName = FileNameForm.GetValue("Enter new name for selected note:", (NoteFolder)tabMain.SelectedTab.Tag);
                if (newNoteName == null)
                    return;
                string newFullPath = _System.CurrentFolder.GetFullPath(newNoteName + ".txt");
                File.Move(GetSelectedFile().GetFullPath(), newFullPath);
                _System.RefreshCurrentFolder();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                Monitor.Exit(_System.LockObject);
            }
        }

        private void menuMove_Click(object sender, EventArgs e)
        {
            Monitor.Enter(_System.LockObject);
            try
            {
                if (GetSelectedFile() == null)
                    return;
                NoteFolder newNoteFolder = FolderNameForm.GetValue("Enter note tab to move to:", _System);
                if (newNoteFolder == null)
                    return;
                string newFullPath = newNoteFolder.GetFullPath(GetSelectedFile().BareFileName);
                File.Move(GetSelectedFile().GetFullPath(), newFullPath);
                _System.RefreshCurrentFolder();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                Monitor.Exit(_System.LockObject);
            }
        }

        private void menuArchive_Click(object sender, EventArgs e)
        {
            NoteFile file = GetSelectedFile();
            if (file == null)
                return;
            DialogResult result = MessageBox.Show(
                "Are you sure you want to archive the selected note " +
                "(this will move it to the archive subfolder)?", "Confirm Archive", 
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (result != DialogResult.OK)
                return;
            Monitor.Enter(_System.LockObject);
            try
            {
                string archiveDirPath = _System.CurrentFolder.GetArchiveDirPath();
                string baseName = Path.GetFileNameWithoutExtension(file.BareFileName);
                string archiveFileName = baseName + "." + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + ".txt";
                string archiveFilePath = Path.Combine(archiveDirPath, archiveFileName);
                File.Move(file.GetFullPath(), archiveFilePath);
                _System.RefreshCurrentFolder();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                Monitor.Exit(_System.LockObject);
            }
        }

        private void menuDelete_Click(object sender, EventArgs e)
        {
            NoteFile file = GetSelectedFile();
            if (file == null)
                return;
            DialogResult result = MessageBox.Show(
                "You will generally only delete a note if you created it by mistake. " +
                "Normally when you are done with a note you will archive it." +
                Environment.NewLine + Environment.NewLine +
                "Are you sure you want to delete the selected note?", "Confirm Delete",
                MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (result != DialogResult.OK)
                return;
            Monitor.Enter(_System.LockObject);
            try
            {
                File.Delete(file.GetFullPath());
                _System.RefreshCurrentFolder();
            }
            catch (Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                Monitor.Exit(_System.LockObject);
            }
        }

        private void menuExplore_Click(object sender, EventArgs e)
        {
            Monitor.Enter(_System.LockObject);
            try
            {
                string tabFolder = _System.CurrentFolder.GetFullPath();
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(tabFolder);
                startInfo.UseShellExecute = true;
                System.Diagnostics.Process.Start(startInfo);
            }
            catch(Exception ex)
            {
                ShowException(ex);
            }
            finally
            {
                Monitor.Exit(_System.LockObject);
            }
        }

        private void menuInstructions_Click(object sender, EventArgs e)
        {
            string instructionsFile = _System.CurrentFolder.GetConfigFilePath("Instructions.txt");
            if (!File.Exists(instructionsFile))
            {
                ShowErrorMessage("There are no instructions for this tab.");
                return;
            }
            EditFile(instructionsFile);
        }

        private NoteFile GetSelectedFile()
        {
            TabPage tab = this.tabMain.SelectedTab;
            if (tab == null)
                return null;
            NoteFolder folder = (NoteFolder)tab.Tag;
            if (folder.LvwFiles.SelectedItems.Count == 0)
                return null;
            return (NoteFile)folder.LvwFiles.SelectedItems[0].Tag;
        }

        private void EditFile(string fullFilePath)
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(fullFilePath);
            startInfo.UseShellExecute = true;
            System.Diagnostics.Process.Start(startInfo);
        }

        private void ShowErrorMessage(string msg)
        {
            MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowException(Exception ex)
        {
            MessageBox.Show(ex.Message);
        }

        private void menuExit_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Whoa there! You CAN exit this program, " +
                "but it's much smarter to leave it running and open other windows on top of it. " +
                "That way it's always around to refer to." + Environment.NewLine + Environment.NewLine +
                "Are you sure you want to exit?", "Really?", MessageBoxButtons.OKCancel, 
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (result != DialogResult.OK)
                return;
            _Quitting = true;
            this.Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_Quitting)
                return;
            e.Cancel = true;
            this.WindowState = FormWindowState.Minimized;
        }
    }
}
