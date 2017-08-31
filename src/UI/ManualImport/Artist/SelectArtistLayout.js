var _ = require('underscore');
var vent = require('vent');
var Marionette = require('marionette');
var Backgrid = require('backgrid');
var ArtistCollection = require('../../Artist/ArtistCollection');
var SelectRow = require('./SelectArtistRow');

module.exports = Marionette.Layout.extend({
    template  : 'ManualImport/Artist/SelectArtistLayoutTemplate',

    regions : {
        artist : '.x-artist'
    },

    ui : {
        filter : '.x-filter'
    },

    columns : [
        {
            name      : 'name',
            label     : 'Name',
            cell      : 'String',
            sortValue : 'sortName'
        }
    ],

    initialize : function() {
        this.artistCollection = ArtistCollection.clone();
        this._setModelCollection();

        this.listenTo(this.artistCollection, 'row:selected', this._onSelected);
        this.listenTo(this, 'modal:afterShow', this._setFocus);
    },

    onRender : function() {
        this.artistView = new Backgrid.Grid({
            columns    : this.columns,
            collection : this.artistCollection,
            className  : 'table table-hover season-grid',
            row        : SelectRow
        });

        this.artist.show(this.artistView);
        this._setupFilter();
    },

    _setupFilter : function () {
        var self = this;

        //TODO: This should be a mixin (same as Add Series searching)
        this.ui.filter.keyup(function(e) {
            if (_.contains([
                    9,
                    16,
                    17,
                    18,
                    19,
                    20,
                    33,
                    34,
                    35,
                    36,
                    37,
                    38,
                    39,
                    40,
                    91,
                    92,
                    93
                ], e.keyCode)) {
                return;
            }

            self._filter(self.ui.filter.val());
        });
    },

    _filter : function (term) {
        this.artistCollection.setFilter(['name', term, 'contains']);
        this._setModelCollection();
    },

    _onSelected : function (e) {
        this.trigger('manualimport:selected:artist', { model: e.model });

        vent.trigger(vent.Commands.CloseModal2Command);
    },

    _setFocus : function () {
        this.ui.filter.focus();
    },
    
    _setModelCollection: function () {
        var self = this;
        
        _.each(this.artistCollection.models, function (model) {
            model.collection = self.artistCollection;
        });
    }
});
