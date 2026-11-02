# HarmonyOS 构建说明

本项目是 Vue 3 UniApp 工程，已在 `src/manifest.json` 中配置 `app-harmony`。源码可由 HBuilderX 调用 DevEco Studio 工具链生成 HarmonyOS 应用。

## 环境准备

1. 安装最新版 HBuilderX，建议 4.81 或更高版本；当前机器已配置 HBuilderX 5.24。
2. 安装 DevEco Studio 5.1.0.849 或更高版本；当前机器已检测到 DevEco Studio 26.0.0.821。
3. 在 HBuilderX 的“工具 → 设置 → 运行配置”中选择 DevEco Studio 安装目录。
4. 在 HBuilderX 中打开本项目，在 `manifest.json` 可视化界面获取/填写实际 DCloud AppID。
5. 在“鸿蒙 App 配置”中确认应用包名为 `com.melco.lms.client`，正式项目应换成你在 AppGallery Connect 登记的唯一包名。

Windows x86 模拟器请选择 API 19 或更高版本，官方建议 API 20+；鸿蒙真机要求 API 12 以上。

## 命令行原生构建

当前项目提供了一条可重复执行的完整构建命令：

```bash
npm run build:harmony-native
```

它会依次：

1. 生成 `dist/build/app-harmony` UniApp 鸿蒙资源。
2. 使用 HBuilderX 的官方鸿蒙模板组装原生工程。
3. 调用 DevEco Studio 的 `ohpm` 和 `hvigor` 构建 HAP。

原生工程输出到 `unpackage/dist/build/app-harmony`。未配置证书时，HAP 位于：

```text
unpackage/dist/build/app-harmony/entry/build/default/outputs/default/entry-default-unsigned.hap
```

如果 HBuilderX 或 DevEco Studio 不在默认目录，可传入：

```powershell
node scripts/build-harmony-native.cjs --hbuilderx "D:\Tools\HBuilderX" --deveco "D:\Huawei\DevEco Studio"
```

Windows 遇到 `ohpm` 的 `EPERM Rename File Error` 时，通常是工程路径过深。可将临时原生工程输出到短路径：

```powershell
node scripts/build-harmony-native.cjs --output "C:\bc-harmony-native"
```

## 调试运行

1. 用 DevEco Studio 启动鸿蒙模拟器，或连接已开启开发者模式的鸿蒙真机。
2. HBuilderX 选择“运行 → 运行到手机或模拟器 → 运行到鸿蒙”。
3. 真机运行时按提示自动申请或配置调试证书。
4. HBuilderX 会在 `unpackage/dist/dev/app-harmony` 生成鸿蒙工程，并构建 `.hap` 调试包后安装到设备。

## 正式发行

1. 在 HBuilderX 的鸿蒙配置中设置发布签名证书。
2. 选择“发行 → App-Harmony-本地打包”。
3. 生成的鸿蒙工程位于 `unpackage/dist/build/app-harmony`，签名发行产物为 `.app` 文件。
4. 将 `.app` 提交到 AppGallery Connect；企业内部分发可使用调试流程生成的 `.hap` 和对应内测证书。

## 定位权限

`src/manifest.json` 已启用 `uni-location.system`，并且 `harmony-configs` 已配置：

- `ohos.permission.LOCATION`
- `ohos.permission.APPROXIMATELY_LOCATION`

定位用途说明为“用于获取上车位置并提供行程预订服务”。配置文件保留了 HBuilderX 模板的完整模块定义，不需要再手动复制。

官方文档：

- https://uniapp.dcloud.net.cn/tutorial/harmony/runbuild
- https://uniapp.dcloud.net.cn/collocation/manifest#app-harmony
