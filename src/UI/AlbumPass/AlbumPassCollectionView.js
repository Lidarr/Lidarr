var _ = require('underscore');
var Marionette = require('marionette');
var AlbumLayout = require('./SingleAlbumCell');
var AsSortedCollectionView = require('../Mixins/AsSortedCollectionView');

var view = Marionette.CollectionView.extend({

    itemView : AlbumLayout,

    initialize : function(options) {
        this.albumCollection = options.collection;
        this.artist = options.artist;
        console.log(this);
    },

    itemViewOptions : function() {
        return {
            albumCollection   : this.albumCollection,
            artist            : this.artist
        };
    }
});

AsSortedCollectionView.call(view);

module.exports = view;