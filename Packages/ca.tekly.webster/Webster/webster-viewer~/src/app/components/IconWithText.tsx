import * as React from 'react';

import { Icon, IIconProps } from '@blueprintjs/core';

export interface IIconWithTextProps extends IIconProps {
	text: string;
}

const spanStyle = {
	display: 'inline-flex'
};

const iconStyle = {
	margin : '0 2px 7px 0'
};

export default function IconWithText(props: IIconWithTextProps) {
	return (
		<span style={spanStyle}>
			<Icon {...props} style={iconStyle}/>
			{props.text}
		</span>
	);
}
