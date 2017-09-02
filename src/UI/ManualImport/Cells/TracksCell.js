var _ = require('underscore');
var vent = require('../../vent');
var NzbDroneCell = require('../../Cells/NzbDroneCell');
var SelectTrackLayout = require('../Track/SelectTrackLayout');

module.exports = NzbDroneCell.extend({
    className : 'tracks-cell',

    events : {
        'click' : '_onClick'
    },

    render : function() {
        this.$el.empty();

        var tracks = this.model.get('tracks');

        if (tracks)
        {
            var trackNumbers = _.map(tracks, 'trackNumber');

            this.$el.html(trackNumbers.join(', '));
        }

        return this;
    },

    _onClick : function () {
        var artist = this.model.get('artist');
        var album = this.model.get('album');

        if (artist === undefined || album === undefined) {
            return;
        }

        var view =  new SelectTrackLayout({ artist: artist, album: album });

        this.listenTo(view, 'manualimport:selected:tracks', this._setTracks);

        vent.trigger(vent.Commands.OpenModal2Command, view);
    },

    _setTracks : function (e) {
        this.model.set('tracks', e.tracks);
    }
});