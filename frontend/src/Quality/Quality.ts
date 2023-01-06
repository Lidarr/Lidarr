export interface Revision {
  version: number;
  real: number;
  isRepack: boolean;
}

interface Quality {
  id: number;
  name: string;
}

export interface QualityModel {
  quality: Quality;
  revision: Revision;
}

export default Quality;
