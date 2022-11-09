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
        /// Called whenever the view becomed visible.
        /// </summary>
        Task OnViewShownAsync();
        /// <summary>
        /// Called when either StartPage has retrieved 
        /// all views on launch or when the view was added.
        /// </summary>
        Task InitializeAsync();
        /// <summary>
        /// Called when the view is hidden.
        /// </summary>
        Task OnViewHiddenAsync();
        /// <summary>
        /// Called when a day ends after 11:59 pm or 23:59.
        /// </summary>
        /// <param name="newTime">Time of the new day.</param>
        Task OnDayChangedAsync(DateTime newTime);
    }
}
