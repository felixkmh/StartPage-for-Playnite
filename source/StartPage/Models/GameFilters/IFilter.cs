using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models.GameFilters
{
    public interface IFilter<T>
    {

    }

    public interface IGameFilter : IFilter<Game>
    {

    }
}
