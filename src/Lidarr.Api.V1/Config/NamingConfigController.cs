using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Lidarr.Http;
using Lidarr.Http.REST;
using Lidarr.Http.REST.Attributes;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Organizer;

namespace Lidarr.Api.V1.Config
{
    [V1ApiController("config/naming")]
    public class NamingConfigController : RestController<NamingConfigResource>
    {
        private readonly INamingConfigService _namingConfigService;
        private readonly IFilenameSampleService _filenameSampleService;
        private readonly IFilenameValidationService _filenameValidationService;
        private readonly IBuildFileNames _filenameBuilder;

        public NamingConfigController(INamingConfigService namingConfigService,
                                  IFilenameSampleService filenameSampleService,
                                  IFilenameValidationService filenameValidationService,
                                  IBuildFileNames filenameBuilder)
        {
            _namingConfigService = namingConfigService;
            _filenameSampleService = filenameSampleService;
            _filenameValidationService = filenameValidationService;
            _filenameBuilder = filenameBuilder;

            SharedValidator.RuleFor(c => c.StandardTrackFormat).ValidTrackFormat();
            SharedValidator.RuleFor(c => c.MultiDiscTrackFormat).ValidTrackFormat();
            SharedValidator.RuleFor(c => c.ArtistFolderFormat).ValidArtistFolderFormat();
        }

        public override NamingConfigResource GetResourceById(int id)
        {
            return GetNamingConfig();
        }

        [HttpGet]
        public NamingConfigResource GetNamingConfig()
        {
            var nameSpec = _namingConfigService.GetConfig();
            var resource = nameSpec.ToResource();

            if (resource.StandardTrackFormat.IsNotNullOrWhiteSpace())
            {
                var basicConfig = _filenameBuilder.GetBasicNamingConfig(nameSpec);
                basicConfig.AddToResource(resource);
            }

            if (resource.MultiDiscTrackFormat.IsNotNullOrWhiteSpace())
            {
                var basicConfig = _filenameBuilder.GetBasicNamingConfig(nameSpec);
                basicConfig.AddToResource(resource);
            }

            return resource;
        }

        [RestPutById]
        public ActionResult<NamingConfigResource> UpdateNamingConfig(NamingConfigResource resource)
        {
            var nameSpec = resource.ToModel();
            ValidateFormatResult(nameSpec);

            _namingConfigService.Save(nameSpec);

            return Accepted(resource.Id);
        }

        [HttpGet("examples")]
        public object GetExamples([FromQuery]NamingConfigResource config)
        {
            if (config.Id == 0)
            {
                config = GetNamingConfig();
            }

            var nameSpec = config.ToModel();
            var sampleResource = new NamingExampleResource();

            var singleTrackSampleResult = _filenameSampleService.GetStandardTrackSample(nameSpec);
            var multiDiscTrackSampleResult = _filenameSampleService.GetMultiDiscTrackSample(nameSpec);

            sampleResource.SingleTrackExample = _filenameValidationService.ValidateTrackFilename(singleTrackSampleResult) != null
                    ? null
                    : singleTrackSampleResult.FileName;

            sampleResource.MultiDiscTrackExample = _filenameValidationService.ValidateTrackFilename(multiDiscTrackSampleResult) != null
                    ? null
                    : multiDiscTrackSampleResult.FileName;

            sampleResource.ArtistFolderExample = nameSpec.ArtistFolderFormat.IsNullOrWhiteSpace()
                ? null
                : _filenameSampleService.GetArtistFolderSample(nameSpec);

            return sampleResource;
        }

        private void ValidateFormatResult(NamingConfig nameSpec)
        {
            var singleTrackSampleResult = _filenameSampleService.GetStandardTrackSample(nameSpec);

            var singleTrackValidationResult = _filenameValidationService.ValidateTrackFilename(singleTrackSampleResult);

            var validationFailures = new List<ValidationFailure>();

            validationFailures.AddIfNotNull(singleTrackValidationResult);

            if (validationFailures.Any())
            {
                throw new ValidationException(validationFailures.DistinctBy(v => v.PropertyName).ToArray());
            }
        }
    }
}
