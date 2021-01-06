import * as sentry from '@sentry/browser';
import * as Integrations from '@sentry/integrations';
import _ from 'lodash';
import parseUrl from 'Utilities/String/parseUrl';

const IgnoreErrors = [
  // Innocuous browser errors
  /ResizeObserver loop limit exceeded/,
  /ResizeObserver loop completed with undelivered notifications/
];

function cleanseUrl(url) {
  const properties = parseUrl(url);

  return `${properties.pathname}${properties.search}`;
}

function shouldIgnoreException(s) {
  return s && IgnoreErrors.find((pattern) => pattern.test(s));
}

function cleanseData(event, hint) {
  const result = _.cloneDeep(event);

  const error = hint && hint.originalException;

  result.transaction = cleanseUrl(result.transaction);

  if (result.exception) {
    result.exception.values.forEach((exception) => {
      const stacktrace = exception.stacktrace;

      if (stacktrace) {
        stacktrace.frames.forEach((frame) => {
          frame.filename = cleanseUrl(frame.filename);
        });
      }
    });
  }

  if (
    error &&
    error.message &&
    shouldIgnoreException(error.message)
  ) {
    return null;
  }

  result.request.url = cleanseUrl(result.request.url);

  return result;
}

function identity(stuff) {
  return stuff;
}

function stripUrlBase(frame) {
  if (frame.filename && window.Lidarr.urlBase) {
    frame.filename = frame.filename.replace(window.Lidarr.urlBase, '');
  }
  return frame;
}

function createMiddleware() {
  return (store) => (next) => (action) => {
    try {
      // Adds a breadcrumb for reporting later (if necessary).
      sentry.addBreadcrumb({
        category: 'redux',
        message: action.type
      });

      return next(action);
    } catch (err) {
      console.error(`[sentry] Reporting error to Sentry: ${err}`);

      // Send the report including breadcrumbs.
      sentry.captureException(err, {
        extra: {
          action: identity(action),
          state: identity(store.getState())
        }
      });
    }
  };
}

export default function createSentryMiddleware() {
  const {
    analytics,
    branch,
    version,
    release,
    userHash,
    isProduction
  } = window.Lidarr;

  if (!analytics) {
    return;
  }

  const dsn = isProduction ? 'https://9df0cbe9eedf4b8698e8bc94ec03c4ff@sentry.servarr.com/18' :
    'https://aaa261a8c3e542f0a52c07bed78e8d13@sentry.servarr.com/19';

  sentry.init({
    dsn,
    environment: branch,
    release,
    sendDefaultPii: true,
    beforeSend: cleanseData,
    integrations: [
      new Integrations.RewriteFrames({ iteratee: stripUrlBase }),
      new Integrations.Dedupe()
    ]
  });

  sentry.configureScope((scope) => {
    scope.setUser({ username: userHash });
    scope.setTag('version', version);
    scope.setTag('production', isProduction);
  });

  return createMiddleware();
}
