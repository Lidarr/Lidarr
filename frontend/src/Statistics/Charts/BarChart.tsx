import {
  BarElement,
  CategoryScale,
  Chart,
  ChartOptions,
  LinearScale,
  Scale,
  Tick,
  Tooltip,
  TooltipItem,
} from 'chart.js';
import { toFont } from 'chart.js/helpers';
import React, { useMemo } from 'react';
import { Bar } from 'react-chartjs-2';
import useChartColors from 'Statistics/Charts/useChartColors';
import ChartContainer, { getChartAriaLabel } from './ChartContainer';
import fitLabel from './fitLabel';

Chart.register(BarElement, CategoryScale, LinearScale, Tooltip);

export interface BarChartItem {
  label: string;
  value: number;
  tooltipLines?: string[];
}

interface BarChartProps {
  title: string;
  items: BarChartItem[];
  formatValue?: (value: number) => string;
  stepSize?: number;
}

// Chart.js's defaults, set on the label axis because fitAxisLabel counts on them
const TICK_LENGTH = 8;
const TICK_PADDING = 3;
const AUTO_SKIP_PADDING = 3;

// Chart.js tilts labels once the widest comes this close to its column's width
const FLAT_LABEL_GAP = 6;

// Fit to the roomier of lying flat in its column or standing up in the axis; the tooltip keeps the full name
function fitAxisLabel(
  this: Scale,
  value: number | string,
  _index: number,
  ticks: Tick[]
) {
  const { ctx } = this;
  const font = toFont(Chart.defaults.font);
  const column = this.maxWidth / ticks.length;
  const flatRoom = column - FLAT_LABEL_GAP;
  const uprightRoom = this.maxHeight - TICK_LENGTH - TICK_PADDING * 2;

  // Upright labels make Chart.js narrow the axis by up to their thickness and padding, and two lines need the skip gap left in each column
  const twoLines = 2 * font.lineHeight;
  const drawnColumn = (this.maxWidth - twoLines - TICK_PADDING) / ticks.length;
  const maxLines = drawnColumn >= twoLines + AUTO_SKIP_PADDING ? 2 : 1;

  ctx.save();
  ctx.font = font.string;

  const lines = fitLabel(
    this.getLabelForValue(Number(value)),
    Math.max(flatRoom, uprightRoom),
    (text) => ctx.measureText(text).width,
    maxLines
  );

  ctx.restore();

  return lines;
}

// Chart.js tilts labels just enough and pads for the first only, so a long later label can overhang the left edge
function standLabelsUpright(axis: Scale) {
  if (axis.labelRotation > 0) {
    axis.labelRotation = 90;
  }
}

function BarChart({ title, items, formatValue, stepSize }: BarChartProps) {
  const colors = useChartColors();

  const data = useMemo(() => {
    return {
      labels: items.map((item) => item.label),
      datasets: [
        {
          data: items.map((item) => item.value),
          backgroundColor: colors.bar,
          borderRadius: 3,
          maxBarThickness: 50,
        },
      ],
    };
  }, [colors, items]);

  const options = useMemo<ChartOptions<'bar'>>(() => {
    return {
      responsive: true,
      maintainAspectRatio: false,
      scales: {
        x: {
          afterCalculateLabelRotation: standLabelsUpright,
          ticks: {
            color: colors.text,
            padding: TICK_PADDING,
            autoSkipPadding: AUTO_SKIP_PADDING,
            callback: fitAxisLabel,
          },
          grid: {
            display: false,
            tickLength: TICK_LENGTH,
          },
        },
        y: {
          beginAtZero: true,
          ticks: {
            precision: 0,
            color: colors.text,
            stepSize,
            ...(formatValue
              ? {
                  callback: (value: number | string) =>
                    formatValue(Number(value)),
                }
              : {}),
          },
          grid: {
            color: colors.grid,
          },
        },
      },
      plugins: {
        legend: {
          display: false,
        },
        tooltip: {
          callbacks: {
            label: (context: TooltipItem<'bar'>) => {
              const value = context.parsed.y ?? 0;

              return ` ${
                formatValue ? formatValue(value) : value.toLocaleString()
              }`;
            },
            afterBody: (contexts: TooltipItem<'bar'>[]) => {
              const index = contexts[0]?.dataIndex ?? 0;

              return items[index]?.tooltipLines ?? [];
            },
          },
        },
      },
    };
  }, [colors, formatValue, items, stepSize]);

  return (
    <ChartContainer title={title}>
      <Bar
        data={data}
        options={options}
        aria-label={getChartAriaLabel(title, items, formatValue)}
      />
    </ChartContainer>
  );
}

export default BarChart;
