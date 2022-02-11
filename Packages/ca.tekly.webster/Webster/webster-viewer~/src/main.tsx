import * as React from 'react';
import { render } from 'react-dom';
import { Provider } from 'react-redux';

import './App.scss';

import configureStore from './app/Store';

import App from './App';

const store = configureStore();

const Root = () => (
	<Provider store={store}>
		<App />
	</Provider>
);

render(<Root />, document.getElementById('app'));
