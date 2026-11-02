const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const crypto = require('node:crypto')

const frontendRoot = path.resolve(__dirname, '..')
const apps = ['LMS-ClientApp', 'LMS-DriverApp', 'LMS-UserApp']
const androidSizes = { mdpi: 48, hdpi: 72, xhdpi: 96, xxhdpi: 144, xxxhdpi: 192 }
const iosSizes = {
  appstore: 1024,
  'iphone.app@2x': 120,
  'iphone.app@3x': 180,
  'ipad.app': 76,
  'ipad.app@2x': 152,
  'ipad.proapp@2x': 167,
}
const iconHashes = new Set()

function pngInfo(filePath) {
  const data = fs.readFileSync(filePath)
  assert.equal(data.subarray(0, 8).toString('hex'), '89504e470d0a1a0a', `${filePath} is not PNG`)
  return { data, width: data.readUInt32BE(16), height: data.readUInt32BE(20), colorType: data[25] }
}

function checkIcon(appRoot, relativePath, size, isOpaque = false) {
  assert.match(relativePath, /^static\/brand\//)
  const filePath = path.join(appRoot, 'src', relativePath)
  const info = pngInfo(filePath)
  assert.equal(info.width, size, `${filePath} width`)
  assert.equal(info.height, size, `${filePath} height`)
  if (isOpaque) assert.equal(info.colorType, 2, `${filePath} must have no alpha channel`)
  return info.data
}

for (const app of apps) {
  const appRoot = path.join(frontendRoot, app)
  const manifest = JSON.parse(fs.readFileSync(path.join(appRoot, 'src', 'manifest.json'), 'utf8'))
  const appIcon = checkIcon(appRoot, 'static/brand/app-icon.png', 1024, true)
  const configuredIcons = manifest['app-plus'].distribute.icons
  for (const [density, size] of Object.entries(androidSizes)) {
    checkIcon(appRoot, configuredIcons.android[density], size)
  }
  for (const [key, size] of Object.entries(iosSizes)) {
    const segments = key.split('.')
    const relativePath = segments.length === 1
      ? configuredIcons.ios[segments[0]]
      : configuredIcons.ios[segments[0]][segments[1]]
    checkIcon(appRoot, relativePath, size, true)
  }

  const harmonyIcons = manifest['app-harmony'].distribute.icons
  checkIcon(appRoot, harmonyIcons.foreground, 1024)
  checkIcon(appRoot, harmonyIcons.background, 1024, true)
  assert.deepEqual(
    pngInfo(path.join(appRoot, 'android', 'app', 'src', 'main', 'res', 'drawable', 'app_icon.png')).data,
    appIcon,
    `${app} native Android icon must match the configured brand icon`,
  )
  assert.match(
    fs.readFileSync(path.join(appRoot, 'index.html'), 'utf8'),
    /<link rel="icon" type="image\/png" href="\/static\/brand\/app-icon\.png"/,
  )
  iconHashes.add(crypto.createHash('sha256').update(appIcon).digest('hex'))
  console.log(`${app} launcher icons verified`)
}

assert.equal(iconHashes.size, apps.length, 'Each app must have a distinct launcher icon')
