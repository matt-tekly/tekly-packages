import { ITableHeading, ICellData } from "~app/components/tables/Table";

export function canBeTable(obj: any) {
    if (!isObject(obj)) {
        return false;
    }

    const keys = Object.keys(obj);

    if (keys.length !== 1) {
        return false;
    }

    const first = obj[keys[0]];

    if (!isArray(first)) {
        return false;
    }

    const firstArray = first as Array<any>;

    if (firstArray.length === 0) {
        return false;
    }

    const hasAnyObject = Object.values(firstArray[0]).some(isObject);

    if (hasAnyObject) {
        return false;
    }

    return true;
}

export function toTable(obj: any) {
    const rootKeys = Object.keys(obj);
    const array = obj[rootKeys[0]] as Array<any>;

    let headings: ITableHeading[] = [];

    if (isObject(array[0])) {
        headings = Object.keys(array[0]).map<ITableHeading>(x =>({text: x, sortable: true}));
    } else {
        headings = [{text: rootKeys[0]}];
    }
   
    const values = array.map(toCellDatas);

    return {
        headings,
        values
    }
}

function toCellDatas(obj: any): ICellData[] {
    if (!isObject(obj)) {
        return [toCellData(obj)]
    }
    return Object.values(obj).map<ICellData>(toCellData);
}

function toCellData(obj: any): ICellData {
    if (typeof obj === 'number') {
        return { text: obj.toString(), sortValue: obj };
    }

    return {text: obj.toString(), sortValue: obj.toString()}
}

function isObject(val: any) {
    return val !== null && typeof val === 'object';
}

function isArray(val: any) {
    return Array.isArray(val);
}