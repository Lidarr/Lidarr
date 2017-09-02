var _ = require('underscore');
var vent = require('vent');
var Marionette = require('marionette');
var Backgrid = require('backgrid');
var TrackCollection = require('../../Artist/TrackCollection');
var LoadingView = require('../../Shared/LoadingView');
var SelectAllCell = require('../../Cells/SelectAllCell');
var TrackNumberCell = require('../../Artist/Details/TrackNumberCell');
var RelativeDateCell = require('../../Cells/RelativeDateCell');
var SelectTrackRow = require('./SelectTrackRow');

module.exports = Marionette.Layout.extend({
    template  : 'ManualImport/Track/SelectTrackLayoutTemplate',

    regions : {
        tracks : '.x-tracks'
    },

    events : {
        'click .x-select' : '_selectTracks'
    },

    columns : [
        {
            name       : '',
            cell       : SelectAllCell,
            headerCell : 'select-all',
            sortable   : false
        },
        {
            name  : 'trackNumber',
            label : '#',
            cell  : TrackNumberCell
        },
        {
            name           : 'title',
            label          : 'Title',
            hideSeriesLink : true,
            cell           : 'string',
            sortable       : false
        }
    ],

    initialize : function(options) {
        this.artist = options.artist;
        this.album = options.album;
    },

    onRender : function() {
        this.tracks.show(new LoadingView());

        this.trackCollection = new TrackCollection({ artistId : this.artist.id, albumId : this.album.id });
        this.trackCollection.fetch();

        this.listenToOnce(this.trackCollection, 'sync', function () {

            this.trackView = new Backgrid.Grid({
                columns    : this.columns,
                collection : this.trackCollection,
                className  : 'table table-hover season-grid',
                row        : SelectTrackRow
            });

            this.tracks.show(this.trackView);
        });
    },

    _selectTracks : function () {
        var tracks = _.map(this.trackView.getSelectedModels(), function (track) {
            return track.toJSON();
        });

        this.trigger('manualimport:selected:tracks', { tracks: tracks });
        vent.trigger(vent.Commands.CloseModal2Command);
    }
});
