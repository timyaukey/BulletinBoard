using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BulletinBoard
{
    public class NoteFile
    {
        public NoteSystem System;
        public NoteFolder Folder;

        public string LabelText;
        public string BareFileName;
        public DateTime CreatedAt;
        public DateTime ModifiedAt;
        public string CreatedBy;
        public string DueBy;
        public string AssignedTo;
        public Dictionary<string, string> DataFields;

        public NoteFile(NoteSystem system, NoteFolder folder)
        {
            System = system;
            Folder = folder;
            DataFields = new Dictionary<string, string>();
        }

        public string GetFullPath()
        {
            return Folder.GetFullPath(BareFileName);
        }

        public void Load()
        {
            try
            {
                using (TextReader reader = new StreamReader(GetFullPath()))
                {
                    for(;;)
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                            break;
                        int colonIndex = line.IndexOf(":");
                        if (colonIndex > 0)
                        {
                            string key = line.Substring(0, colonIndex).Trim().ToLower().Replace(" ", "");
                            string value = line.Substring(colonIndex + 1).Trim();
                            DataFields[key] = value;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Exception reading note file " + GetFullPath() + ": " + ex.Message);
            }
        }
    }
}
