import migrateAddArtistDefaults from './migrateAddArtistDefaults';
import migrateBlacklistToBlocklist from './migrateBlacklistToBlocklist';

export default function migrate(persistedState) {
  migrateAddArtistDefaults(persistedState);
  migrateBlacklistToBlocklist(persistedState);
}
