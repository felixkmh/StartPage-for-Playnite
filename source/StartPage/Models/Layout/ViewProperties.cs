using Playnite.SDK.Data;
using StartPage.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models.Layout
{
    public class ViewProperties : ObservableObject
    {
        private string viewId;
        public string ViewId { get => viewId; set => SetValue(ref viewId, value); }
        private Guid instanceId = Guid.NewGuid();
        public Guid InstanceId { get => instanceId; set => SetValue(ref instanceId, value); }
        private Guid pluginId; 
        public Guid PluginId { get => pluginId ; set => SetValue(ref pluginId, value); }

        [DontSerialize]
        internal object view = null;

        [DontSerialize]
        private StartPageViewArgsBase startPageViewArgs = null;
        [DontSerialize]
        public StartPageViewArgsBase StartPageViewArgs { get => startPageViewArgs; set => SetValue(ref startPageViewArgs, value); }
    }

    public class StartPageViewArgs : StartPageViewArgsBase
    {
        public Guid PluginId { get; set; }
    }
}
