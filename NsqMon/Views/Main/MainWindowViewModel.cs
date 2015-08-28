using System.Collections.ObjectModel;
using NsqMon.Common.ApplicationServices;
using NsqMon.Common.Events.Ux;
using NsqMon.Common.Mvvm;
using NsqMon.Plugin;
using NsqMon.Plugin.Interfaces;
using NsqMon.Test;

namespace NsqMon.Views.Main
{
    public interface IMainWindowViewModel : IViewModelBase
    {
    }

    public class MainWindowViewModel : ViewModelBase, IMainWindowViewModel
    {
        public MainWindowViewModel(IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
            INsqMonPlugin plugin = new NsqMonLocalhostPlugin();

            AddPropertyChangedHandler<ObservableCollection<ICluster>>(nameof(Clusters), OnClustersChanged);
            AddPropertyChangedHandler<ICluster>(nameof(SelectedCluster), OnSelectedClusterChanged);
            AddPropertyChangedHandler<ObservableCollection<IEnvironment>>(nameof(Environments), OnEnvironmentsChanged);

            Clusters = new ObservableCollection<ICluster>(plugin.GetClusters());
        }

        private void OnClustersChanged(object sender, PropertyChangedEventArgs<ObservableCollection<ICluster>> e)
        {
            var clusters = e.NewValue;
            if (clusters == null || clusters.Count == 0)
            {
                SelectedCluster = null;
            }
            else if (clusters.Count == 1)
            {
                SelectedCluster = clusters[0];
            }
        }

        private void OnSelectedClusterChanged(object sender, PropertyChangedEventArgs<ICluster> e)
        {
            Environments = e.NewValue == null ? null : new ObservableCollection<IEnvironment>(e.NewValue.GetEnvironments());
        }

        private void OnEnvironmentsChanged(object sender, PropertyChangedEventArgs<ObservableCollection<IEnvironment>> e)
        {
            var environments = e.NewValue;
            if (environments == null || environments.Count == 0)
            {
                SelectedEnvironment = null;
            }
            else if (environments.Count == 1)
            {
                SelectedEnvironment = environments[0];
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