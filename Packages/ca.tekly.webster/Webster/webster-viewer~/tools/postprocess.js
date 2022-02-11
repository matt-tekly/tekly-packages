const fs = require('fs');
const zlib = require('zlib');

const dir = './dist'
const outputFile = '../Assets/AssetsEmbedded.g.cs';

function gzip(file) {
	const buffer = fs.readFileSync(file);
	return zlib.gzipSync(buffer);
}

const files = fs.readdirSync(dir, {
    withFileTypes: true
});

const filteredFiles = [
    '.ttf',
    '.eot',
    'report.html'
]

function isFileAllowed(file) {
    if (!file.isFile()) {
        return false;
    }

    for (let i = 0; i < filteredFiles.length; i++) {
        if (file.name.endsWith(filteredFiles[i])) {
            return false;
        }
    }

    return true;
}

const content = files
    .filter(isFileAllowed)
    .map(file => ({
        fileName: file.name,
        fileData: gzip(`${dir}/${file.name}`).toString('base64')
    }));

const entries = content.map(c => `\n			{"${c.fileName}", @"${c.fileData}"}`);

const templateFile = 
`//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================
#if (WEBSTER_ENABLE || UNITY_EDITOR && WEBSTER_ENABLE_EDITOR) && !WEBSTER_DISABLE_FRONTEND
using System.Collections.Generic;

namespace Tekly.Webster.Assets
{
	public static partial class AssetsEmbedded
	{
		private static readonly Dictionary<string, string> Assets = new Dictionary<string, string> {${entries}
        };
    }
}
#endif
`

fs.writeFileSync(outputFile, templateFile);