using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public NoteFile(NoteSystem system, NoteFolder folder)
        {
            System = system;
            Folder = folder;
        }

        public string GetFullPath()
        {
            return Folder.GetFullPath(BareFileName);
        }
    }
}
