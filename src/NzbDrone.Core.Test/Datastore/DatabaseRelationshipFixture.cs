﻿using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class DatabaseRelationshipFixture : DbTest
    {
        [Test]
        public void one_to_one()
        {
            var episodeFile = Builder<EpisodeFile>.CreateNew()
                           .With(c => c.Quality = new QualityModel())
                           .BuildNew();

            Db.Insert(episodeFile);

            var episode = Builder<Episode>.CreateNew()
                .With(c => c.EpisodeFileId = episodeFile.Id)
                .BuildNew();

            Db.Insert(episode);

            var loadedEpisodeFile = Db.Single<Episode>().EpisodeFile.Value;

            loadedEpisodeFile.Should().NotBeNull();
            loadedEpisodeFile.ShouldBeEquivalentTo(episodeFile,
                options => options
                    .IncludingAllRuntimeProperties()
                    .Excluding(c => c.DateAdded)
                    .Excluding(c => c.Path)
                    .Excluding(c => c.Series)
                    .Excluding(c => c.Episodes));
        }

        [Test]
        public void one_to_one_should_not_query_db_if_foreign_key_is_zero()
        {
            var episode = Builder<Episode>.CreateNew()
                .With(c => c.EpisodeFileId = 0)
                .BuildNew();

            Db.Insert(episode);

            Db.Single<Episode>().EpisodeFile.Value.Should().BeNull();
        }


        [Test]
        public void embedded_document_as_json()
        {
            var quality = new QualityModel { Quality = Quality.MP3_320, Revision = new Revision(version: 2 )};

            var history = Builder<History.History>.CreateNew()
                            .With(c => c.Id = 0)
                            .With(c => c.Quality = quality)
                            .Build();

            Db.Insert(history);

            var loadedQuality = Db.Single<History.History>().Quality;
            loadedQuality.Should().Be(quality);
        }

        [Test]
        public void embedded_list_of_document_with_json()
        {
            var history = Builder<History.History>.CreateListOfSize(2)
                            .All().With(c => c.Id = 0)
                            .Build().ToList();

            history[0].Quality = new QualityModel(Quality.MP3_512, new Revision(version: 2));
            history[1].Quality = new QualityModel(Quality.MP3_320, new Revision(version: 2));


            Db.InsertMany(history);

            var returnedHistory = Db.All<History.History>();

            returnedHistory[0].Quality.Quality.Should().Be(Quality.MP3_512);
        }
    }
}
