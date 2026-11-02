const fs = require('node:fs')
const os = require('node:os')
const path = require('node:path')
const { execFileSync } = require('node:child_process')

const projectRoot = path.resolve(__dirname, '..')
const compilePath = path.join(projectRoot, 'dist', 'build', 'app-harmony')

function readArg(name) {
  const index = process.argv.indexOf(name)
  return index >= 0 ? process.argv[index + 1] : ''
}

const harmonyProjectPath = readArg('--output')
  ? path.resolve(readArg('--output'))
  : path.join(projectRoot, 'unpackage', 'dist', 'build', 'app-harmony')

function firstExisting(candidates) {
  return candidates.find((candidate) => candidate && fs.existsSync(candidate)) || ''
}

const hbuilderxHome = firstExisting([
  readArg('--hbuilderx'),
  process.env.HBUILDERX_HOME,
  path.join(os.homedir(), 'Tools', 'HBuilderX-5.24', 'HBuilderX'),
  'C:\\Program Files\\HBuilderX',
])

const devecoHome = firstExisting([
  readArg('--deveco'),
  process.env.DEVECO_STUDIO_HOME,
  'C:\\Program Files\\Huawei\\DevEco Studio',
])

function requireDirectory(value, label) {
  if (!value) {
    throw new Error(
      `${label} not found. Pass its path with the corresponding command-line option.`,
    )
  }
}

function restoreBundledHarmonyLibraries() {
  const packageFile = path.join(harmonyProjectPath, 'oh-package.json5')
  const harmonyPackage = JSON.parse(fs.readFileSync(packageFile, 'utf8'))
  const dependencies = Object.values(harmonyPackage.dependencies || {})
  const missingLibraries = dependencies
    .filter((value) => typeof value === 'string' && value.startsWith('./libs/'))
    .map((value) => value.slice(2).replaceAll('\\', '/'))
    .filter((value) => !fs.existsSync(path.join(harmonyProjectPath, value)))

  if (!missingLibraries.length) return

  const templateDirectory = path.join(
    hbuilderxHome,
    'plugins',
    'launcher-harmony',
    'base',
  )
  const fallbackTemplates = [
    'project-dom2.zip',
    'project-dom2-dyn.zip',
    'project-x.zip',
  ].map((name) => path.join(templateDirectory, name))

  for (const library of missingLibraries) {
    const sourceTemplate = fallbackTemplates.find((template) => {
      if (!fs.existsSync(template)) return false
      const entries = execFileSync('tar', ['-tf', template], {
        encoding: 'utf8',
      })
      return entries.split(/\r?\n/).includes(library)
    })

    if (!sourceTemplate) {
      throw new Error(`Required Harmony library is missing: ${library}`)
    }

    execFileSync(
      'tar',
      ['-xf', sourceTemplate, '-C', harmonyProjectPath, library],
      { stdio: 'inherit' },
    )
    console.log(`Restored bundled Harmony library: ${library}`)
  }
}

async function main() {
  requireDirectory(hbuilderxHome, 'HBuilderX')
  requireDirectory(devecoHome, 'DevEco Studio')
  requireDirectory(compilePath, 'Compiled app-harmony resources')

  const launcherPath = path.join(
    hbuilderxHome,
    'plugins',
    'launcher',
    'out',
    'export',
    'HarmonyLauncher.js',
  )
  const templatePath = path.join(
    hbuilderxHome,
    'plugins',
    'launcher-harmony',
    'base',
    'project.zip',
  )

  if (!fs.existsSync(launcherPath) || !fs.existsSync(templatePath)) {
    throw new Error(
      'HBuilderX Harmony plugins are missing. Install “App真机运行” and “鸿蒙真机运行” first.',
    )
  }

  const { HarmonyLauncher } = require(launcherPath)
  const launcher = new HarmonyLauncher()

  await launcher.init({
    devToolsPath: devecoHome,
    projectPath: projectRoot,
    compilePath,
    udid: '',
  })

  // The launcher defaults to its debug folder. A release build is kept in the
  // standard HBuilderX local-package path so it can also be opened in DevEco.
  launcher.harmonyProjectPath = harmonyProjectPath

  await launcher.prepareHarmonyProjectPath()
  await launcher.assembleCompileOutput()
  await launcher.assembleCompiledManifest()
  await launcher.assembleSigningConfigs()
  restoreBundledHarmonyLibraries()
  await launcher.installDependencies()
  await launcher.buildHap()

  console.log(`Harmony project: ${launcher.harmonyProjectPath}`)
  console.log(`HAP package: ${launcher.hapPackagePath}`)
}

main().catch((error) => {
  console.error(error instanceof Error ? error.stack : error)
  process.exitCode = 1
})
