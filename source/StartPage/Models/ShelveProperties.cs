using LandingPage.Converters;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Order
    {
        [Description("LOCMenuSortAscending")]
        Ascending = 0,
        [Description("LOCMenuSortDescending")]
        Descending = 1
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum SortingField
    {
        [Description("LOCGameNameTitle")]
        Name,
        [Description("LOCGameSortingNameTitle")]
        SortingName,
        [Description("LOCGameLastActivityTitle")]
        LastActivity,
        [Description("LOCDateAddedLabel")]
        DateAdded,
        [Description("LOCTimePlayed")]
        Playtime,
        [Description("LOCGameReleaseDateTitle")]
        ReleaseDate,
        [Description("LOCCriticScore")]
        CriticScore,
        [Description("LOCUserScore")]
        UserScore,
        [Description("LOCCommunityScore")]
        CommunityScore,
        [Description("LOCCompletionStatus")]
        CompletionStatus,
        [Description("LOC_SPG_BackgroundImageSourceRandom")]
        Random
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum GroupingField
    {
        [Description("LOCMenuGroupDont")]
        None,
        [Description("LOCGameLastActivityTitle")]
        LastActivity,
        [Description("LOCDateAddedLabel")]
        DateAdded,
        [Description("LOCCriticScore")]
        CriticScore,
        [Description("LOCUserScore")]
        UserScore,
        [Description("LOCCommunityScore")]
        CommunityScore,
        [Description("LOCLibrary")]
        Library,
        [Description("LOCCompletionStatus")]
        CompletionStatus
    }

    public class ShelveProperties : ObservableObject
    {
        public static ShelveProperties RecentlyPlayed
            => new ShelveProperties { Order = Order.Descending, SortBy = SortingField.LastActivity, GroupBy = GroupingField.LastActivity, Name = ResourceProvider.GetString("LOCQuickFilterRecentlyPlayed") };

        public static ShelveProperties RecentlyAdded
            => new ShelveProperties { Order = Order.Descending, SortBy = SortingField.DateAdded, GroupBy = GroupingField.DateAdded, Name = ResourceProvider.GetString("LOC_SPG_RecentlyAddedGames") };

        private string name = string.Empty;
        public string Name { get => name; set => SetValue(ref name, value); }

        private int numberOfGames = 11;
        public int NumberOfGames { get => numberOfGames; set => SetValue(ref numberOfGames, value); }

        private int skippedGames = 0;
        public int SkippedGames { get => Math.Max(0, skippedGames); set => SetValue(ref skippedGames, Math.Max(0, value)); }

        private Order order = Order.Ascending;
        public Order Order { get => order; set => SetValue(ref order, value); }

        private SortingField sortBy = SortingField.Name;
        public SortingField SortBy { get => sortBy; set => SetValue(ref sortBy, value); }

        private GroupingField groupBy = GroupingField.LastActivity;
        public GroupingField GroupBy { get => groupBy; set => SetValue(ref groupBy, value); }

        private bool ignoreHidden = true;
        public bool IgnoreHidden { get => ignoreHidden; set => SetValue(ref ignoreHidden, value); }

        private bool installedOnly = false;
        public bool InstalledOnly { get => installedOnly; set => SetValue(ref installedOnly, value); }

        private bool favoritesOnly = false;
        public bool FavoritesOnly { get => favoritesOnly; set => SetValue(ref favoritesOnly, value); }

        private HashSet<Guid> categories = new HashSet<Guid>();
        public HashSet<Guid> Categories { get => categories; set => SetValue(ref categories, value); }

        private HashSet<Guid> tags = new HashSet<Guid>();
        public HashSet<Guid> Tags { get => tags; set => SetValue(ref tags, value); }

        private HashSet<Guid> completionStatus = new HashSet<Guid>();
        public HashSet<Guid> CompletionStatus { get => completionStatus; set => SetValue(ref completionStatus, value); }

        private HashSet<Guid> features = new HashSet<Guid>();
        public HashSet<Guid> Features { get => features; set => SetValue(ref features, value); }

        private HashSet<Guid> platforms = new HashSet<Guid>();
        public HashSet<Guid> Platforms { get => platforms; set => SetValue(ref platforms, value); }

        private HashSet<Guid> sources = new HashSet<Guid>();
        public HashSet<Guid> Sources { get => sources; set => SetValue(ref sources, value); }

        private HashSet<Guid> genres = new HashSet<Guid>();
        public HashSet<Guid> Genres { get => genres; set => SetValue(ref genres, value); }
    }
}
