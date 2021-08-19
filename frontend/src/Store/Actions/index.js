import * as albums from './albumActions';
import * as albumHistory from './albumHistoryActions';
import * as albumStudio from './albumStudioActions';
import * as app from './appActions';
import * as artist from './artistActions';
import * as artistEditor from './artistEditorActions';
import * as artistHistory from './artistHistoryActions';
import * as artistIndex from './artistIndexActions';
import * as blocklist from './blocklistActions';
import * as calendar from './calendarActions';
import * as captcha from './captchaActions';
import * as commands from './commandActions';
import * as customFilters from './customFilterActions';
import * as history from './historyActions';
import * as interactiveImportActions from './interactiveImportActions';
import * as oAuth from './oAuthActions';
import * as organizePreview from './organizePreviewActions';
import * as paths from './pathActions';
import * as providerOptions from './providerOptionActions';
import * as queue from './queueActions';
import * as releases from './releaseActions';
import * as retagPreview from './retagPreviewActions';
import * as search from './searchActions';
import * as settings from './settingsActions';
import * as system from './systemActions';
import * as tags from './tagActions';
import * as tracks from './trackActions';
import * as trackFiles from './trackFileActions';
import * as wanted from './wantedActions';

export default [
  app,
  blocklist,
  captcha,
  calendar,
  commands,
  customFilters,
  albums,
  trackFiles,
  albumHistory,
  history,
  interactiveImportActions,
  oAuth,
  organizePreview,
  retagPreview,
  paths,
  providerOptions,
  queue,
  releases,
  albumStudio,
  artist,
  artistEditor,
  artistHistory,
  artistIndex,
  search,
  settings,
  system,
  tags,
  tracks,
  wanted
];
