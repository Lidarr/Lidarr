// Whole units only past a half: a tenth of a GiB reads as 102.4 MiB
const UNIT_STEPS = [0.5, 1, 2, 5, 10, 20, 50, 100, 200];
const TARGET_TICKS = 5;

// Chart.js picks steps that are round in tens; byte ticks need steps round in KiB, MiB or GiB
export default function getByteStepSize(maxBytes: number) {
  if (!(maxBytes > 0)) {
    return undefined;
  }

  let unit = Math.pow(1024, Math.floor(Math.log2(maxBytes) / 10));

  // Past 1000 of a unit, step in the next one: a top tick at 1500 MiB would read as 1.5 GiB
  if (maxBytes / unit > 1000) {
    unit *= 1024;
  }

  const unitStep = UNIT_STEPS.find(
    (step) => step * TARGET_TICKS >= maxBytes / unit
  );

  return unitStep === undefined ? undefined : unitStep * unit;
}
