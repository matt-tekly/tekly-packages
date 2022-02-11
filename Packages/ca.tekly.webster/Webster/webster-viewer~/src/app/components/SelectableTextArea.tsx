import * as React from 'react';

import { Hotkey, Hotkeys, HotkeysTarget } from '@blueprintjs/core';

interface ISelectableTextAreaProps {
	value: string;
	className?: string;
}

@HotkeysTarget
export default class SelectableTextArea extends React.PureComponent<ISelectableTextAreaProps> {

	private divRef: React.RefObject<HTMLDivElement>;

	constructor(props: ISelectableTextAreaProps) {
		super(props);
		this.divRef = React.createRef<HTMLDivElement>();
	}

	public render() {
		return (
			<div className={this.props.className} ref={this.divRef}>
				{this.props.value}
			</div>
		);
	}

	public renderHotkeys() {
		return (
			<Hotkeys>
				<Hotkey global={true} combo='mod + a' label='Be awesome all the time' onKeyDown={this.onSelectAll} />
			</Hotkeys>
		);
	}

	private onSelectAll = (e: KeyboardEvent) => {
		if (!this.divRef.current) {
			return;
		}

		const selection = window.getSelection();
		if (!selection) {
			return;
		}

		e.preventDefault();
		e.stopPropagation();
		selection.selectAllChildren(this.divRef.current);
	}
}
