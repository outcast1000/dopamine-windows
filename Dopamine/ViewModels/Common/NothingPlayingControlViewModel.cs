using Dopamine.Services.Playback;
using Dopamine.ViewModels.Common.Base;
using GongSolutions.Wpf.DragDrop;
using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;

namespace Dopamine.ViewModels.Common
{
    public class NothingPlayingControlViewModel : ContextMenuViewModelBase, IDropTarget
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

        protected override void SearchOnline(string id)
        {
            throw new NotImplementedException();
        }
    }
}
