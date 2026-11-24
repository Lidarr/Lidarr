type Measure = (text: string) => number;

// Guarded: this module loads with the app, so a throw here would blank every page on a browser without it
const graphemes =
  typeof Intl.Segmenter === 'function'
    ? new Intl.Segmenter(undefined, { granularity: 'grapheme' })
    : null;

// Offsets text can be cut at, starting with 0: between whole characters, or code points without Intl.Segmenter
function characterStarts(text: string) {
  if (graphemes) {
    return Array.from(graphemes.segment(text), (segment) => segment.index);
  }

  const starts: number[] = [];
  let offset = 0;

  for (const character of text) {
    starts.push(offset);
    offset += character.length;
  }

  return starts;
}

function cutLine(text: string, maxWidth: number, measure: Measure) {
  if (measure(text) <= maxWidth) {
    return text;
  }

  const starts = characterStarts(text);
  let i = starts.length - 1;

  while (
    i > 0 &&
    measure(`${text.slice(0, starts[i]).trimEnd()}…`) > maxWidth
  ) {
    i--;
  }

  // The isolate puts the ellipsis at the end a right-to-left reader reaches last
  return `\u2068${text.slice(0, starts[i]).trimEnd()}…\u2069`;
}

// The longest start of text that fits, ended at a space when one falls inside it
function lineEnd(text: string, maxWidth: number, measure: Measure) {
  const ends = [...characterStarts(text).slice(1), text.length];
  let i = ends.length - 1;

  while (i > 0 && measure(text.slice(0, ends[i])) > maxWidth) {
    i--;
  }

  const space = text.lastIndexOf(' ', ends[i]);

  return space > 0 ? space : ends[i];
}

export default function fitLabel(
  label: string,
  maxWidth: number,
  measure: Measure,
  maxLines = 1
) {
  const lines: string[] = [];
  let rest = label;

  while (lines.length < maxLines - 1 && measure(rest) > maxWidth) {
    const end = lineEnd(rest, maxWidth, measure);

    lines.push(rest.slice(0, end).trimEnd());
    rest = rest.slice(end).trimStart();
  }

  lines.push(cutLine(rest, maxWidth, measure));

  return lines;
}
