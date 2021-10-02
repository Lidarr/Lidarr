using System.Collections.Generic;
using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Languages
{
    public class LanguageFieldConverter
    {
        public List<FieldSelectOption> GetSelectOptions()
        {
            return Language.All.ConvertAll(v => new FieldSelectOption { Value = v.Id, Name = v.Name });
        }
    }
}
