using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NsqMon.Common.ApplicationServices;
using NsqMon.Common.Events.Ux;
using NsqMon.Common.Mvvm;
using NsqMon.Plugin;
using NsqMon.Plugin.Interfaces;
using NsqMon.Test;
using NsqMon.Views.CDTag.Views;

namespace NsqMon
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

    public interface IMainWindowViewModel : IViewModelBase
    {
    }

    public class MainWindowViewModel : ViewModelBase, IMainWindowViewModel
    {
        private readonly INsqMonPlugin _plugin;

        public MainWindowViewModel(IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
            _plugin = new NsqMonLocalhostPlugin();

            EnhancedPropertyChanged += MainWindowViewModel_EnhancedPropertyChanged;

            Clusters = new ObservableCollection<ICluster>(_plugin.GetClusters());
        }

        private void MainWindowViewModel_EnhancedPropertyChanged(object sender, EnhancedPropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Clusters))
            {
                var clusters = (IList<ICluster>)e.NewValue;
                if (clusters == null || clusters.Count == 0)
                {
                    SelectedCluster = null;
                }
                else if (clusters.Count == 1)
                {
                    SelectedCluster = clusters[0];
                }
            }
            else if (e.PropertyName == nameof(SelectedCluster))
            {
                var cluster = (ICluster)e.NewValue;
                if (cluster == null)
                {
                    Environments = null;
                    return;
                }

                Environments = new ObservableCollection<IEnvironment>(cluster.GetEnvironments());
            }
            else if (e.PropertyName == nameof(Environments))
            {
                var environments = (IList<IEnvironment>)e.NewValue;
                if (environments == null || environments.Count == 0)
                {
                    SelectedEnvironment = null;
                    return;
                }
                else if (environments.Count == 1)
                {
                    SelectedEnvironment = environments[0];
                }
            }
        }

        public ObservableCollection<ICluster> Clusters
        {
            get { return Get<ObservableCollection<ICluster>>(nameof(Clusters)); }
            set { Set(nameof(Clusters), value); }
        }

        public ICluster SelectedCluster
        {
            get { return Get<ICluster>(nameof(SelectedCluster)); }
            set { Set(nameof(SelectedCluster), value); }
        }

        public ObservableCollection<IEnvironment> Environments
        {
            get { return Get<ObservableCollection<IEnvironment>>(nameof(Environments)); }
            set { Set(nameof(Environments), value); }
        }

        public IEnvironment SelectedEnvironment
        {
            get { return Get<IEnvironment>(nameof(SelectedEnvironment)); }
            set { Set(nameof(SelectedEnvironment), value); }
        }
    }
}
