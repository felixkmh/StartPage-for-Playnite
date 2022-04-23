using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Playnite.SDK.Models;

namespace StartPage.SDK
{
    /// <summary>
    /// Corresponds to an available view.
    /// </summary>
    public class StartPageViewArgsBase
    {
        /// <summary>
        /// Uniquely identifying a custom view within an extension.
        /// </summary>
        public string ViewId { get; set; }
        /// <summary>
        /// Name shown for this view in StartPage.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Optional description of the view.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Indicates whether there are settings to customize this view.
        /// </summary>
        public bool HasSettings { get; set; } = false;
        /// <summary>
        /// Indicates whether multiple instances of this view can be added to StartPage.
        /// Each instance will have its own unique, persistent InstanceID.
        /// </summary>
        public bool AllowMultipleInstances { get; set; } = false;
    }

    /// <summary>
    /// 
    /// </summary>
    public class StartPageExtensionArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public string ExtensionName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<StartPageViewArgsBase> Views { get; set; }
    }

    /// <summary>
    /// Interface that a control or its DataContext can implement to be notified of some events.
    /// </summary>
    public interface IStartPageControl
    {
        /// <summary>
        /// Called when the StartPage view is opened.
        /// </summary>
        void OnStartPageOpened();
        /// <summary>
        /// Called when the StartPage view is closed.
        /// </summary>
        void OnStartPageClosed();
        /// <summary>
        /// Called when a day ends after 11:59 pm or 23:59.
        /// </summary>
        /// <param name="newTime">Time of the new day.</param>
        void OnDayChanged(DateTime newTime);
    }

    /// <summary>
    /// A generic plugin can implement this interface to provide custom views for StartPage.
    /// </summary>
    public interface IStartPageExtension
    {
        /// <summary>
        /// Returns a list of <see cref="StartPageViewArgsBase"/> of available views and the extension name. Each <see cref="StartPageViewArgsBase"/> can be 
        /// used as the parameter to <see cref="IStartPageExtension.GetStartPageView(string, Guid)"/> to retrieve a custom view.
        /// </summary>
        /// <returns>A <see cref="StartPageExtensionArgs"/> containing the extension name and its available views.</returns>
        StartPageExtensionArgs GetAvailableStartPageViews();
        /// <summary>
        /// Used to request a view to be inserted into StartPage.
        /// </summary>
        /// <param name="viewId">The id of the requested view.</param>
        /// <param name="instanceId">Optional instanceId if multiple instances can be added.</param>
        /// <returns>A <see cref="FrameworkElement"/>. Returns <see langword="null"/> if the <paramref name="viewId"/> is invalid.
        /// </returns>
        object GetStartPageView(string viewId, Guid instanceId);
        /// <summary>
        /// Can provide a settings view to customize the view with Id <paramref name="viewId"/>.
        /// </summary>
        /// <param name="viewId">ID of the view that the settings customize.</param>
        /// <param name="instanceId">Optional instanceId if multiple instances can be managed.</param>
        /// <returns>A settings view for the view with ID <paramref name="viewId"/> if it valid and the view has settings. Otherwise <see langword="null"/>.</returns>
        Control GetStartPageViewSettings(string viewId, Guid instanceId);
        /// <summary>
        /// Called when a view is removed from StartPage.
        /// </summary>
        /// <param name="viewId">The viewId of the removed view.</param>
        /// <param name="instanceId">InstanceId of the removed view.</param>
        void OnViewRemoved(string viewId, Guid instanceId);
    }
}
