using TiaGitAddIn.Models.Lad;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class LadPinViewModel(LadPinLayout pin) : ViewModelBase
    {
        public string Name => pin.Name;
        public string Operand => pin.Operand;
    }
}
