var _ = require('underscore');
var PageableCollection = require('backbone.pageable');
var MovieModel = require('../../Artist/ArtistModel');
var AsSortedCollection = require('../../Mixins/AsSortedCollection');
var AsPageableCollection = require('../../Mixins/AsPageableCollection');
var AsPersistedStateCollection = require('../../Mixins/AsPersistedStateCollection');

var BulkImportCollection = PageableCollection.extend({
		url   : window.NzbDrone.ApiRoot + '/artist/bulkimport',
		model : MovieModel,
		tableName : 'bulkimport',

		state : {
			pageSize : 100000,
			sortKey: 'sortName',
			firstPage: 1
		},

		fetch : function(options) {

			options = options || {};

			var data = options.data || {};

			if (data.id === undefined || data.folder === undefined) {
				data.id = this.folderId;
				data.folder = this.folder;
			}

			options.data = data;
			console.log(this);
			return PageableCollection.prototype.fetch.call(this, options);
		},

		parseLinks : function(options) {
			
			return {
				first : this.url,
				next: this.url,
				last : this.url
			};
		}
});


BulkImportCollection = AsSortedCollection.call(BulkImportCollection);
BulkImportCollection = AsPageableCollection.call(BulkImportCollection);
BulkImportCollection = AsPersistedStateCollection.call(BulkImportCollection);

module.exports = BulkImportCollection;
