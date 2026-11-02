# LMS app launcher icons

The Client, Driver, and User apps share the existing MELCO monogram but use distinct background and role colors. The checked-in PNGs are generated from the logo in `LMS-ClientApp/src/static/figma/melco-logo.png`.

Run `powershell -File scripts/generate-app-icons.ps1` from the `frontend` directory after changing the icon design. Then run `node scripts/verify-app-icons.cjs` to check every configured PNG size, iOS opacity, Android native resource, and H5 favicon. Each app's `npm test` runs the same verification.

The generator synchronizes `android/app/src/main/res/drawable/app_icon.png`, and each APK build script refreshes it again from `src/static/brand/app-icon.png`. Uni-app's manifest points to density-specific Android icons, iOS launcher icons, and HarmonyOS foreground/background layers. The three `index.html` files use the 1024-pixel icon as the browser favicon.
