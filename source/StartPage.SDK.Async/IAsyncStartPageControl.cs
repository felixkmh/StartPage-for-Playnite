using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StartPage.SDK.Async
{
    /// <summary>
    /// Interface that a control or its DataContext can implement to be notified of some events.
    /// </summary>
    public interface IAsyncStartPageControl
    {
        /// <summary>
        /// Called whenever the view becomes visible. Happens whenever StartPage is opened and 
        /// when the view is added.
        /// </summary>
        Task OnViewShownAsync();
        /// <summary>
        /// Called whenever the view is added to the grid.
        /// Use this for one time initialization. Ideally, GetStartPageView()
        /// only creates the Control, without populating the view model.
        /// In here, all async operations, like reading files, parsing/preparing data, should be done 
        /// on a background thread and be awaited, so that the UI isn't blocked.
        /// </summary>
        Task InitializeAsync();
        /// <summary>
        /// Called when the view is hidden. Happens when StartPage is closed.
        /// In here, all async operations, like reading files, parsing/preparing data, should be done 
        /// on a background thread and be awaited, so that the UI isn't blocked.
        /// </summary>
        Task OnViewHiddenAsync();
        /// <summary>
        /// Called when a day ends after 11:59 pm or 23:59.
        /// </summary>
        /// <param name="newTime">Time of the new day.</param>
        Task OnDayChangedAsync(DateTime newTime);
    }
}
