## About

This project is used to generate the static website for Webster.

It is built using:

* [TypeScript](https://www.typescriptlang.org/) 
* [Parcel](https://parceljs.org/) for compiling and packaging
* [React](https://reactjs.org/) for the frontend
* [Blueprintjs](https://blueprintjs.com/) for the React UI toolkit
* [Konva](https://konvajs.org/) for rendering Frameline using HTML5 canvas

## Building

There are a few dependencies to install first

1. [NodeJs](https://nodejs.org/en/)
1. [Yarn](https://yarnpkg.com/lang/en/)

Once you have those installed you must install the dependencies of the node project:

`yarn install`

Then you can build the production version of the website by running:

`yarn prod`

This step produces production versions of the assets for the website. The assets are then gzipped and written into `Webster/Assets/AssetsEmbedded.g.cs` file. This allows for compiling out any assets being used by the Webster website.

### Developing and Iterating

You can easily iterate on UI by:

1. Running your game Unity
1. Running `yarn start`. This will watch files for changes and rebuild them as they change. 

During development compiled files are placed in the `build` directory. Webster will search for resources in the `build` directory before using the production assets.

Running the game with Webster enabled will serve the website at http://localhost:4649/

It is recommended to place all custom UI and css in the `src/app/custom` directory. This will allow for developing custom UI without the risk of conflicting with new versions of Webster.

#### Adding your own pages
You can add your own pages by adding entries to the `src/app/custom/CustomAppRoutes.tsx` file. Follow the examples in the `src/AppRoutes.tsx` file.