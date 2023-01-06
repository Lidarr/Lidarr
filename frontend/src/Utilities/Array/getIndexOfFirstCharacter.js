export default function getIndexOfFirstCharacter(items, character) {
  return items.findIndex((item) => {
    const firstCharacter = item.sortName.charAt(0);

    if (character === '#') {
      return !isNaN(firstCharacter);
    }

    return firstCharacter === character;
  });
}
