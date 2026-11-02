const fs = require('node:fs')
const os = require('node:os')
const path = require('node:path')
const { execFileSync } = require('node:child_process')

const projectRoot = path.resolve(__dirname, '..')
const sourceWeb = path.join(projectRoot, 'dist', 'build', 'h5')
const androidRoot = path.join(projectRoot, 'android')
const webAssets = path.join(androidRoot, 'app', 'src', 'main', 'assets', 'www')
const iconSource = path.join(projectRoot, 'src', 'static', 'brand', 'app-icon.png')
const iconTarget = path.join(androidRoot, 'app', 'src', 'main', 'res', 'drawable', 'app_icon.png')

function findGradle() {
  const explicit = process.env.LMS_GRADLE
  if (explicit && fs.existsSync(explicit)) return explicit

  const dists = path.join(os.homedir(), '.gradle', 'wrapper', 'dists')
  if (!fs.existsSync(dists)) return ''

  const matches = []
  for (const distribution of fs.readdirSync(dists)) {
    const distributionPath = path.join(dists, distribution)
    if (!fs.statSync(distributionPath).isDirectory()) continue
    for (const hash of fs.readdirSync(distributionPath)) {
      const hashPath = path.join(distributionPath, hash)
      if (!fs.statSync(hashPath).isDirectory()) continue
      const bin = path.join(hashPath, distribution.replace(/-(all|bin)$/, ''), 'bin')
      const executable = path.join(bin, process.platform === 'win32' ? 'gradle.bat' : 'gradle')
      if (fs.existsSync(executable)) matches.push(executable)
    }
  }
  return matches.sort().at(-1) || ''
}

if (!fs.existsSync(sourceWeb)) {
  throw new Error('H5 build output is missing. Run npm run build:h5 first.')
}

fs.rmSync(webAssets, { recursive: true, force: true })
fs.mkdirSync(path.dirname(webAssets), { recursive: true })
fs.cpSync(sourceWeb, webAssets, { recursive: true })
fs.mkdirSync(path.dirname(iconTarget), { recursive: true })
fs.copyFileSync(iconSource, iconTarget)

const gradle = findGradle()
if (!gradle) {
  throw new Error('Gradle was not found. Set LMS_GRADLE to a Gradle executable.')
}

const androidStudioJava = 'C:\\Program Files\\Android\\Android Studio\\jbr'
const env = {
  ...process.env,
  ANDROID_HOME: process.env.ANDROID_HOME
    || path.join(os.homedir(), 'AppData', 'Local', 'Android', 'Sdk'),
  JAVA_HOME: process.env.JAVA_HOME
    || (fs.existsSync(androidStudioJava) ? androidStudioJava : ''),
}

const gradleCommand = process.platform === 'win32' ? process.env.ComSpec || 'cmd.exe' : gradle
const gradleArgs = process.platform === 'win32'
  ? ['/d', '/s', '/c', gradle, 'assembleDebug', '--no-daemon']
  : ['assembleDebug', '--no-daemon']

execFileSync(gradleCommand, gradleArgs, {
  cwd: androidRoot,
  env,
  stdio: 'inherit',
})

const apk = path.join(androidRoot, 'app', 'build', 'outputs', 'apk', 'debug', 'app-debug.apk')
if (!fs.existsSync(apk)) throw new Error('Gradle finished without producing an APK.')
console.log(`APK package: ${apk}`)
