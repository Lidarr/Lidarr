using System.Collections.Generic;
using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class MoveArtistServiceFixture : CoreTest<MoveArtistService>
    {
        private Artist _artist;
        private MoveArtistCommand _command;
        private BulkMoveArtistCommand _bulkCommand;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Artist>
                .CreateNew()
                .Build();

            _command = new MoveArtistCommand
            {
                ArtistId = _artist.Id,
                SourcePath = @"C:\Test\Music\Artist".AsOsAgnostic(),
                DestinationPath = @"C:\Test\Music2\Artist".AsOsAgnostic(),
                MoveFiles = true
            };

            _bulkCommand = new BulkMoveArtistCommand
            {
                Artist = new List<BulkMoveArtist>
                {
                    new BulkMoveArtist
                    {
                        ArtistId = _artist.Id,
                        SourcePath = @"C:\Test\Music\Artist".AsOsAgnostic()
                    }
                },
                DestinationRootFolder = @"C:\Test\Music2".AsOsAgnostic(),
                MoveFiles = true
            };

            Mocker.GetMock<IArtistService>()
                .Setup(s => s.GetArtist(It.IsAny<int>()))
                .Returns(_artist);

            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(It.IsAny<string>()))
                .Returns(true);
        }

        private void GivenFailedMove()
        {
            Mocker.GetMock<IDiskTransferService>()
                .Setup(s => s.TransferFolder(It.IsAny<string>(), It.IsAny<string>(), TransferMode.Move))
                .Throws<IOException>();
        }

        [Test]
        public void should_log_error_when_move_throws_an_exception()
        {
            GivenFailedMove();

            Subject.Execute(_command);

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_revert_artist_path_on_error()
        {
            GivenFailedMove();

            Subject.Execute(_command);

            ExceptionVerification.ExpectedErrors(1);

            Mocker.GetMock<IArtistService>()
                .Verify(v => v.UpdateArtist(It.IsAny<Artist>(), It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void should_use_destination_path()
        {
            Subject.Execute(_command);

            Mocker.GetMock<IDiskTransferService>()
                .Verify(
                    v => v.TransferFolder(_command.SourcePath,
                                          _command.DestinationPath,
                                          TransferMode.Move),
                    Times.Once());

            Mocker.GetMock<IBuildFileNames>()
                .Verify(v => v.GetArtistFolder(It.IsAny<Artist>(), null), Times.Never());
        }

        [Test]
        public void should_build_new_path_when_root_folder_is_provided()
        {
            var artistFolder = "Artist";
            var expectedPath = Path.Combine(_bulkCommand.DestinationRootFolder, artistFolder);

            Mocker.GetMock<IBuildFileNames>()
                .Setup(s => s.GetArtistFolder(It.IsAny<Artist>(), null))
                .Returns(artistFolder);

            Subject.Execute(_bulkCommand);

            Mocker.GetMock<IDiskTransferService>()
                .Verify(
                    v => v.TransferFolder(_bulkCommand.Artist.First().SourcePath,
                                          expectedPath,
                                          TransferMode.Move),
                    Times.Once());
        }

        [Test]
        public void should_skip_artist_folder_if_it_does_not_exist()
        {
            Mocker.GetMock<IDiskProvider>()
                .Setup(s => s.FolderExists(It.IsAny<string>()))
                .Returns(false);

            Subject.Execute(_command);

            Mocker.GetMock<IDiskTransferService>()
                .Verify(
                    v => v.TransferFolder(_command.SourcePath,
                        _command.DestinationPath,
                        TransferMode.Move), Times.Never());

            Mocker.GetMock<IBuildFileNames>()
                .Verify(v => v.GetArtistFolder(It.IsAny<Artist>(), null), Times.Never());
        }

        [Test]
        public void should_raise_artist_moved_event_when_move_files_false()
        {
            _command.MoveFiles = false;
            Subject.Execute(_command);

            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent(It.Is<ArtistMovedEvent>(c => c.Artist.Id == _artist.Id)), Times.Once());
        }

        [Test]
        public void should_raise_artist_moved_event_when_move_files_false_bulk()
        {
            _bulkCommand.MoveFiles = false;

            var artistFolder = "Artist";
            var expectedPath = Path.Combine(_bulkCommand.DestinationRootFolder, artistFolder);

            Mocker.GetMock<IBuildFileNames>()
                .Setup(s => s.GetArtistFolder(It.IsAny<Artist>(), null))
                .Returns(artistFolder);

            Subject.Execute(_bulkCommand);

            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent(It.Is<ArtistMovedEvent>(c => c.Artist.Id == _artist.Id)), Times.Once());
        }
    }
}
