import roundNumber from 'Utilities/Number/roundNumber';

// Rounding alone can show 100% while some are missing: a part between 0 and whole never shows 0% or 100%
export default function formatPercent(
  part: number,
  whole: number,
  decimalPlaces = 0
) {
  if (!whole) {
    return '0%';
  }

  const step = Math.pow(10, -decimalPlaces);
  const percentage = roundNumber((part / whole) * 100, decimalPlaces);

  if (percentage === 0 && part > 0) {
    return `<${step}%`;
  }

  if (percentage === 100 && part < whole) {
    return `>${roundNumber(100 - step, decimalPlaces)}%`;
  }

  return `${percentage}%`;
}
