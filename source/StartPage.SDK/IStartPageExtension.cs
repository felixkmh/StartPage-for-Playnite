using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Playnite.SDK.Models;

namespace StartPage.SDK
{
    /// <summary>
    /// 
    /// </summary>
    public class StartPageViewArgs
    {
        /// <summary>
        /// Uniquely identifying a custom view within an extension.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Name shown for this view in StartPage.
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IStartPageExtension
    {
        /// <summary>
        /// Returns a list of <see cref="StartPageViewArgs"/> of available views. Each <see cref="StartPageViewArgs"/> can be 
        /// used as the parameter to <see cref="IStartPageExtension.GetStartPageView(StartPageViewArgs)"/> to retrieve a custom view.
        /// </summary>
        /// <returns><see cref="IEnumerable{T}"/> of <see cref="StartPageViewArgs"/></returns>
        IEnumerable<StartPageViewArgs> GetAvailableStartPageViews();
        /// <summary>
        /// Used to request a view to be inserted into StartPage.
        /// </summary>
        /// <param name="args"></param>
        /// <returns>Either an <see cref="System.Collections.ObjectModel.ObservableCollection{Game}"/> of <see cref="Playnite.SDK.Models.Game"/>s, 
        /// or a <see cref="Control"/>.
        /// </returns>
        object GetStartPageView(StartPageViewArgs args);
    }
}
