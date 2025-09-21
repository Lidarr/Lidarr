import PropTypes from 'prop-types';
import React, { useCallback } from 'react';
import FormInputGroup from 'Components/Form/FormInputGroup';
import { inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

function SearchAliasInput({ aliases, artistName, value, onChange, onInputChange }) {
  const onSearchAliasChange = useCallback(
    ({ value: newValue }) => {
      onInputChange({
        name: 'searchAlias',
        value: newValue === artistName ? null : newValue
      });
    },
    [onInputChange, artistName]
  );

  if (!aliases || aliases.length === 0) {
    return null;
  }

  // Create options: deduplicate aliases and filter out empty ones
  const uniqueAliases = [...new Set(aliases.filter((alias) => alias && alias.trim().length > 0))];
  const searchOptions = uniqueAliases.map((alias) => ({ key: alias, value: alias, text: alias }));

  // If no aliases available after filtering, don't show the control
  if (searchOptions.length === 0) {
    return null;
  }

  const displayValue = value || artistName;

  return (
    <FormInputGroup
      type={inputTypes.SELECT}
      name="searchAlias"
      value={displayValue}
      values={searchOptions}
      helpText={translate('SearchAliasHelpText')}
      onChange={onSearchAliasChange}
    />
  );
}

SearchAliasInput.propTypes = {
  aliases: PropTypes.arrayOf(PropTypes.string),
  artistName: PropTypes.string.isRequired,
  value: PropTypes.string,
  onChange: PropTypes.func,
  onInputChange: PropTypes.func.isRequired
};

export default SearchAliasInput;
