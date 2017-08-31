var _ = require('underscore');
var Marionette = require('marionette');

module.exports = Marionette.ItemView.extend({
    template  : 'ManualImport/Summary/ManualImportSummaryViewTemplate',

    initialize : function (options) {
        var tracks = _.map(options.tracks, function (track) {
                return track.toJSON();
            });

        this.templateHelpers = {
            file     : options.file,
            artist   : options.artist,
            album    : options.album,
            tracks   : tracks,
            quality  : options.quality
        };
    }
});