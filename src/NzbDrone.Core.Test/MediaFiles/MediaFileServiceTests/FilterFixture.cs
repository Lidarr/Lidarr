using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Test.Common;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Abstractions;

namespace NzbDrone.Core.Test.MediaFiles.MediaFileServiceTests
{
    [TestFixture]
    public class FilterFixture : FileSystemTest<MediaFileService>
    {
        private Artist _artist;

        [SetUp]
        public void Setup()
        {
            _artist = new Artist
                      {
                          Id = 10,
                          Path = @"C:\".AsOsAgnostic()
                      };
        }

        private List<FileInfoBase> GivenFiles(string[] files)
        {
            foreach (var file in files)
            {
                FileSystem.AddFile(file, new MockFileData(string.Empty));
            }
            
            return files.Select(x => DiskProvider.GetFileInfo(x)).ToList();
        }

        [Test]
        public void filter_should_return_all_files_if_no_existing_files()
        {
            var files = GivenFiles(new []
                {
                    "C:\\file1.avi".AsOsAgnostic(),
                    "C:\\file2.avi".AsOsAgnostic(),
                    "C:\\file3.avi".AsOsAgnostic()
                });

            Mocker.GetMock<IMediaFileRepository>()
                .Setup(c => c.GetFilesByArtist(It.IsAny<int>()))
                .Returns(new List<TrackFile>());

            Subject.FilterExistingFiles(files, _artist).Should().BeEquivalentTo(files);
        }

        [Test]
        public void filter_should_return_none_if_all_files_exist()
        {
            var files = GivenFiles(new []
                {
                    "C:\\file1.avi".AsOsAgnostic(),
                    "C:\\file2.avi".AsOsAgnostic(),
                    "C:\\file3.avi".AsOsAgnostic()
                });

            Mocker.GetMock<IMediaFileRepository>()
                .Setup(c => c.GetFilesByArtist(It.IsAny<int>()))
                .Returns(files.Select(f => new TrackFile { RelativePath = f.Name }).ToList());

            Subject.FilterExistingFiles(files, _artist).Should().BeEmpty();
        }

        [Test]
        public void filter_should_return_none_existing_files()
        {
            var files = GivenFiles(new []
                {
                    "C:\\file1.avi".AsOsAgnostic(),
                    "C:\\file2.avi".AsOsAgnostic(),
                    "C:\\file3.avi".AsOsAgnostic()
                });

            Mocker.GetMock<IMediaFileRepository>()
                .Setup(c => c.GetFilesByArtist(It.IsAny<int>()))
                .Returns(new List<TrackFile>
                {
                    new TrackFile{ RelativePath = "file2.avi".AsOsAgnostic()}
                });

            Subject.FilterExistingFiles(files, _artist).Should().HaveCount(2);
            Subject.FilterExistingFiles(files, _artist).Select(x => x.FullName).Should().NotContain("C:\\file2.avi".AsOsAgnostic());
        }

        [Test]
        public void filter_should_return_none_existing_files_ignoring_case()
        {
            WindowsOnly();

            var files = GivenFiles(new []
                {
                    "C:\\file1.avi".AsOsAgnostic(),
                    "C:\\FILE2.avi".AsOsAgnostic(),
                    "C:\\file3.avi".AsOsAgnostic()
                });

            Mocker.GetMock<IMediaFileRepository>()
                .Setup(c => c.GetFilesByArtist(It.IsAny<int>()))
                .Returns(new List<TrackFile>
                {
                    new TrackFile{ RelativePath = "file2.avi".AsOsAgnostic()}
                });


            Subject.FilterExistingFiles(files, _artist).Should().HaveCount(2);
            Subject.FilterExistingFiles(files, _artist).Select(x => x.FullName).Should().NotContain("C:\\file2.avi".AsOsAgnostic());
        }

        [Test]
        public void filter_should_return_none_existing_files_not_ignoring_case()
        {
            MonoOnly();

            var files = GivenFiles(new []
                {
                    "C:\\file1.avi".AsOsAgnostic(),
                    "C:\\FILE2.avi".AsOsAgnostic(),
                    "C:\\file3.avi".AsOsAgnostic()
                });

            Mocker.GetMock<IMediaFileRepository>()
                .Setup(c => c.GetFilesByArtist(It.IsAny<int>()))
                .Returns(new List<TrackFile>
                {
                    new TrackFile{ RelativePath = "file2.avi".AsOsAgnostic()}
                });

            Subject.FilterExistingFiles(files, _artist).Should().HaveCount(3);
        }

        [Test]
        public void filter_should_not_change_casing()
        {
            var files = GivenFiles(new []
                {
                    "C:\\FILE1.avi".AsOsAgnostic()
                });

            Mocker.GetMock<IMediaFileRepository>()
                .Setup(c => c.GetFilesByArtist(It.IsAny<int>()))
                .Returns(new List<TrackFile>());

            Subject.FilterExistingFiles(files, _artist).Should().HaveCount(1);
            Subject.FilterExistingFiles(files, _artist).Select(x => x.FullName).Should().NotContain(files.First().FullName.ToLower());
            Subject.FilterExistingFiles(files, _artist).Should().Contain(files.First());
        }

        [Test]
        public void filter_should_not_return_existing_file_if_size_unchanged()
        {
            FileSystem.AddFile("C:\\file1.avi".AsOsAgnostic(), new MockFileData("".PadRight(10)));
            FileSystem.AddFile("C:\\file2.avi".AsOsAgnostic(), new MockFileData("".PadRight(10)));
            FileSystem.AddFile("C:\\file3.avi".AsOsAgnostic(), new MockFileData("".PadRight(10)));

            var files = FileSystem.AllFiles.Select(x => DiskProvider.GetFileInfo(x)).ToList();

            Mocker.GetMock<IMediaFileRepository>()
                .Setup(c => c.GetFilesByArtist(It.IsAny<int>()))
                .Returns(new List<TrackFile>
                {
                    new TrackFile{
                        RelativePath = "file2.avi".AsOsAgnostic(),
                        Size = 10
                    }
                });

            Subject.FilterExistingFiles(files, _artist).Should().HaveCount(2);
            Subject.FilterExistingFiles(files, _artist).Select(x => x.FullName).Should().NotContain("C:\\file2.avi".AsOsAgnostic());
        }

        [Test]
        public void filter_should_return_existing_file_if_size_unchanged()
        {
            FileSystem.AddFile("C:\\file1.avi".AsOsAgnostic(), new MockFileData("".PadRight(10)));
            FileSystem.AddFile("C:\\file2.avi".AsOsAgnostic(), new MockFileData("".PadRight(11)));
            FileSystem.AddFile("C:\\file3.avi".AsOsAgnostic(), new MockFileData("".PadRight(10)));

            var files = FileSystem.AllFiles.Select(x => DiskProvider.GetFileInfo(x)).ToList();

            Mocker.GetMock<IMediaFileRepository>()
                .Setup(c => c.GetFilesByArtist(It.IsAny<int>()))
                .Returns(new List<TrackFile>
                {
                    new TrackFile{
                        RelativePath = "file2.avi".AsOsAgnostic(),
                        Size = 10
                    }
                });

            Subject.FilterExistingFiles(files, _artist).Should().HaveCount(3);
            Subject.FilterExistingFiles(files, _artist).Select(x => x.FullName).Should().Contain("C:\\file2.avi".AsOsAgnostic());
        }

    }
}
