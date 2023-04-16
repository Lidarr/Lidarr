using System.Linq;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Validation.Paths
{
    public class ArtistAncestorValidator : PropertyValidator
    {
        private readonly IArtistService _artistService;

        public ArtistAncestorValidator(IArtistService artistService)
        {
            _artistService = artistService;
        }

        protected override string GetDefaultMessageTemplate() => "Path is an ancestor of an existing artist";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            return !_artistService.AllArtistPaths().Any(s => context.PropertyValue.ToString().IsParentPath(s.Value));
        }
    }
}
