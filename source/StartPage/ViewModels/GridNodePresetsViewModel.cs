using LandingPage.Models.Layout;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LandingPage.ViewModels
{
    public class GridNodePresetsViewModel : ObservableObject
    {
        private LandingPageExtension plugin;

        public ObservableCollection<GridNodePreset> Presets { get => plugin.Settings.GridNodePresets; set { plugin.Settings.GridNodePresets = value; OnPropertyChanged(); } }

        public ICommand SavePresetCommand { get; }
        public ICommand RemovePresetsCommand { get; }
        public ICommand ExportPresetCommand { get; }
        public ICommand ImportPresetCommand { get; }

        public GridNodePresetsViewModel(LandingPageExtension plugin)
        {
            this.plugin = plugin;
            SavePresetCommand = new RelayCommand(() =>
            {
                var res = plugin.PlayniteApi.Dialogs.SelectString(ResourceProvider.GetString("LOC_SPG_SelectLayoutName"), string.Empty, "Layout");
                if (res.Result)
                {
                    Presets.Add(new GridNodePreset { Name = res.SelectedString, Node = plugin.Settings.GridLayout });
                }
            });

            RemovePresetsCommand = new RelayCommand<IEnumerable<GridNodePreset>>(p =>
            {
                if (p != null)
                {
                    foreach(var preset in p)
                    {
                        Presets.Remove(preset);
                    }
                }
            });

            ExportPresetCommand = new RelayCommand<GridNodePreset>(p =>
            {
                var dialogResult = plugin.PlayniteApi.Dialogs.SaveFile("JSON|*.json");
                if (string.IsNullOrEmpty(dialogResult))
                {
                    return;
                }
                try
                {
                    var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(p);
                    File.WriteAllText(dialogResult, serialized);
                }
                catch (Exception ex)
                {
                    LandingPageExtension.logger.Error(ex, "Failed to save layout preset.");
                }
            });

            ImportPresetCommand = new RelayCommand(() =>
            {
                var dialogResult = plugin.PlayniteApi.Dialogs.SelectFile("JSON|*.json");
                if (string.IsNullOrEmpty(dialogResult))
                {
                    return;
                }
                try
                {
                    if (File.Exists(dialogResult))
                    {
                        var serialized = File.ReadAllText(dialogResult);
                        var preset = Newtonsoft.Json.JsonConvert.DeserializeObject<GridNodePreset>(serialized);
                        if (preset is GridNodePreset)
                        {
                            Presets.Add(preset);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LandingPageExtension.logger.Error(ex, $"Failed to import preset from {dialogResult}.");
                }
            });
        }
    }
}
