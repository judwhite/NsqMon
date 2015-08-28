using NsqMon.Views.CDTag.Views;

namespace NsqMon.Views.Main
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowViewBase
    {
        private readonly IMainWindowViewModel _viewModel;

        public MainWindow(IMainWindowViewModel viewModel)
            : base(viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();

            HandleEscape = false;
        }
    }
}
