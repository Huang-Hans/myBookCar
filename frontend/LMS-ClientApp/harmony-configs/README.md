# HarmonyOS 配置目录

HBuilderX 运行到鸿蒙后，会将此目录中的文件合并到生成的鸿蒙工程。

当前项目已在 `src/manifest.json` 的 `app-harmony` 节点声明包名与系统定位模块，并在本目录配置了鸿蒙定位权限：

- `ohos.permission.LOCATION`
- `ohos.permission.APPROXIMATELY_LOCATION`

`entry/src/main/module.json5` 保留了 HBuilderX 模板的完整模块定义，定位授权说明位于对应的 `resources/base/element/string.json`。构建时不要用不完整文件替换它们，否则会覆盖完整配置。
