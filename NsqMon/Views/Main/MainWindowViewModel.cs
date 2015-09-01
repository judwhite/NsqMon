using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using NsqMon.Common.ApplicationServices;
using NsqMon.Common.Events.Ux;
using NsqMon.Common.Mvvm;
using NsqMon.Plugin;
using NsqMon.Plugin.Interfaces;
using NsqMon.Test;
using NsqSharp.Api;

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

            AddPropertyChangedHandler<ObservableCollection<ICluster>>("Clusters", OnClustersChanged);
            AddPropertyChangedHandler<ICluster>("SelectedCluster", OnSelectedClusterChanged);
            AddPropertyChangedHandler<ObservableCollection<IEnvironment>>("Environments", OnEnvironmentsChanged);
            AddPropertyChangedHandler<IEnvironment>("SelectedEnvironment", OnSelectedEnvironmentChanged);

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

        private void OnSelectedEnvironmentChanged(object sender, PropertyChangedEventArgs<IEnvironment> e)
        {
            var environment = e.NewValue;
            if (environment == null)
            {
                NsqLookupds = null;

            }
            else
            {
                NsqLookupds = new ObservableCollection<NsqLookupdModel>(
                    environment.GetNsqLookupds().Select(uri => new NsqLookupdModel { Uri = uri })
                );

                PollNsqLookupds();
            }
        }

        public ObservableCollection<ICluster> Clusters
        {
            get { return Get<ObservableCollection<ICluster>>("Clusters"); }
            set { Set("Clusters", value); }
        }

        public ICluster SelectedCluster
        {
            get { return Get<ICluster>("SelectedCluster"); }
            set { Set("SelectedCluster", value); }
        }

        public ObservableCollection<IEnvironment> Environments
        {
            get { return Get<ObservableCollection<IEnvironment>>("Environments"); }
            set { Set("Environments", value); }
        }

        public IEnvironment SelectedEnvironment
        {
            get { return Get<IEnvironment>("SelectedEnvironment"); }
            set { Set("SelectedEnvironment", value); }
        }

        public ObservableCollection<NsqLookupdModel> NsqLookupds
        {
            get { return Get<ObservableCollection<NsqLookupdModel>>("NsqLookupds"); }
            set { Set("NsqLookupds", value); }
        }

        public ObservableCollection<TopicModel> Topics
        {
            get { return Get<ObservableCollection<TopicModel>>("Topics"); }
            set { Set("Topics", value); }
        }

        private void PollNsqLookupds()
        {
            var nsqLookupds = NsqLookupds;
            if (nsqLookupds == null || nsqLookupds.Count == 0)
                return;

            foreach (var nsqLookupdModel in nsqLookupds)
            {
                var task = Task.Factory.StartNew(() => PingNsqLookupd(nsqLookupdModel));
            }
        }

        private void PingNsqLookupd(NsqLookupdModel nsqLookupdModel)
        {
            var client = new NsqLookupdHttpClient(nsqLookupdModel.Uri.OriginalString, TimeSpan.FromSeconds(10));
            bool success = false;
            var pingResponse = DateTime.MinValue;
            try
            {
                client.Ping();
                pingResponse = DateTime.Now;
                success = true;
            }
            catch (Exception)
            {
            }
            
            _dispatcher.BeginInvoke(() =>
                                    {
                                        nsqLookupdModel.IsAlive = success;
                                        if (success)
                                            nsqLookupdModel.LastPingResponse = pingResponse;
                                    });

            try
            {
                var topics = client.GetTopics().Select(p => new TopicModel { Topic = p });
                var producers = client.GetNodes();

                _dispatcher.BeginInvoke(() =>
                                        {
                                            Topics = new ObservableCollection<TopicModel>(topics);
                                        });
            }
            catch (Exception)
            {
                throw;
            }
        }

        public class NsqLookupdModel : ModelBase
        {
            public Uri Uri
            {
                get { return Get<Uri>("Uri"); }
                set { Set("Uri", value); }
            }

            public bool? IsAlive
            {
                get { return Get<bool?>("IsAlive"); }
                set { Set("IsAlive", value); }
            }

            public DateTime? LastPingResponse
            {
                get { return Get<DateTime?>("LastPingResponse"); }
                set { Set("LastPingResponse", value); }
            }
        }

        public class TopicModel : ModelBase
        {
            public string Topic
            {
                get { return Get<string>("Topic"); }
                set { Set("Topic", value); }
            }
        }
    }
}