using Dopamine.Data.Entities;
using Prism.Mvvm;

namespace Dopamine.Services.Entities
{
    public class FolderViewModel : BindableBase
    {
        private FolderV folder;

        public FolderViewModel(FolderV folder)
        {
            this.folder = folder;
        }

        public FolderV Folder => this.folder;

        public string Path => this.folder.Path;

        public long FolderId => this.folder.Id;
       
        public string Directory => System.IO.Path.GetFileName(this.folder.Path);

        public bool ShowInCollection
        {
            get { return this.folder.Show ? true : false; }

            set
            {
                this.folder.Show = value;
                RaisePropertyChanged(nameof(this.ShowInCollection));
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !GetType().Equals(obj.GetType()))
            {
                return false;
            }

            return string.Equals(this.Path, ((FolderViewModel)obj).Path);
        }

        public override int GetHashCode()
        {
            return this.Path.GetHashCode();
        }

        public override string ToString()
        {
            return this.Directory; 
        }
    }
}
