import PropTypes from 'prop-types';
import React from 'react';
import FieldSet from 'Components/FieldSet';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import IconButton from 'Components/Link/IconButton';
import { icons, inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './ScheduledTasksSettings.css';

const units = [
  { key: 'minutes', value: 'minutes', minutes: 1 },
  { key: 'hours', value: 'hours', minutes: 60 },
  { key: 'days', value: 'days', minutes: 1440 }
];

function getUnit(interval) {
  if (interval % 1440 === 0) {
    return 'days';
  }

  if (interval % 60 === 0) {
    return 'hours';
  }

  return 'minutes';
}

function getValue(interval, unit) {
  const unitConfig = units.find((item) => item.key === unit);
  if (!unitConfig) {
    return Math.max(1, Math.round(interval));
  }
  return Math.max(1, Math.round(interval / unitConfig.minutes));
}

function getInterval(value, unit) {
  const unitConfig = units.find((item) => item.key === unit);
  if (!unitConfig) {
    return value;
  }
  return value * unitConfig.minutes;
}

function ScheduledTasksSettings(props) {
  const {
    advancedSettings,
    isPopulated,
    items,
    pendingChanges,
    isSaving,
    setTaskInterval,
    setTaskPending
  } = props;

  const [drafts, setDrafts] = React.useState({});

  if (!advancedSettings || !isPopulated) {
    return null;
  }

  const getEffectiveInterval = (item) => {
    const change = pendingChanges[item.id];

    if (change?.reset) {
      return item.defaultInterval;
    }

    return change?.interval ?? item.interval;
  };

  const getDraft = (item) => {
    const draft = drafts[item.id];

    if (draft) {
      return draft;
    }

    const interval = getEffectiveInterval(item);
    const unit = getUnit(interval);

    return {
      unit,
      value: getValue(interval, unit)
    };
  };

  const onValueChange = (item, value) => {
    const draft = getDraft(item);

    setDrafts((prev) => ({
      ...prev,
      [item.id]: {
        ...draft,
        value
      }
    }));

    if (value > 0) {
      setTaskInterval({ id: item.id, interval: getInterval(value, draft.unit) });
    }
  };

  const onUnitChange = (item, unit) => {
    const draft = getDraft(item);
    const interval = getInterval(draft.value, draft.unit);
    const value = getValue(interval, unit);

    setDrafts((prev) => ({
      ...prev,
      [item.id]: { value, unit }
    }));

    setTaskInterval({ id: item.id, interval: getInterval(value, unit) });
  };

  const onReset = (item) => {
    setDrafts((prev) => {
      const next = { ...prev };
      delete next[item.id];
      return next;
    });
    setTaskPending({ id: item.id, change: { reset: true } });
  };

  return (
    <FieldSet legend={translate('Scheduled')}>
      <div className={styles.tasksContainer}>
        {
          items.map((item) => {
            const { unit, value } = getDraft(item);

            return (
              <FormGroup
                className={styles.task}
                key={item.id}
                advancedSettings={advancedSettings}
                isAdvanced={true}
              >
                <FormLabel>
                  {item.name}
                </FormLabel>

                <div className={styles.taskInputs}>
                  <FormInputGroup
                    type={inputTypes.NUMBER}
                    name={`task-${item.id}`}
                    min={1}
                    value={value}
                    onChange={(input) => onValueChange(item, input.value)}
                  />
                </div>

                <div className={styles.taskUnit}>
                  <FormInputGroup
                    type={inputTypes.SELECT}
                    name={`task-unit-${item.id}`}
                    values={units}
                    value={unit}
                    onChange={(input) => onUnitChange(item, input.value)}
                  />
                </div>

                <div className={styles.taskAction}>
                  <IconButton
                    name={icons.RESTORE}
                    title={translate('Reset')}
                    isDisabled={isSaving}
                    onPress={() => onReset(item)}
                  />
                </div>
              </FormGroup>
            );
          })
        }
      </div>
    </FieldSet>
  );
}

ScheduledTasksSettings.propTypes = {
  advancedSettings: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  isSaving: PropTypes.bool.isRequired,
  items: PropTypes.array.isRequired,
  pendingChanges: PropTypes.object.isRequired,
  setTaskInterval: PropTypes.func.isRequired,
  setTaskPending: PropTypes.func.isRequired
};

export default ScheduledTasksSettings;
