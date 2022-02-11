const UNITS = ['KiB', 'MiB', 'GiB', 'TiB', 'PiB', 'EiB', 'ZiB', 'YiB'];

export function humanFileSize(bytes: number) {
	if (Math.abs(bytes) < 1024) {
		return `${bytes} B`;
	}

	let u = -1;
	do {
		bytes /= 1024;
		++u;
	} while (Math.abs(bytes) >= 1024 && u < UNITS.length - 1);
	return `${bytes.toFixed(1)} ${UNITS[u]}`;
}
