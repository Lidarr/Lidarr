export function canSelectTrackRow(event) {
  if (event.defaultPrevented || event.button > 0 || event.detail > 1 || event.ctrlKey || event.metaKey || event.altKey) {
    return false;
  }
  // React portal events can bubble through a row even when the modal is not
  // inside that row in the DOM.
  if (event.currentTarget && !event.currentTarget.contains(event.target)) {
    return false;
  }
  return !event.target.closest('a, button, input, select, textarea, label, [role="button"], [role="checkbox"], [role="link"], [data-row-selection-ignore]');
}
