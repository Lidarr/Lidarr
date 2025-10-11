export default function getPathWithUrlBase(path: string) {
  return `${window.Lidarr.urlBase}${path}`;
}
