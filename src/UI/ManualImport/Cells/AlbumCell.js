var vent = require('../../vent');
var NzbDroneCell = require('../../Cells/NzbDroneCell');
var SelectAlbumLayout = require('../ALbum/SelectAlbumLayout');

module.exports = NzbDroneCell.extend({
    className : 'album-cell',

    events : {
        'click' : '_onClick'
    },

    render : function() {
        this.$el.empty();

        var album = this.model.get('album');

        if (album) 
        {
            this.$el.html(album.title);
        }

        this.delegateEvents();
        return this;
    },

    _onClick : function () {
        var artist = this.model.get('artist');

        if (!artist) {
            return;
        }

        var view = new SelectAlbumLayout({ artist: artist });

        this.listenTo(view, 'manualimport:selected:album', this._setAlbum);

        vent.trigger(vent.Commands.OpenModal2Command, view);
    },

    _setAlbum : function (e) {
        if (this.model.has('album') && e.model.id === this.model.get('album').id) {
            return;
        }

        this.model.set({
            album : e.model.toJSON(),
            tracks: []
        });
    }
});