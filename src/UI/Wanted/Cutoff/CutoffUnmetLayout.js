var _ = require('underscore');
var Marionette = require('marionette');
var Backgrid = require('backgrid');
var CutoffUnmetCollection = require('./CutoffUnmetCollection');
var SelectAllCell = require('../../Cells/SelectAllCell');
var SeriesTitleCell = require('../../Cells/SeriesTitleCell');
var EpisodeNumberCell = require('../../Cells/EpisodeNumberCell');
var EpisodeTitleCell = require('../../Cells/EpisodeTitleCell');
var RelativeDateCell = require('../../Cells/RelativeDateCell');
var EpisodeStatusCell = require('../../Cells/EpisodeStatusCell');
var GridPager = require('../../Shared/Grid/Pager');
var ToolbarLayout = require('../../Shared/Toolbar/ToolbarLayout');
var LoadingView = require('../../Shared/LoadingView');
var Messenger = require('../../Shared/Messenger');
var CommandController = require('../../Commands/CommandController');
require('backgrid.selectall');
require('../../Mixins/backbone.signalr.mixin');

module.exports = Marionette.Layout.extend({
    template : 'Wanted/Cutoff/CutoffUnmetLayoutTemplate',

    regions : {
        cutoff  : '#x-cutoff-unmet',
        toolbar : '#x-toolbar',
        pager   : '#x-pager'
    },

    ui : {
        searchSelectedButton : '.btn i.icon-lidarr-search'
    },

    columns : [
        {
            name       : '',
            cell       : SelectAllCell,
            headerCell : 'select-all',
            sortable   : false
        },
        {
            name      : 'series',
            label     : 'Artist',
            cell      : SeriesTitleCell,
            sortValue : 'series.sortTitle'
        },
//        {
//            name     : 'this',
//            label    : 'Track Number',
//            cell     : EpisodeNumberCell,
//            sortable : false
//        },
        {
            name     : 'this',
            label    : 'Track Title',
            cell     : EpisodeTitleCell,
            sortable : false
        },
        {
            name  : 'airDateUtc',
            label : 'Release Date',
            cell  : RelativeDateCell
        },
        {
            name     : 'status',
            label    : 'Status',
            cell     : EpisodeStatusCell,
            sortable : false
        }
    ],

    initialize : function() {
        this.collection = new CutoffUnmetCollection().bindSignalR({ updateOnly : true });

        this.listenTo(this.collection, 'sync', this._showTable);
    },

    onShow : function() {
        this.cutoff.show(new LoadingView());
        this._showToolbar();
        this.collection.fetch();
    },

    _showTable : function() {
        this.cutoffGrid = new Backgrid.Grid({
            columns    : this.columns,
            collection : this.collection,
            className  : 'table table-hover'
        });

        this.cutoff.show(this.cutoffGrid);

        this.pager.show(new GridPager({
            columns    : this.columns,
            collection : this.collection
        }));
    },

    _showToolbar : function() {
        var leftSideButtons = {
            type       : 'default',
            storeState : false,
            items      : [
                {
                    title        : 'Search Selected',
                    icon         : 'icon-lidarr-search',
                    callback     : this._searchSelected,
                    ownerContext : this,
                    className    : 'x-search-selected'
                },
                {
                    title : 'Album Studio',
                    icon  : 'icon-lidarr-monitored',
                    route : 'albumstudio'
                }
            ]
        };

        var filterOptions = {
            type          : 'radio',
            storeState    : false,
            menuKey       : 'wanted.filterMode',
            defaultAction : 'monitored',
            items         : [
                {
                    key      : 'monitored',
                    title    : '',
                    tooltip  : 'Monitored Only',
                    icon     : 'icon-lidarr-monitored',
                    callback : this._setFilter
                },
                {
                    key      : 'unmonitored',
                    title    : '',
                    tooltip  : 'Unmonitored Only',
                    icon     : 'icon-lidarr-unmonitored',
                    callback : this._setFilter
                }
            ]
        };

        this.toolbar.show(new ToolbarLayout({
            left    : [
                leftSideButtons
            ],
            right   : [
                filterOptions
            ],
            context : this
        }));

        CommandController.bindToCommand({
            element : this.$('.x-search-selected'),
            command : {
                name : 'episodeSearch'
            }
        });
    },

    _setFilter : function(buttonContext) {
        var mode = buttonContext.model.get('key');

        this.collection.state.currentPage = 1;
        var promise = this.collection.setFilterMode(mode);

        if (buttonContext) {
            buttonContext.ui.icon.spinForPromise(promise);
        }
    },

    _searchSelected : function() {
        var selected = this.cutoffGrid.getSelectedModels();

        if (selected.length === 0) {
            Messenger.show({
                type    : 'error',
                message : 'No episodes selected'
            });

            return;
        }

        var ids = _.pluck(selected, 'id');

        CommandController.Execute('episodeSearch', {
            name       : 'episodeSearch',
            episodeIds : ids
        });
    }
});