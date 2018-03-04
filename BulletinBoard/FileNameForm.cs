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
    public partial class FileNameForm : Form
    {
        private NoteFolder _NoteFolder;

        public FileNameForm()
        {
            InitializeComponent();
            this.DialogResult = DialogResult.Cancel;
        }

        public static string GetValue(string instructions, NoteFolder noteFolder)
        {
            using (FileNameForm frm = new FileNameForm())
            {
                frm._NoteFolder = noteFolder;
                frm.lblInstructions.Text = instructions;
                DialogResult result = frm.ShowDialog();
                if (result == DialogResult.OK)
                    return frm.txtFileName.Text;
                return null;
            }
        }

        private void btnOkay_Click(object sender, EventArgs e)
        {
            foreach(char nameChar in txtFileName.Text)
            {
                foreach(char forbiddenChar in "\\/:*?\"<>|")
                {
                    if (nameChar == forbiddenChar)
                    {
                        ShowValidationError("Note name may not contain \\/:*?\"<>| characters.");
                        return;
                    }
                }
            }
            if (string.IsNullOrEmpty(txtFileName.Text))
            {
                ShowValidationError("Note name may not be blank.");
                return;
            }
            if (File.Exists(_NoteFolder.GetFullPath(txtFileName.Text + ".txt")))
            {
                ShowValidationError("A note by that name already exists.");
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
    }
}
