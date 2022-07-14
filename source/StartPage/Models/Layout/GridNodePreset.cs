using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandingPage.Models.Layout
{
    public class GridNodePreset : ObservableObject
    {
        private GridNode node;
        public GridNode Node { get => node; set => SetValue(ref node, value); }

        private string name;
        public string Name { get => name; set => SetValue(ref name, value); }

        private Guid id = Guid.NewGuid();
        public Guid Id { get => id; set => SetValue(ref id, value); }
    }
}
