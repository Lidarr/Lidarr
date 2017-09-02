var vent = require('../../vent');
var NzbDroneCell = require('../../Cells/NzbDroneCell');
var SelectArtistLayout = require('../Artist/SelectArtistLayout');

module.exports = NzbDroneCell.extend({
    className : 'artist-cell editable',

    events : {
        'click' : '_onClick'
    },

    render : function() {
        this.$el.empty();

        var artist = this.model.get('artist');

        if (artist)
        {
            this.$el.html(artist.name);
        }

        this.delegateEvents();
        return this;
    },

    _onClick : function () {
        var view = new SelectArtistLayout();

        this.listenTo(view, 'manualimport:selected:artist', this._setArtist);

        vent.trigger(vent.Commands.OpenModal2Command, view);
    },

    _setArtist : function (e) {
        if (this.model.has('artist') && e.model.id === this.model.get('artist').id) {
            return;
        }

        this.model.set({
            artist       : e.model.toJSON(),
            seasonNumber : undefined,
            tracks     : []
        });
    }
});