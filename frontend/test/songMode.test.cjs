const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { test } = require('node:test');
const { transformSync } = require('@babel/core');
const React = require('react');

const root = path.resolve(__dirname, '../src');
const cache = new Map();
const translations = JSON.parse(fs.readFileSync(path.resolve(__dirname, '../../src/NzbDrone.Core/Localization/Core/en.json'), 'utf8'));

// Load the real selection logic/components without a browser or the Redux app.
function load(file) {
  file = path.resolve(file);
  if (cache.has(file)) return cache.get(file).exports;
  const module = { exports: {} };
  cache.set(file, module);
  const compiled = transformSync(fs.readFileSync(file, 'utf8'), {
    configFile: false,
    babelrc: false,
    presets: [['@babel/preset-env', { targets: { node: 'current' } }], '@babel/preset-react']
  }).code;
  function localRequire(name) {
    if (['react', 'prop-types', 'classnames', 'lodash', 'reselect'].includes(name)) return require(name);
    if (name === 'react-redux') return { connect: () => (component) => component };
    if (name === 'Album/Details/songAvailability') return load(path.join(root, 'Album/Details/songAvailability.js'));
    if (name === './songAvailability') return load(path.join(path.dirname(file), 'songAvailability.js'));
    if (name.endsWith('.css')) return { __esModule: true, default: new Proxy({}, { get: (_, key) => key }) };
    if (name === 'Helpers/Props') return { kinds: { INFO: 'info', SUCCESS: 'success', PRIMARY: 'primary', DANGER: 'danger' }, sizes: { LARGE: 'large' }, icons: {} };
    if (name === 'Utilities/String/translate') return (key, args = []) => (translations[key] || key).replace(/\{(\d+)\}/g, (_, index) => args[index]);
    if (name.startsWith('Utilities/Table/')) return load(path.join(root, `${name}.js`));
    if (file.includes(`${path.sep}Utilities${path.sep}Table${path.sep}`) && name.startsWith('./')) return load(path.join(path.dirname(file), `${name}.js`));
    if (name === './songSelection') return load(path.join(path.dirname(file), 'songSelection.js'));
    if (name === './trackRowSelection') return load(path.join(path.dirname(file), 'trackRowSelection.js'));
    if (name === './artistLibraryView') return load(path.join(path.dirname(file), 'artistLibraryView.js'));
    return function Stub() { return null; };
  }
  new Function('require', 'module', 'exports', compiled)(localRequire, module, module.exports);
  return module.exports;
}

const { createSongSelection, filterSongs, saveSongSelection, downloadSongSelection } = load(path.join(root, 'Artist/Details/songSelection.js'));
const { getArtistLibraryView, getArtistLibraryViewLink } = load(path.join(root, 'Artist/Details/artistLibraryView.js'));
const ArtistLibraryViews = load(path.join(root, 'Artist/Details/ArtistLibraryViews.js')).default;
const ArtistSongs = load(path.join(root, 'Artist/Details/ArtistDetailsTracks.js')).default;
const QueueRow = load(path.join(root, 'Activity/Queue/QueueRow.js')).default;
const TrackRow = load(path.join(root, 'Album/Details/TrackRow.js')).default;
const SongPopularityCell = load(path.join(root, 'Album/Details/SongPopularityCell.js')).default;
const { createArtistSongs } = load(path.join(root, 'Artist/Details/ArtistDetailsTracksConnector.js'));
const songs = [
  { id: 1, groupKey: 'one', title: 'Song One', albumTitle: 'Album A', monitored: true, monitorTrackIds: [1, 11] },
  { id: 2, groupKey: 'two', title: 'Song Two', albumTitle: 'Album B', monitored: false, monitorTrackIds: [2, 22] }
];

function panel(save = async () => {}) {
  const component = new ArtistSongs({ items: songs, artistSongMode: true, onSongsSelectionSave: save, onSongsDownload: async () => {} });
  component.setState = (update) => Object.assign(component.state, typeof update === 'function' ? update(component.state) : update);
  return component;
}

function textOf(element) {
  if (element == null || typeof element === 'boolean') return '';
  if (typeof element !== 'object') return String(element);
  if (Array.isArray(element)) return element.map(textOf).join('');
  return textOf(element.props.children);
}

test('checklist starts with saved monitoring, and title/album filtering is case insensitive', () => {
  assert.deepEqual(createSongSelection(songs), { one: true, two: false });
  assert.deepEqual(filterSongs(songs, ' ALBUM b '), [songs[1]]);
  assert.deepEqual(filterSongs(songs, 'song ONE'), [songs[0]]);
});

test('filtering/unmounting a row preserves hidden selected songs and mount is not an edit', () => {
  const component = panel();
  component.onSelectedChange({ id: 'one', value: true });
  assert.equal(component.state.isDirty, false);
  component.onQueryChange({ value: 'Album B' });
  component.onSelectedChange({ id: 'one', value: null });
  assert.equal(component.state.selectedState.one, true);
  component.onSelectAllChange({ value: true });
  assert.deepEqual(component.state.selectedState, { one: true, two: true });
});

test('clear selection clears all songs, including filtered-out rows, without saving', () => {
  const component = panel(() => assert.fail('should not save on clear'));
  component.onQueryChange({ value: 'Album B' });
  component.onClearPress();
  assert.deepEqual(component.state.selectedState, { one: false, two: false });
  assert.equal(component.state.isDirty, true);
});

test('save applies only selected songs and searches one representative per recording', async () => {
  const calls = [];
  await saveSongSelection({
    items: songs, selectedState: { two: true },
    monitor: async (ids, value) => calls.push(['monitor', ids, value]),
    enableArtist: async () => calls.push(['enable']),
    search: async (ids) => calls.push(['search', ids])
  });
  assert.deepEqual(calls, [['monitor', [1, 11], false], ['monitor', [2, 22], true], ['enable'], ['search', [2]]]);
});

test('saving an empty selection stops monitoring without a search or enabling the artist', async () => {
  const calls = [];
  await saveSongSelection({
    items: songs, selectedState: {},
    monitor: async (ids, value) => calls.push([ids, value]),
    enableArtist: () => assert.fail('should not enable artist'),
    search: () => assert.fail('should not search')
  });
  assert.deepEqual(calls, [[[1, 11], false]]);
});

test('monitor save failure never starts a download', async () => {
  await assert.rejects(saveSongSelection({
    items: songs, selectedState: { two: true },
    monitor: async () => { throw new Error('failed'); },
    enableArtist: () => assert.fail('should not enable'),
    search: () => assert.fail('should not search')
  }));
});

test('failed save keeps draft and displays an error for retry', async () => {
  const component = panel(async () => { throw new Error('failed'); });
  component.onSelectedChange({ id: 'two', value: true });
  await component.onSaveSelectionPress();
  assert.equal(component.state.saveError, true);
  assert.equal(component.state.isSaving, false);
  assert.equal(component.state.selectedState.two, true);
});

test('checklist is immediately visible with a direct download action', () => {
  const rendered = panel().render();
  assert.match(textOf(rendered), /Download selected/);
  assert.doesNotMatch(textOf(rendered), /Select Songs/);
});

test('queue shows wanted titles under the album only for selected-song grabs', () => {
  const props = {
    columns: [{ name: 'albums.title', isVisible: true }],
    album: { title: 'Album A' }, customFormats: [], size: 100, sizeleft: 50,
    targetTracks: [{ foreignRecordingId: 'one', title: 'Song One' }, { foreignRecordingId: 'two', title: 'Song Two' }]
  };
  assert.match(textOf(new QueueRow(props).render()), /Wanted songs: Song One, Song Two/);
  assert.doesNotMatch(textOf(new QueueRow({ ...props, targetTracks: [] }).render()), /Wanted songs/);
});

test('popularity sorting uses provider positions and leaves unknown songs last', () => {
  const items = [
    { ...songs[0], popularity: { position: 20 } },
    { ...songs[1], popularity: { position: 1 } },
    { id: 3, title: 'AAA', albumTitle: 'Album C' }
  ];
  assert.deepEqual(filterSongs(items, '').map((song) => song.id), [2, 1, 3]);
  assert.deepEqual(filterSongs(items, '', 'title').map((song) => song.id), [3, 1, 2]);
  assert.deepEqual(filterSongs(items, '', 'album').map((song) => song.id), [1, 2, 3]);
  assert.deepEqual(items.map((song) => song.id), [1, 2, 3]);
});

function rowEvent(overrides = {}) {
  const target = { closest: () => null };
  return { target, currentTarget: { contains: () => true }, button: 0, detail: 1, ...overrides };
}

function selectableRow(props = {}) {
  const calls = [];
  const row = new TrackRow({ id: 1, selectionId: 'recording', isSelectMode: true, isRowSelectable: true, isSelected: false, onSelectedChange: (value) => calls.push(value), ...props });
  return { row, calls };
}

test('clicking a row selects or unselects its recording and forwards shift for range selection', () => {
  const { row, calls } = selectableRow();
  row.onRowClick(rowEvent());
  assert.deepEqual(calls, [{ id: 'recording', value: true, shiftKey: false }]);
  row.props = { ...row.props, isSelected: true };
  row.onRowClick(rowEvent({ shiftKey: true }));
  assert.deepEqual(calls[1], { id: 'recording', value: false, shiftKey: true });
});

test('interactive controls, portal dialogs and text selection do not also toggle the row', () => {
  const { row, calls } = selectableRow();
  for (const tag of ['input', 'button', 'a', 'label']) {
    row.onRowClick(rowEvent({ target: { closest: (selector) => selector.includes(tag) ? {} : null } }));
  }
  row.onRowClick(rowEvent({ currentTarget: { contains: () => false } }));
  global.window = { getSelection: () => ({ toString: () => 'selected text' }) };
  try { row.onRowClick(rowEvent()); } finally { delete global.window; }
  assert.equal(calls.length, 0);
});

test('disabled selection and modified, double, secondary or cancelled clicks are ignored', () => {
  const { row, calls } = selectableRow();
  for (const overrides of [{ button: 2 }, { detail: 2 }, { ctrlKey: true }, { metaKey: true }, { altKey: true }, { defaultPrevented: true }]) {
    row.onRowClick(rowEvent(overrides));
  }
  row.props = { ...row.props, isSelectionDisabled: true };
  row.onRowClick(rowEvent());
  assert.equal(calls.length, 0);
});

test('Space and Enter select a focused row without intercepting control keyboard events', () => {
  const { row, calls } = selectableRow();
  const target = {};
  let prevented = 0;
  for (const key of [' ', 'Enter']) {
    row.onRowKeyDown({ key, target, currentTarget: target, preventDefault: () => prevented++ });
  }
  row.onRowKeyDown({ key: 'Enter', target: {}, currentTarget: target });
  row.props = { ...row.props, isSelectionDisabled: true };
  row.onRowKeyDown({ key: 'Enter', target, currentTarget: target });
  assert.equal(calls.length, 2);
  assert.equal(prevented, 2);
});

test('shift selection follows the displayed popularity order and preserves filtered-out choices', () => {
  const component = panel();
  component.props = { ...component.props, items: [
    { ...songs[0], popularity: { position: 3 } },
    { ...songs[1], popularity: { position: 1 } },
    { id: 3, groupKey: 'three', title: 'Song Three', albumTitle: 'Album A', popularity: { position: 2 } }
  ] };
  component.onSelectedChange({ id: 'two', value: true });
  component.onSelectedChange({ id: 'one', value: false, shiftKey: true });
  assert.deepEqual(component.state.selectedState, { one: false, two: false, three: false });
});

test('popularity shows a named provider position and source link, without inventing a rating for unknown songs', () => {
  const known = SongPopularityCell({ popularity: { position: 2, providerScore: 900000, sourceUrl: 'https://www.deezer.com/track/123' } });
  assert.match(textOf(known), /Top #2Deezer/);
  assert.equal(known.props.children.props.to, 'https://www.deezer.com/track/123');
  const unknown = SongPopularityCell({});
  assert.equal(textOf(unknown), '—');
  assert.match(unknown.props.title, /not a quality rating/i);
});

test('Songs has a direct URL and Albums is the default, independent of artist monitoring', () => {
  assert.equal(getArtistLibraryView(''), 'albums');
  assert.equal(getArtistLibraryView('?view=songs'), 'songs');
  assert.equal(getArtistLibraryView('?view=unexpected'), 'albums');
  assert.equal(getArtistLibraryViewLink('artist-id', 'songs'), '/artist/artist-id?view=songs');
  assert.equal(getArtistLibraryViewLink('artist-id', 'albums'), '/artist/artist-id');
});

function libraryViews(view = 'albums', artistSongMode = false) {
  const component = new ArtistLibraryViews({
    artistId: 1, foreignArtistId: 'artist-id', artistName: 'Artist', artistMonitored: true,
    artistSongMode, view, isPopulated: true, hasTracks: true, isRefreshing: false,
    onRefreshPress: () => {}, children: React.createElement('span', {}, 'Album cards')
  });
  component.setState = (state) => Object.assign(component.state, state);
  return component;
}

function childrenOf(element) { return React.Children.toArray(element.props.children); }

test('Albums and Songs navigation is always visible without changing monitoring', () => {
  for (const artistSongMode of [true, false]) {
    const component = libraryViews('albums', artistSongMode);
    const [navigation, albums, songsPanel] = childrenOf(component.render());
    const links = childrenOf(navigation);
    assert.deepEqual(links.map(textOf), ['Albums', 'Songs']);
    assert.equal(links[0].props['aria-current'], 'page');
    assert.equal(links[1].props.to, '/artist/artist-id?view=songs');
    assert.equal(links[1].props.onPress, undefined);
    assert.equal(albums.props.hidden, false);
    assert.match(textOf(albums), /Album cards/);
    assert.equal(songsPanel, undefined); // Popularity is loaded only after Songs is opened.
  }
});

test('switching views keeps the song panel mounted and album controls available', () => {
  const component = libraryViews('songs', true);
  const [, albums, songPanel] = childrenOf(component.render());
  assert.equal(albums.props.hidden, true);
  assert.match(textOf(albums), /Album cards/);
  assert.equal(songPanel.props.hidden, false);
  component.props = { ...component.props, view: 'albums' };
  const [, visibleAlbums, hiddenSongs] = childrenOf(component.render());
  assert.equal(visibleAlbums.props.hidden, false);
  assert.equal(hiddenSongs.props.hidden, true);
  assert.equal(hiddenSongs.props.children.type, songPanel.props.children.type);
  assert.equal(hiddenSongs.props.children.key, songPanel.props.children.key);
});

test('artists with album monitoring start with an empty song draft without modifying their flags', () => {
  assert.deepEqual(createSongSelection(songs, false), { one: false, two: false });
  assert.equal(songs[0].monitored, true);
  assert.deepEqual(createSongSelection(songs, true), { one: true, two: false });
});

test('one-time song downloads search only checked representatives and do not touch monitoring', async () => {
  const calls = [];
  await downloadSongSelection({ items: songs, selectedState: { two: true }, search: async (ids) => calls.push(ids) });
  assert.deepEqual(calls, [[2]]);
  assert.equal(songs[0].monitored, true);
  assert.equal(songs[1].monitored, false);
  await downloadSongSelection({ items: songs, selectedState: {}, search: () => assert.fail('empty selection must not search') });
});

test('Download selected does not save monitoring and a failed search keeps the draft', async () => {
  const component = panel(() => assert.fail('download must not save monitoring'));
  component.onSelectedChange({ id: 'two', value: true });
  component.props.onSongsDownload = async () => { throw new Error('search failed'); };
  await component.onDownloadSelectedPress();
  assert.equal(component.state.downloadError, true);
  assert.equal(component.state.saveError, false);
  assert.equal(component.state.selectedState.two, true);
  component.props.onSongsDownload = async () => {};
  await component.onDownloadSelectedPress();
  assert.equal(component.state.downloadStarted, true);
  assert.equal(component.state.isDirty, true); // Downloading did not save the monitoring draft.
});

test('Songs works for album-monitored artists without requiring a mode change or showing a monitoring-save action', () => {
  const component = panel();
  component.props = { ...component.props, artistSongMode: false };
  const output = textOf(component.render());
  assert.match(output, /Download selected/);
  assert.match(output, /Album monitoring stays unchanged/);
  assert.doesNotMatch(output, /Save monitored songs/);
});

test('downloaded recordings on another album are grouped once with the owned file as representative', () => {
  const grouped = createArtistSongs([
    { id: 1, albumId: 10, foreignRecordingId: 'recording', title: 'Same song', duration: 1000, hasFile: false, monitored: true },
    { id: 2, albumId: 20, foreignRecordingId: 'recording', title: 'Same song', duration: 1000, trackFileId: 99, monitored: false },
    { id: 3, albumId: 10, foreignRecordingId: 'live-recording', title: 'Same song (live)', duration: 1000, trackFileId: 0 }
  ], [{ id: 10, title: 'Original', albumType: 'Album' }, { id: 20, title: 'Compilation', albumType: 'Album' }]);
  const owned = grouped.find((song) => song.groupKey === 'recording');
  assert.equal(owned.hasFile, true);
  assert.equal(owned.id, 2);
  assert.equal(owned.trackFileId, 99);
  assert.deepEqual(filterSongs(grouped, '', 'title', 'downloaded'), [owned]);
  assert.deepEqual(filterSongs(grouped, '', 'title', 'missing').map((song) => song.groupKey), ['live-recording']);
});

test('availability filters preserve hidden choices and select-all affects only visible missing songs', () => {
  const component = panel();
  component.props = { ...component.props, items: [
    { ...songs[0], hasFile: true }, { ...songs[1], hasFile: false }
  ] };
  component.onLibraryFilterChange({ value: 'missing' });
  component.onSelectAllChange({ value: true });
  assert.deepEqual(component.state.selectedState, { one: true, two: true });
  component.onSelectAllChange({ value: false });
  assert.deepEqual(component.state.selectedState, { one: true, two: false });
  component.onLibraryFilterChange({ value: 'downloaded' });
  component.onSelectAllChange({ value: false });
  assert.deepEqual(component.state.selectedState, { one: false, two: false });
  assert.equal(component.state.lastToggled, null);
});

test('song rows show downloaded/missing text and downloaded shading without disabling upgrade selection', () => {
  const { row, calls } = selectableRow({ trackFileId: 99, columns: [{ name: 'status', isVisible: true }] });
  const rendered = row.render();
  assert.match(textOf(rendered), /Downloaded/);
  assert.match(rendered.props.className, /downloadedRow/);
  row.onRowClick(rowEvent());
  assert.equal(calls[0].value, true);
  row.props = { ...row.props, trackFileId: 0, hasFile: false };
  assert.match(textOf(row.render()), /Missing/);
  assert.doesNotMatch(row.render().props.className, /downloadedRow/);
  row.props = { ...row.props, isRowSelectable: false };
  assert.doesNotMatch(textOf(row.render()), /Downloaded|Missing/);
});

test('downloaded selection keeps its downloaded appearance and shows a separate selected state', () => {
  const { row } = selectableRow({ trackFileId: 99, isSelected: true, columns: [] });
  assert.match(row.render().props.className, /downloadedRow/);
  assert.match(row.render().props.className, /selectedRow/);
  assert.equal(row.render().props['aria-selected'], true);
});

test('song counts describe library availability and rows update when an imported file arrives', () => {
  const component = panel();
  component.props = { ...component.props, items: [{ ...songs[0], trackFileId: 99 }, { ...songs[1], hasFile: false }] };
  assert.match(textOf(component.render()), /1 downloaded/);
  assert.match(textOf(component.render()), /1 missing/);
  const { row } = selectableRow({ trackFileId: 0, columns: [{ name: 'status', isVisible: true }] });
  assert.match(textOf(row.render()), /Missing/);
  row.props = { ...row.props, trackFileId: 99 };
  assert.match(textOf(row.render()), /Downloaded/);
});
