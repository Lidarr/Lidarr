using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.Validators;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Organizer
{
    public static class FileNameValidation
    {
        internal static readonly Regex OriginalTokenRegex = new Regex(@"(\{original[- ._](?:title|filename)\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IRuleBuilderOptions<T, string> ValidTrackFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            ruleBuilder.SetValidator(new NotEmptyValidator(null));
            ruleBuilder.SetValidator(new IllegalCharactersValidator());

            return ruleBuilder.SetValidator(new ValidStandardTrackFormatValidator());
        }

        public static IRuleBuilderOptions<T, string> ValidArtistFolderFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            ruleBuilder.SetValidator(new NotEmptyValidator(null));
            ruleBuilder.SetValidator(new IllegalCharactersValidator());

            return ruleBuilder.SetValidator(new RegularExpressionValidator(FileNameBuilder.ArtistNameRegex)).WithMessage("Must contain Artist name");
        }

        public static IRuleBuilderOptions<T, string> ValidAlbumFolderFormat<T>(this IRuleBuilder<T, string> ruleBuilder)
        {
            ruleBuilder.SetValidator(new NotEmptyValidator(null));
            ruleBuilder.SetValidator(new IllegalCharactersValidator());
            return ruleBuilder.SetValidator(new RegularExpressionValidator(FileNameBuilder.AlbumTitleRegex)).WithMessage("Must contain Album title");

            //.SetValidator(new RegularExpressionValidator(FileNameBuilder.ReleaseDateRegex)).WithMessage("Must contain Release year");
        }
    }

    public class ValidStandardTrackFormatValidator : PropertyValidator
    {
        public ValidStandardTrackFormatValidator()
            : base("Must contain Track Title and Track numbers OR Original Title")
        {
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            var value = context.PropertyValue as string;

            if (!(FileNameBuilder.TrackTitleRegex.IsMatch(value) &&
                FileNameBuilder.TrackRegex.IsMatch(value)) &&
                !FileNameValidation.OriginalTokenRegex.IsMatch(value))
            {
                return false;
            }

            return true;
        }
    }

    public class IllegalCharactersValidator : PropertyValidator
    {
        private readonly char[] _invalidPathChars = Path.GetInvalidPathChars();

        public IllegalCharactersValidator()
            : base("Contains illegal characters: {InvalidCharacters}")
        {
        }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            var value = context.PropertyValue as string;
            var invalidCharacters = new List<char>();

            if (value.IsNullOrWhiteSpace())
            {
                return true;
            }

            foreach (var i in _invalidPathChars)
            {
                if (value.IndexOf(i) >= 0)
                {
                    invalidCharacters.Add(i);
                }
            }

            if (invalidCharacters.Any())
            {
                context.MessageFormatter.AppendArgument("InvalidCharacters", string.Join("", invalidCharacters));
                return false;
            }

            return true;
        }
    }
}
