import { color } from 'chart.js/helpers';
import { useMemo } from 'react';
import useTheme from 'Helpers/Hooks/useTheme';
import themes from 'Styles/Themes';

export interface ChartColors {
  text: string;
  grid: string;
  bar: string;
  palette: string[];
  primaryColor: string;
  successColor: string;
  dangerColor: string;
  warningColor: string;
  grayColor: string;
  colorImpairedStripe: string;
}

const useChartColors = (): ChartColors => {
  const theme = useTheme();

  return useMemo(() => {
    const variables = themes[theme as keyof typeof themes];

    return {
      text: variables.textColor,
      grid: color(variables.darkGray).alpha(0.25).rgbString(),
      bar: variables.themeBlue,

      // successColor is left out: next to Lidarr's green it reads as the same slice
      palette: [
        variables.themeBlue,
        variables.primaryColor,
        variables.warningColor,
        variables.dangerColor,
        variables.purple,
        variables.pink,
        variables.gray,
      ],
      primaryColor: variables.primaryColor,
      successColor: variables.successColor,
      dangerColor: variables.dangerColor,
      warningColor: variables.warningColor,
      grayColor: variables.gray,
      colorImpairedStripe: variables.colorImpairedGradient,
    };
  }, [theme]);
};

export default useChartColors;
