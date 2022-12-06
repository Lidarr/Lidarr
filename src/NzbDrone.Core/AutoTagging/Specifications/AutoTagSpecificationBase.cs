using NzbDrone.Core.Music;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.AutoTagging.Specifications
{
    public abstract class AutoTaggingSpecificationBase : IAutoTaggingSpecification
    {
        public abstract int Order { get; }
        public abstract string ImplementationName { get; }

        public string Name { get; set; }
        public bool Negate { get; set; }
        public bool Required { get; set; }

        public IAutoTaggingSpecification Clone()
        {
            return (IAutoTaggingSpecification)MemberwiseClone();
        }

        public abstract NzbDroneValidationResult Validate();

        public bool IsSatisfiedBy(Artist artist)
        {
            var match = IsSatisfiedByWithoutNegate(artist);

            if (Negate)
            {
                match = !match;
            }

            return match;
        }

        protected abstract bool IsSatisfiedByWithoutNegate(Artist artist);
    }
}
