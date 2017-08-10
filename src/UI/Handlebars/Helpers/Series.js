var Handlebars = require('handlebars');
var StatusModel = require('../../System/StatusModel');
var _ = require('underscore');

Handlebars.registerHelper('poster', function() {

    var placeholder = StatusModel.get('urlBase') + '/Content/Images/poster-dark.png';
    var poster = _.where(this.images, { coverType : 'poster' });

    if (poster[0]) {
        if (!poster[0].url.match(/^https?:\/\//)) {
            return new Handlebars.SafeString('<img class="series-poster x-series-poster" {0}>'.format(Handlebars.helpers.defaultImg.call(null, poster[0].url, 250)));
        } else {
            var url = poster[0].url.replace(/^https?\:/, '');
            return new Handlebars.SafeString('<img class="series-poster x-series-poster" {0}>'.format(Handlebars.helpers.defaultImg.call(null, url)));
        }
    }

    return new Handlebars.SafeString('<img class="series-poster placeholder-image" src="{0}">'.format(placeholder));
});

Handlebars.registerHelper('traktUrl', function() {
    return 'http://trakt.tv/search/tvdb/' + this.tvdbId + '?id_type=show';
});

Handlebars.registerHelper('imdbUrl', function() {
    return 'http://imdb.com/title/' + this.imdbId;
});

Handlebars.registerHelper('tvdbUrl', function() {
    return 'http://www.thetvdb.com/?tab=series&id=' + this.tvdbId;
});

Handlebars.registerHelper('tvRageUrl', function() {
    return 'http://www.tvrage.com/shows/id-' + this.tvRageId;
});

Handlebars.registerHelper('tvMazeUrl', function() {
    return 'http://www.tvmaze.com/shows/' + this.tvMazeId + '/_';
});

Handlebars.registerHelper('route', function() {
    return StatusModel.get('urlBase') + '/series/' + this.titleSlug;
});

Handlebars.registerHelper('percentOfEpisodes', function() {
    var episodeCount = this.episodeCount;
    var episodeFileCount = this.episodeFileCount;

    var percent = 100;

    if (episodeCount > 0) {
        percent = episodeFileCount / episodeCount * 100;
    }

    return percent;
});

Handlebars.registerHelper('seasonCountHelper', function() {
    var seasonCount = this.seasonCount;
    var continuing = this.status === 'continuing';

    if (continuing) {
        return new Handlebars.SafeString('<span class="label label-info">Season {0}</span>'.format(seasonCount));
    }

    if (seasonCount === 1) {
        return new Handlebars.SafeString('<span class="label label-info">{0} Season</span>'.format(seasonCount));
    }

    return new Handlebars.SafeString('<span class="label label-info">{0} Seasons</span>'.format(seasonCount));
});

Handlebars.registerHelper ('truncate', function (str, len) {
    if (str && str.length > len && str.length > 0) {
        var new_str = str + " ";
        new_str = str.substr (0, len);
        new_str = str.substr (0, new_str.lastIndexOf(" "));
        new_str = (new_str.length > 0) ? new_str : str.substr (0, len);

        return new Handlebars.SafeString ( new_str +'...' ); 
    }
    return str;
});

/*Handlebars.registerHelper('titleWithYear', function() {
    if (this.title.endsWith(' ({0})'.format(this.year))) {
        return this.title;
    }

    if (!this.year) {
        return this.title;
    }

    return new Handlebars.SafeString('{0} <span class="year">({1})</span>'.format(this.title, this.year));
});*/
