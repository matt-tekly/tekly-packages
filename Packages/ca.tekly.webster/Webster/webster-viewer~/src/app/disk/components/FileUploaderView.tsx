import React = require("react");
import { Dialog } from "@blueprintjs/core";
import { scanFiles } from "~app/utility/FileUtils";


interface IFileUploaderViewProps {

}

interface IFileUploaderViewState {
    isOpen: boolean;
}

export class FileUploaderView extends React.Component<IFileUploaderViewProps, IFileUploaderViewState> {

    private static __singleton: FileUploaderView;

    constructor(props: IFileUploaderViewProps) {
        super(props);
        FileUploaderView.__singleton = this;

        this.state = {
            isOpen: false
        };
    }

    public render() {
        return (
            <Dialog icon='info-sign' onClose={this.handleClose} title='Upload Files' isOpen={this.state.isOpen}>
            </Dialog>
        )
    }

    public static async show(path: string, items: DataTransferItemList) {
       //  FileUploaderView.__singleton.handleOpen();
        const entries = await scanFiles(items);

        //fetch(`/api/disk${path}/${file.name}`, { method: 'PUT', body: file });
        entries.forEach(x => console.log(`/api/disk${path}${x.fullPath}`));
    }

    private handleOpen = () => this.setState({ isOpen: true })
    private handleClose = () => this.setState({ isOpen: false })
}