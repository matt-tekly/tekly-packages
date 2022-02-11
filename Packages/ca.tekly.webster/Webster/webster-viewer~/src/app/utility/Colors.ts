export function getDivergentColor(index: number) {
	// return divergentColors[getSteppedIndex(index, 8, divergentColors.length)];
	return divergentColors[index];
}

function getSteppedIndex(index: number, step: number, count: number) {
	const offset = Math.floor((index * step) / count);
	return (index * step + offset) % count;
}

export const divergentColors = ['#425ca4',
	'#9fc047',
	'#d15c85',
	'#509933',
	'#cd435c',
	'#44cc7c',
	'#dc985f',
	'#7b7fec',
	'#4bb77e',
	'#c87027',
	'#36d0c3',
	'#d04747',
	'#1fe1fb',
	'#c85435',
	'#4b9dde',
	'#7cbf76',
	'#7d63b5',
	'#9c9837',
	'#5b56bc',
	'#3a7b3a',
	'#5888e0',
	'#d5a550',
	'#9192e1',
	'#517924',
	'#c3586b',
	'#85ca6c',
	'#c36453',
	'#47c19a',
	'#caa331',
	'#766b24',
	'#b3b465',
	'#985e25'
];
