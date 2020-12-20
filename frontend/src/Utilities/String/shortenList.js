export default function shortenList(input, startCount = 3, endCount = 1, separator = ', ') {
  const sorted = [...input].sort();
  if (sorted.length <= startCount + endCount) {
    return sorted.join(separator);
  }
  return [...sorted.slice(0, startCount), '...', sorted.slice(-endCount)].join(separator);
}
