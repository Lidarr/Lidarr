using System.Linq;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Validation.Paths
{
    public class ArtistPathValidator : PropertyValidator
    {
        private readonly IArtistService _artistService;

        public ArtistPathValidator(IArtistService artistService)
        {
            _artistService = artistService;
        }

        protected override string GetDefaultMessageTemplate() => "Path '{path}' is already configured for an existing artist";

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
            {
                return true;
            }

            context.MessageFormatter.AppendArgument("path", context.PropertyValue.ToString());

            dynamic instance = context.ParentContext.InstanceToValidate;
            var instanceId = (int)instance.Id;

            return !_artistService.AllArtistPaths().Any(s => s.Value.PathEquals(context.PropertyValue.ToString()) && s.Key != instanceId);
        }
    }
}
