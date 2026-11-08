import React, { useCallback, useMemo } from 'react';
import translate from 'Utilities/String/translate';
import EnhancedSelectInput from './EnhancedSelectInput';

export type AlbumReleaseCountKey = 'trackCount' | 'mediumCount';

interface AlbumReleaseCountRelease {
  trackCount: number;
  mediumCount: number;
}

interface AlbumReleaseCountSelectInputProps {
  name: string;
  value: number[];
  countKey: AlbumReleaseCountKey;
  albumReleases: { value: AlbumReleaseCountRelease[] };
  onChange(payload: { name: string; value: number[] }): void;
}

// Multi-select over the distinct track counts (or audio medium counts) present in the
// album's releases. Options are built from the metadata already stored for the album, so
// only values that exist in MusicBrainz for this album are offered. An empty selection
// means "any".
function AlbumReleaseCountSelectInput(
  props: AlbumReleaseCountSelectInputProps
) {
  const { value, countKey, albumReleases, onChange, ...otherProps } = props;

  const releases = useMemo(() => albumReleases?.value ?? [], [albumReleases]);
  const selected = useMemo(() => value ?? [], [value]);

  const values = useMemo(() => {
    const counts = new Map<number, number>();

    releases.forEach((release) => {
      const count = release[countKey];
      counts.set(count, (counts.get(count) ?? 0) + 1);
    });

    // Keep any selected value that no longer exists so it stays visible and removable.
    selected.forEach((count) => {
      if (!counts.has(count)) {
        counts.set(count, 0);
      }
    });

    return Array.from(counts.entries())
      .sort(([a], [b]) => a - b)
      .map(([count, releaseCount]) => ({
        key: count,
        value:
          countKey === 'trackCount'
            ? translate('CountTracks', { count })
            : translate('CountMedia', { count }),
        hint: translate('CountReleases', { count: releaseCount }),
      }));
  }, [releases, selected, countKey]);

  const onChangeWrapper = useCallback(
    ({ name, value: newValue }: { name: string; value: number[] }) => {
      onChange({ name, value: [...newValue].sort((a, b) => a - b) });
    },
    [onChange]
  );

  return (
    <EnhancedSelectInput
      {...otherProps}
      value={selected}
      values={values}
      onChange={onChangeWrapper}
    />
  );
}

export default AlbumReleaseCountSelectInput;
