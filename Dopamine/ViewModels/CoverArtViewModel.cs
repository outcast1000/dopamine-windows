using Prism.Mvvm;

namespace Dopamine.ViewModels
{
    public class CoverArtViewModel : BindableBase
    {
        private string coverArt;

        public string CoverArt
        {
            get { return this.coverArt; }
            set { SetProperty<string>(ref this.coverArt, value); }
        }
    }
}