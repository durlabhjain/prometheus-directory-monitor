const prometheus = require('prom-client');
const fs = require('fs');
const path = require('path');

const configFiles = ['./config.json', './config.local.json'];

const config = configFiles.reduce((baseConfig, configFile) => {
    if (fs.existsSync(configFile)) {
        const tempConfig = fs.readFileSync(configFile);
        return { ...baseConfig, ...JSON.parse(tempConfig) };
    }
    return baseConfig;
}, {});


const directorySizeGauge = new prometheus.Gauge({
    name: 'directory_size',
    help: 'Total size of directory',
    labelNames: ['directory_path']
});

function getDirectorySize(filePath) {
    let totalSize = 0;
    const files = fs.readdirSync(filePath, { withFileTypes: true });

    files.forEach(file => {
        const entryPath = path.join(filePath, file.name);
        if (file.isDirectory()) {
            totalSize += getDirectorySize(entryPath);
        } else {
            totalSize += fs.statSync(entryPath).size;
        }
    });

    return totalSize;
}

function updateDirectorySize() {
    const directories = config.directory.split(',');
    for (const directory of directories) {
        const loggingKey = `getSize: ${directory}`;
        console.time(loggingKey);
        const directorySize = getDirectorySize(directory);
        console.timeEnd(loggingKey);
        console.log(`${directory}: ${directorySize} bytes`)
        directorySizeGauge.set({ directory_path: directory }, directorySize);
    }
    setTimeout(() => {
        updateDirectorySize();
    }, config.intervalInSeconds);
}

updateDirectorySize();

const server = require('http').createServer(async (req, res) => {
    if (req.url === '/metrics') {
        res.setHeader('Content-Type', prometheus.register.contentType);
        res.end(await prometheus.register.metrics());
    }
});

server.listen(config.port);