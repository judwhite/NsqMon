using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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

            Task.Factory.StartNew(PollNsqLookupds);
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
            int nsqLookupdIndex = -1;
            while (true)
            {
                bool wait = true;
                try
                {
                    var nsqLookupds = NsqLookupds;
                    if (nsqLookupds != null && nsqLookupds.Count != 0)
                    {
                        nsqLookupdIndex++;
                        var nsqLookupdModel = nsqLookupds[nsqLookupdIndex % nsqLookupds.Count];
                        PingNsqLookupd(nsqLookupdModel);

                        wait = (nsqLookupdIndex > nsqLookupds.Count);
                    }
                    else
                    {
                        // TODO
                    }
                }
                catch (Exception)
                {
                    wait = true;
                }

                if (wait)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                }
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
                topics = topics.OrderBy(p => p.Topic);

                _dispatcher.BeginInvoke(() =>
                                        {
                                            if (Topics == null)
                                            {
                                                Topics = new ObservableCollection<TopicModel>(topics);
                                            }
                                            else
                                            {
                                                foreach (var topic in topics)
                                                {
                                                    var existingTopic = Topics.SingleOrDefault(p => p.Topic == topic.Topic);
                                                    if (existingTopic == null)
                                                        Topics.Add(topic);
                                                }
                                            }
                                        });
            }
            catch (Exception)
            {
                throw;
            }

            try
            {
                var nsqdNodes = client.GetNodes();
                var tasks = new List<Task>();
                for (int i = 0; i < nsqdNodes.Length; i++)
                {
                    var nsqdNode = nsqdNodes[i];

                    var task = Task.Factory.StartNew(() => GetNsqdInformation(nsqdNode));
                    tasks.Add(task);
                }

                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void GetNsqdInformation(ProducerInformation nsqdNode)
        {
            var httpUri = string.Format("http://{0}:{1}", nsqdNode.BroadcastAddress, nsqdNode.HttpPort);
            var client = new NsqdHttpClient(httpUri, TimeSpan.FromSeconds(10));
            var stats = client.GetStats();
            var topics = new List<TopicModel>(Topics);
            foreach (var topic in topics)
            {
                var nsqdTopic = stats.Topics.SingleOrDefault(p => p.TopicName == topic.Topic);
                //int? messageCount = nsqdTopic == null ? (int?)null : nsqdTopic.MessageCount;
                topic.SetNsqdStats(nsqdNode, nsqdTopic);
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
            private readonly Dictionary<string, NsqdStatsTopic> _nsqdStats = new Dictionary<string, NsqdStatsTopic>();
            private readonly object _nsqdStatsLocker = new object();

            public string Topic
            {
                get { return Get<string>("Topic"); }
                set { Set("Topic", value); }
            }

            public int? TotalMessages
            {
                get { return Get<int?>("TotalMessages"); }
                set { Set("TotalMessages", value); }
            }

            public int? TotalDepth
            {
                get { return Get<int?>("TotalDepth"); }
                set { Set("TotalDepth", value); }
            }

            public int? TotalBackendDepth
            {
                get { return Get<int?>("TotalBackendDepth"); }
                set { Set("TotalBackendDepth", value); }
            }

            public int? TotalChannelDepth
            {
                get { return Get<int?>("TotalChannelDepth"); }
                set { Set("TotalChannelDepth", value); }
            }

            public int? TotalChannelBackendDepth
            {
                get { return Get<int?>("TotalChannelBackendDepth"); }
                set { Set("TotalChannelBackendDepth", value); }
            }

            public int? ChannelCount
            {
                get { return Get<int?>("ChannelCount"); }
                set { Set("ChannelCount", value); }
            }

            internal void SetNsqdStats(ProducerInformation nsqdNode, NsqdStatsTopic nsqdTopic)
            {
                if (nsqdNode == null)
                    throw new ArgumentNullException("nsqdNode");

                lock (_nsqdStatsLocker)
                {
                    string key = string.Format("{0}:{1}", nsqdNode.BroadcastAddress, nsqdNode.HttpPort);
                    _nsqdStats[key] = nsqdTopic;
                }

                CalculateStats();
            }

            private void CalculateStats()
            {
                int totalMessages = 0;
                int totalDepth = 0;
                int totalBackendDepth = 0;
                int totalChannelDepth = 0;
                int totalChannelBackendDepth = 0;
                int channelCount = 0;
                lock (_nsqdStatsLocker)
                {
                    foreach (var kvp in _nsqdStats)
                    {
                        var value = kvp.Value;
                        if (value == null)
                            continue;
                        totalMessages += value.MessageCount;
                        totalDepth += value.Depth;
                        totalBackendDepth += value.BackendDepth;
                        channelCount = Math.Max(channelCount, value.Channels.Length);

                        foreach (var channel in value.Channels)
                        {
                            totalChannelDepth += channel.Depth;
                            totalChannelBackendDepth += channel.BackendDepth;
                        }
                    }

                    if (TotalMessages != totalMessages ||
                        TotalDepth != totalDepth ||
                        TotalBackendDepth != totalBackendDepth ||
                        ChannelCount != channelCount ||
                        TotalChannelDepth != totalChannelDepth ||
                        TotalChannelBackendDepth != totalChannelBackendDepth
                       )
                    {
                        _dispatcher.BeginInvoke(() =>
                        {
                            TotalMessages = totalMessages;
                            TotalDepth = totalDepth;
                            TotalBackendDepth = totalBackendDepth;
                            ChannelCount = channelCount;
                            TotalChannelDepth = totalChannelDepth;
                            TotalChannelBackendDepth = totalChannelBackendDepth;
                        });
                    }
                }
            }
        }
    }
}