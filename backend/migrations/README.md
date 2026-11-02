# MongoDB 数据库迁移

使用官方 `redgate/flyway:13.10.3-mongo` 镜像，内置 Flyway 和 mongosh。部署机仅需 Docker / Podman，不需要安装 Java、Node.js 或 mongosh，不再安装 npm 依赖或构建专用迁移镜像。

## 目录与写法

全部 DDL / DML 放在一个目录，直接写 mongosh 语句，没有 `exports.up/down`、公共执行器或辅助模块。

```text
scripts/
  20261022000100-ddl-platform-initial.js
  20261022000200-ddl-dispatch-initial.js
  20261022000300-ddl-booking-initial.js
  20261022000400-ddl-notification-initial.js
  20261022000500-ddl-reporting-initial.js
  20261022001100-dml-platform-role-permissions.js
  20261022001200-dml-platform-common-locations.js
  20261022001300-dml-platform-configuration.js
  20261022002100-dml-platform-demo-users.js
  20261022002200-dml-dispatch-demo-drivers.js
  20261022002300-dml-booking-demo-guests.js
```

命名固定为 `{yyyyMMddHHmmss}-ddl-{说明}.js` 或 `{yyyyMMddHHmmss}-dml-{说明}.js`。`flyway.conf` 将版本前缀设为空、分隔符设为 `-`，因此不需要改成默认的 `V1__...` 格式。DDL / DML 按同一时间序列执行；非法文件名会报错。

```js
db = db.getSiblingDB('lms_booking');
db.trips.createIndex({ tripReference: 1 }, { unique: true });
```

每个文件明确选择业务库。初始 DDL 保留已有集合和相同索引，索引冲突时停止。初始 DML 内嵌正式与演示数据，使用 mongosh 原生 `EJSON.deserialize` 保留原 BSON 类型，再用 `$setOnInsert` 补充缺失数据，已有账号和配置不覆盖。迁移不执行额外的管理员开户流程。

运营经理权限直接定义在初始权限种子中，包含与 65 环境一致的全部 29 项权限。使用 `$setOnInsert` 初始化缺失角色，不覆盖数据库中已有的角色授权。

`20261029000100-dml-platform-member-edit-permission.js` 为已有 `guests:read` 权限的运营经理补充 `guests:write`，支持会员管理编辑；使用 `$addToSet` 保留其他权限，未授权会员查询的角色不自动增加编辑权限。超级管理员沿用代码中的完整权限集合。此脚本需要按部署流程执行，代码修改不会直接更改数据库授权；已有会话需重新登录或刷新令牌后读取新权限。

## 执行

在部署机仓库根目录执行：

```bash
python3 ops.py all migration-status
python3 ops.py all migrate
```

`migration-status` 调用 Flyway `info`，`migrate` 调用 Flyway `migrate`。入口只支持 `all`，保证同一版本序列完整执行。每次使用临时容器，只读挂载 `scripts/` 与 `flyway.conf`，退出后删除容器；不启动或重新部署业务服务。迁移不会自动拉取代码，应先确认仓库版本。

## 连接配置

`ops.py` 从进程环境或 `backend/docker/.env` 读取配置，连接及认证信息只经环境传入，不展开到命令日志。

| 环境变量 | 用途 |
|---|---|
| `FLYWAY_URL` | MongoDB 连接串，优先于下面的兼容配置 |
| `LMS_MIGRATION_MONGO_URL` | 兼容原有的迁移连接串配置 |
| `FLYWAY_USER` / `FLYWAY_PASSWORD` | 可选认证信息，也可沿用连接串内的认证信息 |
| `LMS_HOST` / `MONGO_REPLICA_SET` | 未配置迁移连接串时，沿用现有部署主机与副本集名 |

入口将连接串的默认数据库统一设为 `lms_migrations`，保留服务器和连接选项。未显式配置 `authSource` 时，沿用原连接串的数据库作为认证库；原连接串没有数据库时使用 `admin`。未指定 `w` 时使用 `majority`。迁移账号需能访问五个业务库及 `lms_migrations`。

直接使用官方容器时，应自行提供默认数据库为 `lms_migrations` 的 `FLYWAY_URL`，并挂载本目录配置和脚本；上面的地址兼容处理由 `ops.py` 提供。

## 版本记录与失败恢复

本次按要求将批量排班、同日多班及撤回排班的结构合并到原始 `20261022000200-ddl-dispatch-initial.js`，不再保留对应的三个独立迁移。原始 DDL 直接创建 `shift_batches` 集合，并以 `ux_active_driver_date_period` 部分唯一索引约束未撤回排班的司机、业务日期和时段。

排班及批次不再存储物业；读取时按 `driverId` 关联司机当前物业。旧记录中的 `propertyId` 被忽略，新建或替换写入均不包含它。初始 DDL 的日期索引改为 `ix_date_period`，不再使用排班物业索引。本次不清理现有数据中的旧字段，也不执行索引或迁移变更。

此调整用于定义新的初始化结构，不会自动升级已初始化的数据库。已经执行旧版初始 DDL 或已删除迁移的环境，需要单独核对实际结构、初始脚本校验和及 Flyway 历史记录；不得直接假定再次执行 `migrate` 就会应用这些变化。本次不执行数据库变更、repair、clean 或历史记录修改。

- 版本、校验和及执行历史由 Flyway 管理，历史集合为 `lms_migrations.flyway_schema_history`；删除临时容器不会删除记录。
- 已执行文件不再修改或改名，后续调整新增时间戳文件。占位符替换关闭，避免修改模板或初始数据中的文本。
- 保留原有 `schema_migrations`、`schema_migration_leases`、`migration_changelog` 等旧记录；不自动导入旧执行历史，不自动 baseline，也不清库。
- 切换前停止旧迁移任务。如果记录库已有旧数据而没有 Flyway 历史，Flyway 可能要求显式 baseline，应先核对历史后由运维处理，不能以清空记录库绕过。
- MongoDB JS 迁移按非事务方式执行，单个文件中途失败时，之前成功的写入可能已保留。先排查并修复原因，核对实际写入及 Flyway 的失败记录；需要 repair 时，核对后手动处理，不自动 repair 或重试。
- 所有迁移由单一发布任务串行执行，不并发启动多个迁移容器；框架使用自身执行管理，不再维护自定义锁。
- Flyway 默认禁用 `clean`；现有运维入口仅提供 `info` 和 `migrate`。
- 报表结构初始化不等于历史报表回填，继续使用现有的 `REPORT_BACKFILL_FROM` / `REPORT_BACKFILL_TO` 业务流程。

参考：[Flyway MongoDB 支持](https://documentation.red-gate.com/fd/mongodb-341246448.html)、[MongoDB 原生 JS 迁移教程](https://documentation.red-gate.com/fd/tutorial-using-native-connectors-to-connect-to-mongodb-277579356.html)。

排班也不再存储 `vehiclePlate` 或 `vehicleSlot`；车辆通过司机的 `assignedVehicleId` 从车辆表读取。司机未保存该 ID 时，兼容车辆表中的 `driverId` 反向绑定。读取均校验司机与车辆物业一致，换车影响该司机全部排班（含历史）。原始 DDL 不再创建排班车辆占用索引 `ux_vehicle_slot`。旧排班车辆字段和已有索引未作数据库清理；本次不执行迁移。
