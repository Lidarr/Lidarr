import ModelBase from 'App/ModelBase';

export type WriteAudioTagsType = 'no' | 'newFiles' | 'allFiles' | 'sync';

interface TaggingProfile extends ModelBase {
  name: string;
  writeAudioTags: WriteAudioTagsType;
  scrubAudioTags: boolean;
  embedCoverArt: boolean;
  order: number;
  tags: number[];
  skipHardlinkedFiles: boolean;
}

export default TaggingProfile;
