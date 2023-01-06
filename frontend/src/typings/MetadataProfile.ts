interface PrimaryAlbumType {
  id?: number;
  name?: string;
}

interface SecondaryAlbumType {
  id?: number;
  name?: string;
}

interface ReleaseStatus {
  id?: number;
  name?: string;
}

interface ProfilePrimaryAlbumTypeItem {
  primaryAlbumType?: PrimaryAlbumType;
  allowed: boolean;
}

interface ProfileSecondaryAlbumTypeItem {
  secondaryAlbumType?: SecondaryAlbumType;
  allowed: boolean;
}

interface ProfileReleaseStatusItem {
  releaseStatus?: ReleaseStatus;
  allowed: boolean;
}

interface MetadataProfile {
  name: string;
  primaryAlbumTypes: ProfilePrimaryAlbumTypeItem[];
  secondaryAlbumTypes: ProfileSecondaryAlbumTypeItem[];
  ReleaseStatuses: ProfileReleaseStatusItem[];
  id: number;
}

export default MetadataProfile;
