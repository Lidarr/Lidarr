using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Parser;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Core.Parser.Model;
using System;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class FingerprintingServiceFixture : CoreTest<FingerprintingService>
    {
        [TestCase("nin.mp3")]
        [TestCase("nin.flac")]
        public void should_fingerprint_file(string file)
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Media", file);
            
            var fingerprint = Subject.GetFingerprint(path);
            fingerprint.Fingerprint.Should().Be("AQACmomSJEySRNISHCa6zDh6OO_hoUfr48cvHD83-JApYL6F8oCMHvlg46qOHYf740L6ascN_fj0wh90aD2OC41FC-Exf0IvPBKyH6qNOGLEgrBxXC3w4MiFB2J--Cce4cYXpLwGXER64RHxQxT142eJFMeL40g_HSbUh7h6xONhuDZ-5NpxFrpwfCrSo4pj_EJYHoehvzjeHcbnDGoOH30WPEYeEz3xI72P0sLRNRHx49IB_IXx4_iH5yF-HOdx2FoE8R5-HM2hJmOF6hFSXziPHdtxmLmQ5sKYHzouFjlMokfOBncsaE91mPoQ_MctTCqRX9CEnziO_zhcCPk_POmOoz8u1KOQ6-ihazgaHw9OaPlxAvmKfDhUpcWH_MNbBr-Do0eYH7p-5PqFPUbD5TK2o6p0_MeP8IcOw0c-H8eRZxCjRniE8HgPhlKKxqmk4jnai8JZBO9SPMauFEeOHxeOFj_-NsgFdaHxIzx8PMzQRsQfRL4GHvoiNLnwXD6uA-Jx_DiM4-Wh5nBUIXx8vEMtBt60wWwQ3mh7ovE1PCmaHMdx9AiOM8SFXMfVC_oToDl64sqSsfiNB-Wh5_iT4xf-D8dh6PjxAP6CP8fjQf9ghM069IYfwxf8C5cQdwcXC5-CPNiP48WBKh_i49DN4Dz6C82Rx8mQH7-wgzls5VDznPhz4PBx_AAr-PjxVBHUB-aHQld2oWfQODnOoKcLX7iW4fHBFcfxHPOgH_Mt_HjEJDoq9cKH44nyQyfCjBeeVBP4VEUfvIvwIIueUYWTJviFH_lx_EeOQ7wG1sibwNGL7oh_6GzQTGKIkFm64CE-1TiYH_6OWHXw_Dh-2Dp0_TgR43mOXjOOkOGhZdKHPHiIvgy2H_0xJlsQX9Cu48gN__iR5_jBHj7U6wiVtyj9oemOz8LR_Ba-IM8VXDPCJ1PBRdMRyhe8aYZ-HP3R48fBvdCeHM9R7Qve48htuMoS0eiR-_jxoDkYLjoeJ9DUU_iA4zr44zec4_EOPcdTeAib4URPPLCPnseniUL0D6HSHf9xoz3wByF73Ic-VA_-fYh_ePwQ_cGlQD-ap-jDHOEpXENWwrEyPMe1I8ePHz_yQz-8D_-NrIbzo7uF-IcuohkZXBpyH89jnAJFHSH5DP9R5fh0FDrB42mGazj-bbhz6DwahGSKq_jxGwx3-EIsbnh-wdYO4YSPHz8OQszxPMSPhsf3HDl6zTCRhwr6g1fxPAej44txfLhw6JiPisfb4LhzlDy0ZDhj5NqIo3-w_UA6Cn2GKk6hHtcAH7-JH4cDNSkeHS9yH9_x4cyQ6yIemM5xBefx9HCqGT-OHzxOaDwefLfwpBfyC-EP_9CZo88snEPOHl-EHn4QMSd-XAN2PAe8HdBlH9SLrzm6HH-hK0d5mCrOoj9-MMe3hrhycPERjw9-YciJHx-EP8cTsRvOw5OOHjpjY14yEj0ycbBxZUXEVHB25BrM4McOhCwwCCDgjAQAFIOoAIICIQSAQiBDFELAEaYUMMtAKgUSACBnRJEQEGCAeAoRRIQQliFAsVYGGOKIcEYAAIVRgjgAJDNGKYCUBgIIKLwxCBHjCVDGECYuAsJBDhUSBhGhCACGKKOEEUUI4AAxHAkFHQEAQWeQJAYIYGQUAgBBgNGAMAEQMABRJwQSD2gCGAIMAOUIYhQBKwBjBCKmlCIBAACUYmYJZA0gRDgBCDDwWQcRAAAARJxACoFuBABOAACIwlY5Q4BwQAEAJDDYKZGQwogIyQwSABFmFDHDCAKAUMwooYAQABEgACPEAEORcQQ4aAAgHBRBpFFMECUoUJQZs4ARSBEFgUHKOUGAAYQ5QxAQyAFADDlAKgKMUAIQBxpQjBDLIBNCAGKsIBQRRQSSwjkFkCPDEOEQIUwKJJiywADhtHGAEaKAUBASxRRBxBgCDFBAKYWAAkIAYQQQBAkLBFEAKEIexEYxQYhFBghiAEDECCABMQAACh1QiBAgCDBUIEUZQAAAwpxYADFzAIAEIQEwkAQIAJhwylElhINSOMEEAwwggJVSAjHjgBQIEcMQcUARBJRT8AKBgAMAGKMIABIxQYVwhACAFbKAAUEAccAR4wmRlAjCiEEGMGIcEAQYZCSCTCInBQ");
            fingerprint.Duration.Should().BeApproximately(85.11, 0.1);
        }

        [TestCase("nin.mp3")]
        [TestCase("nin.flac")]
        public void should_lookup_file(string file)
        {
            UseRealHttp();

            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Media", file);
            var localTrack = new LocalTrack { Path = path };
            Subject.Lookup(new List<LocalTrack> { localTrack }, 0.5);
            localTrack.AcoustIdResults.Should().NotBeNull();
            localTrack.AcoustIdResults.Should().Contain("30f3f33e-8d0c-4e69-8539-cbd701d18f28");
        }

        [Test]
        public void should_lookup_list()
        {
            UseRealHttp();

            var files = new [] {
                "nin.mp3",
                "nin.flac"
            }.Select(x => new LocalTrack { Path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Media", x) }).ToList();
            Subject.Lookup(files, 0.5);

            files[0].AcoustIdResults.Should().Contain("30f3f33e-8d0c-4e69-8539-cbd701d18f28");
            files[1].AcoustIdResults.Should().Contain("30f3f33e-8d0c-4e69-8539-cbd701d18f28");
        }

        [Test]
        public void should_lookup_list_when_fpcalc_fails_for_some_files()
        {
            UseRealHttp();

            var files = new [] {
                "nin.mp3",
                "missing.mp3",
                "nin.flac"
            }.Select(x => new LocalTrack { Path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Media", x) }).ToList();

            var idpairs = files.Select(x => Tuple.Create(x, Subject.GetFingerprint(x.Path))).ToList();
            
            Subject.Lookup(idpairs, 0.5);

            files[0].AcoustIdResults.Should().Contain("30f3f33e-8d0c-4e69-8539-cbd701d18f28");
            files[1].AcoustIdResults.Should().BeNull();
            files[2].AcoustIdResults.Should().Contain("30f3f33e-8d0c-4e69-8539-cbd701d18f28");
        }

        [Test]
        public void should_lookup_list_when_fpcalc_fails_for_all_files()
        {
            UseRealHttp();

            var files = new [] {
                "missing1.mp3",
                "missing2.mp3"
            }.Select(x => new LocalTrack { Path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Media", x) }).ToList();

            var idpairs = files.Select(x => Tuple.Create<LocalTrack, AcoustId>(x, null)).ToList();
            
            Subject.Lookup(idpairs, 0.5);

            files[0].AcoustIdResults.Should().BeNull();
            files[1].AcoustIdResults.Should().BeNull();
        }
    }
}
