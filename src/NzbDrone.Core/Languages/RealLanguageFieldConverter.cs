using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Languages
{
    public class RealLanguageFieldConverter
    {
        public List<FieldSelectOption> GetSelectOptions()
        {
            return Language.All
                .Where(l => l != Language.Unknown && l != Language.Any)
                .ToList()
                .ConvertAll(v => new FieldSelectOption { Value = v.Id, Name = v.Name });
        }
    }
}
