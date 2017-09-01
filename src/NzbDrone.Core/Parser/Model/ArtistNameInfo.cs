using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.Parser.Model
{
    public class ArtistNameInfo
    {
        public string Name { get; set; }
        public string NameWithoutYear { get; set; }
        public int Year { get; set; }
    }
}
