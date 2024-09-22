using FluentValidation.Validators;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Validation.Paths
{
    public class AlbumExistsValidator : PropertyValidator
    {
        private readonly IAlbumService _albumService;

        public AlbumExistsValidator(IAlbumService albumService)
        {
            _albumService = albumService;
        }

        protected override string GetDefaultMessageTemplate() => "This album has already been added.";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            var foreignAlbumId = context.PropertyValue.ToString();

            return _albumService.FindById(foreignAlbumId) == null;
        }
    }
}
