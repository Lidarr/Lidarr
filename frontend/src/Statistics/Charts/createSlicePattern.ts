const TILE_SIZE = 12;
const STRIPE_SPACING = 6;

type Segment = [number, number, number, number];

const OVERHANG = 3;
const FAR = TILE_SIZE + OVERHANG;

// The lines through the tile corners are drawn too, or the stripes break where tiles meet
const RISING: Segment[] = [
  [-OVERHANG, OVERHANG, OVERHANG, -OVERHANG],
  [0, STRIPE_SPACING, STRIPE_SPACING, 0],
  [0, TILE_SIZE, TILE_SIZE, 0],
  [STRIPE_SPACING, TILE_SIZE, TILE_SIZE, STRIPE_SPACING],
  [TILE_SIZE - OVERHANG, FAR, FAR, TILE_SIZE - OVERHANG],
];

const FALLING: Segment[] = [
  [TILE_SIZE - OVERHANG, -OVERHANG, FAR, OVERHANG],
  [STRIPE_SPACING, 0, TILE_SIZE, STRIPE_SPACING],
  [0, 0, TILE_SIZE, TILE_SIZE],
  [0, STRIPE_SPACING, STRIPE_SPACING, TILE_SIZE],
  [-OVERHANG, TILE_SIZE - OVERHANG, OVERHANG, FAR],
];

const HORIZONTAL: Segment[] = [
  [0, 3, TILE_SIZE, 3],
  [0, 9, TILE_SIZE, 9],
];

const VERTICAL: Segment[] = [
  [3, 0, 3, TILE_SIZE],
  [9, 0, 9, TILE_SIZE],
];

const SPARSE_RISING: Segment[] = [RISING[0], RISING[2], RISING[4]];

// One variant per palette colour (useChartColors): an eighth slice repeats the first's pattern
const STRIPE_SEGMENTS: Segment[][] = [
  RISING,
  FALLING,
  HORIZONTAL,
  VERTICAL,
  [...RISING, ...FALLING],
  [...HORIZONTAL, ...VERTICAL],
  SPARSE_RISING,
];

const createSlicePattern = (
  color: string,
  stripeColor: string,
  variant: number
): CanvasPattern | string => {
  const canvas = document.createElement('canvas');

  canvas.width = TILE_SIZE;
  canvas.height = TILE_SIZE;

  const context = canvas.getContext('2d');

  if (!context) {
    return color;
  }

  context.fillStyle = color;
  context.fillRect(0, 0, TILE_SIZE, TILE_SIZE);

  context.strokeStyle = stripeColor;
  context.globalAlpha = 0.5;
  context.lineWidth = 3;

  STRIPE_SEGMENTS[variant % STRIPE_SEGMENTS.length].forEach(
    ([x1, y1, x2, y2]) => {
      context.beginPath();
      context.moveTo(x1, y1);
      context.lineTo(x2, y2);
      context.stroke();
    }
  );

  return context.createPattern(canvas, 'repeat') ?? color;
};

export default createSlicePattern;
