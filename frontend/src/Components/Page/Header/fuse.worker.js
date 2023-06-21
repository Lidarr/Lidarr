import Fuse from 'fuse.js';

const fuseOptions = {
  shouldSort: true,
  includeMatches: true,
  ignoreLocation: true,
  threshold: 0.3,
  maxPatternLength: 32,
  minMatchCharLength: 1,
  keys: [
    'artistName',
    'foreignArtistId',
    'tags.label'
  ]
};

function getSuggestions(artists, value) {
  const limit = 10;
  let suggestions = [];

  if (value.length === 1) {
    for (let i = 0; i < artists.length; i++) {
      const s = artists[i];
      if (s.firstCharacter === value.toLowerCase()) {
        suggestions.push({
          item: artists[i],
          indices: [
            [0, 0]
          ],
          matches: [
            {
              value: s.title,
              key: 'title'
            }
          ],
          arrayIndex: 0
        });
        if (suggestions.length > limit) {
          break;
        }
      }
    }
  } else {
    const fuse = new Fuse(artists, fuseOptions);
    suggestions = fuse.search(value, { limit });
  }

  return suggestions;
}

onmessage = function(e) {
  if (!e) {
    return;
  }

  const {
    artists,
    value
  } = e.data;

  const suggestions = getSuggestions(artists, value);

  const results = {
    value,
    suggestions
  };

  self.postMessage(results);
};
