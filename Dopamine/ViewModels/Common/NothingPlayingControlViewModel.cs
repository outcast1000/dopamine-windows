using Dopamine.Services.Playback;
using Dopamine.ViewModels.Common.Base;
using GongSolutions.Wpf.DragDrop;
using Prism.Ioc;
using Prism.Mvvm;
using System.Threading.Tasks;

namespace Dopamine.ViewModels.Common
{
    public class NothingPlayingControlViewModel : TracksViewModelBase, IDropTarget
    {
        public NothingPlayingControlViewModel(IContainerProvider container) : base(container)
        {
        }

        public void DragOver(IDropInfo dropInfo)
        {
            throw new System.NotImplementedException();
        }

        public void Drop(IDropInfo dropInfo)
        {
            throw new System.NotImplementedException();
        }

        protected override Task EmptyListsAsync()
        {
            throw new System.NotImplementedException();
        }

        protected override Task FillListsAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}
