export async function scanFiles(items: DataTransferItemList): Promise<any[]> {
    let result: any[] = [];

    let promises: Promise<void>[] = []
    for (var i = 0; i < items.length; i++) {
        const item = items[i].webkitGetAsEntry();
        if (item) {
            if (item.isDirectory) {
                promises.push(readAllEntries(item.createReader(), result));
            } else {
                result.push(item);
            }
        }
    }

    await Promise.all(promises);

    return result;
}

async function readAllEntries(directoryReader: any, allFiles: any[]): Promise<void> {
    let allItemsInDirectory: any[] = [];

    let results = await readSomeEntries(directoryReader);
    while (results.length) {
        allItemsInDirectory = allItemsInDirectory.concat(results);
        results = await readSomeEntries(directoryReader);
    }

    for (var i = 0; i < allItemsInDirectory.length; i++) {
        const item = allItemsInDirectory[i];

        if (item.isDirectory) {
            await readAllEntries(item.createReader(), allFiles);
        } else {
            allFiles.push(item);
        }
    }
}

function readSomeEntries(directoryReader: any): Promise<any[]> {
    return new Promise<any[]>((resolve, reject) => {
        directoryReader.readEntries((results: any[]) => {
            resolve(results);
        }, reject);
    });
}