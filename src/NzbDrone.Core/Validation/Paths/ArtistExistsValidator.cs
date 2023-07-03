using FluentValidation.Validators;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Validation.Paths
{
    public class ArtistExistsValidator : PropertyValidator
    {
        private readonly IArtistService _artistService;

        public ArtistExistsValidator(IArtistService artistService)
        {
            _artistService = artistService;
        }

        protected override string GetDefaultMessageTemplate() => "This artist has already been added.";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            var foreignArtistId = context.PropertyValue.ToString();

            return _artistService.FindById(foreignArtistId) == null;
        }
    }
}
