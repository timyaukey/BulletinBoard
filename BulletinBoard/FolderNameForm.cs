using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BulletinBoard
{
    public partial class FolderNameForm : Form
    {
        private NoteSystem _NoteSystem;

        public FolderNameForm()
        {
            InitializeComponent();
            this.DialogResult = DialogResult.Cancel;
        }

        public static NoteFolder GetValue(string instructions, NoteSystem noteSystem)
        {
            using (FolderNameForm frm = new FolderNameForm())
            {
                frm._NoteSystem = noteSystem;
                frm.lblInstructions.Text = instructions;
                DialogResult result = frm.ShowDialog();
                if (result == DialogResult.OK)
                    return ((FolderListItem)frm.lstFolders.SelectedItem).Folder;
                return null;
            }
        }

        private void FolderNameForm_Load(object sender, EventArgs e)
        {
            lstFolders.Items.Clear();
            foreach(NoteFolder folder in _NoteSystem.Folders)
            {
                lstFolders.Items.Add(new FolderListItem(folder));
            }
        }

        private void btnOkay_Click(object sender, EventArgs e)
        {
            if (lstFolders.SelectedItem == null)
            {
                ShowValidationError("Please choose a note tab.");
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ShowValidationError(string msg)
        {
            MessageBox.Show(msg, "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private class FolderListItem
        {
            public readonly NoteFolder Folder;

            public FolderListItem(NoteFolder folder)
            {
                Folder = folder;
            }

            public override string ToString()
            {
                return Folder.LabelText;
            }
        }
    }
}
