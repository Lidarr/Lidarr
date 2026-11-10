import { WriteAudioTagsType } from 'typings/TaggingProfile';
import translate from 'Utilities/String/translate';

interface WriteAudioTagsOption {
  key: WriteAudioTagsType;
  value: string;
}

const writeAudioTagOptions: WriteAudioTagsOption[] = [
  {
    key: 'sync',
    get value() {
      return translate('WriteAudioTagsSync');
    },
  },
  {
    key: 'allFiles',
    get value() {
      return translate('WriteAudioTagsAllFiles');
    },
  },
  {
    key: 'newFiles',
    get value() {
      return translate('WriteAudioTagsNewFiles');
    },
  },
  {
    key: 'no',
    get value() {
      return translate('Never');
    },
  },
];

export default writeAudioTagOptions;
