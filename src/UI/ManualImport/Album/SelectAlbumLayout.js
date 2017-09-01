var _ = require('underscore');
var vent = require('vent');
var Marionette = require('marionette');
var Backgrid = require('backgrid');
var LoadingView = require('../../Shared/LoadingView');
var AlbumCollection = require('../../Artist/AlbumCollection');
var SelectAlbumRow = require('./SelectAlbumRow');

module.exports = Marionette.Layout.extend({
    template  : 'ManualImport/Album/SelectAlbumLayoutTemplate',

    regions : {
        album : '.x-album'
    },

    ui : {
        filter : '.x-filter'
    },

    columns : [
        {
            name      : 'title',
            label     : 'Title',
            cell      : 'String',
            sortValue : 'title' //TODO Change to Sort Title
        }
    ],

    initialize : function(options) {
        this.artist = options.artist;
        this.albumCollection = new AlbumCollection({ artistId : this.artist.id });
        this.albumCollection.fetch();
        this._setModelCollection();

        this.listenTo(this.albumCollection, 'row:selected', this._onSelected);
        this.listenTo(this, 'modal:afterShow', this._setFocus);
    },

    onRender : function() {


        this.albumView = new Backgrid.Grid({
            columns    : this.columns,
            collection : this.albumCollection,
            className  : 'table table-hover album-grid',
            row        : SelectAlbumRow
        });

        this.album.show(this.albumView);
    },

    _onSelected : function (e) {
        this.trigger('manualimport:selected:album', { model: e.model });

        vent.trigger(vent.Commands.CloseModal2Command);
    },

    _setFocus : function () {
        this.ui.filter.focus();
    },
    
    _setModelCollection: function () {
        var self = this;
        
        _.each(this.albumCollection.models, function (model) {
            model.collection = self.albumCollection;
        });
    }
});
