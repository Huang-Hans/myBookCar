#!/usr/bin/env python3
"""LMS 应用服务部署脚本（部署机 192.168.30.65 的仓库克隆里执行）。

只管理应用服务：API 网关、五个业务 API（Platform / Dispatch / Booking / Notification / Reporting）与前端静态站点。
基础设施（MongoDB、Keycloak）由 backend/docker/docker-compose.yml 与
backend/docker/docker-compose.keycloak.yml 另行部署，本脚本不改动它们。

仅依赖 Python 标准库。build / deploy / apk 执行前自动 git pull --ff-only，拉取失败则停止。
首次部署顺序：pull（如需要）→ migrate → deploy。
migrate 使用官方 Flyway + mongosh 容器执行 JS 迁移，统一记录版本和校验和。
APK 成功复制到下载目录后，会清理同一应用的旧包，每个应用只保留本次生成的最新包。

用法：
    python3 ops.py all migrate            # 按时间执行统一目录的 DDL / DML（含正式和演示数据）
    python3 ops.py all migration-status   # 查看统一迁移记录（只读）
    python3 ops.py all deploy             # 拉取代码、构建镜像并启动全部应用服务，随后等待就绪
    python3 ops.py gateway deploy         # 只处理网关（platform / dispatch / booking / notification / reporting / web 同理）
    python3 ops.py all build              # 拉取代码后构建镜像
    python3 ops.py all start              # 用现有镜像启动，不重新构建
    python3 ops.py all stop               # 停止容器（保留容器与镜像）
    python3 ops.py all restart            # 重启容器（不重新构建）
    python3 ops.py all logs [-f] [--tail N]   # 查看日志，默认最后 200 行
    python3 ops.py gateway exec           # 进入容器 shell（仅单个目标可用）
    python3 ops.py all pull [--force]     # git fetch 后对齐到 origin/<当前分支>；与远端内容不同的文件会拦下
    python3 ops.py ps                     # 列出应用容器与访问地址
    python3 ops.py driver apk             # 拉取代码后打司机端 APK（调试签名）到 ~/downloads（网页端 /downloads/ 可下载）
    python3 ops.py user apk               # 打客服移动端 UserApp；client 为客户端，all / web 三端都打

常用环境变量（也可写在 backend/docker/.env，compose 与 migrate 共用）：
    LMS_HOST              部署机地址，默认 192.168.30.65
    MONGO_REPLICA_SET     MongoDB 副本集名，默认 rs0
    KEYCLOAK_AUTHORITY    Keycloak realm 地址，默认 http://192.168.30.65:8085/realms/lms
    LMS_MIGRATION_MONGO_URL 迁移连接串，需有五个业务库及 lms_migrations 的访问权限
    FLYWAY_URL / FLYWAY_USER / FLYWAY_PASSWORD
                          Flyway 连接配置；URL 优先于 LMS_MIGRATION_MONGO_URL，认证信息仅通过环境传入
    LMS_DOWNLOADS_DIR     APK 输出目录，默认 ~/downloads（与 compose 挂到网页端 /downloads 的目录一致）
    JAVA_HOME / ANDROID_HOME / LMS_GRADLE
                          apk 用的 JDK、Android SDK 与 Gradle，默认 ~/jdk-21、~/Android/Sdk、~/gradle/bin/gradle

APK 自己的配置与部署机的 backend/docker/.env 分开，写在 frontend/apk.env（随仓库提交）：
    LMS_APK_API_BASE_URL  APK 内调用的网关地址，必须是 https://（APK 页面运行在 HTTPS 源下，HTTP 接口会被当作混合内容拦截）
                          也可用同名环境变量临时覆盖；不读取 backend/docker/.env 里的同名项
"""

from __future__ import annotations

import argparse
import os
import re
import shutil
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path
from urllib.parse import parse_qsl, unquote, urlencode, urlsplit, urlunsplit

ROOT = Path(__file__).resolve().parent
COMPOSE_FILE = ROOT / "backend" / "docker" / "docker-compose.app.yml"
ENV_FILE = ROOT / "backend" / "docker" / ".env"
# APK 打包配置，与部署机 .env 分开：APK 在 HTTPS 源下运行，接口地址必须是 HTTPS，不能沿用部署机的内网 HTTP 地址。
APK_ENV_FILE = ROOT / "frontend" / "apk.env"
PROJECT = "lms-app"

DEFAULT_HOST = "192.168.30.65"
DEFAULT_REPLICA_SET = "rs0"
DEFAULT_KEYCLOAK_AUTHORITY = f"http://{DEFAULT_HOST}:8085/realms/lms"
MIGRATION_IMAGE = "redgate/flyway:13.10.3-mongo"

SERVICES = {
    "platform-api": {"container": "lms-platform-api", "port": 5688, "probe": "/readyz"},
    "dispatch-api": {"container": "lms-dispatch-api", "port": 5689, "probe": "/readyz"},
    "booking-api": {"container": "lms-booking-api", "port": 5690, "probe": "/readyz"},
    "notification-api": {"container": "lms-notification-api", "port": 5691, "probe": "/readyz"},
    "reporting-api": {"container": "lms-reporting-api", "port": 5692, "probe": "/readyz"},
    "api-gateway": {"container": "lms-api-gateway", "port": 5088, "probe": "/"},
    "admin-web": {"container": "lms-admin-web", "port": 5188, "probe": "/healthz"},
    "user-web": {"container": "lms-user-web", "port": 5189, "probe": "/healthz"},
    "driver-web": {"container": "lms-driver-web", "port": 5191, "probe": "/healthz"},
    "client-web": {"container": "lms-client-web", "port": 5192, "probe": "/healthz"},
}
TARGETS = {
    "all": list(SERVICES),
    "gateway": ["api-gateway"],
    "platform": ["platform-api"],
    "dispatch": ["dispatch-api"],
    "booking": ["booking-api"],
    "notification": ["notification-api"],
    "reporting": ["reporting-api"],
    "web": ["admin-web", "user-web", "driver-web", "client-web"],
    "user": ["user-web"],
    "driver": ["driver-web"],
    "client": ["client-web"],
}

# 移动端 APK：由各自 android/ 下的 WebView 壳工程打包。
# 键是对应业务的网页服务名，用于从目标选择移动端；值是（前端目录, 产物文件名前缀）。
MOBILE_APPS = {
    "user-web": ("LMS-UserApp", "lms-user"),
    "driver-web": ("LMS-DriverApp", "lms-driver"),
    "client-web": ("LMS-ClientApp", "lms-client"),
}
DEFAULT_APK_API_BASE_URL = "https://melco1.weprogram.site"
DEFAULT_APK_OUTPUT_DIR = Path.home() / "downloads"  # 与 docker-compose.app.yml 的 LMS_DOWNLOADS_DIR 默认值一致
# 部署机把这些路径写在 ~/.bashrc 的交互段里，非交互 SSH / 脚本拿不到，这里给出同样的默认值。
DEFAULT_JAVA_HOME = Path.home() / "jdk-21"
DEFAULT_ANDROID_HOME = Path.home() / "Android" / "Sdk"
DEFAULT_GRADLE = Path.home() / "gradle" / "bin" / "gradle"

def die(message: str) -> None:
    print(f"\n[✗] {message}", file=sys.stderr)
    raise SystemExit(1)


def ensure_docker_host() -> None:
    """部署机上 docker 由 podman 提供，未设置 DOCKER_HOST 时补上用户的 podman socket。"""
    if os.environ.get("DOCKER_HOST"):
        return
    socket_path = Path(f"/run/user/{os.getuid()}/podman/podman.sock")
    if socket_path.exists():
        os.environ["DOCKER_HOST"] = f"unix://{socket_path}"


def run(
    command: list[str], *, allow_fail: bool = False,
    cwd: Path = ROOT, env: dict[str, str] | None = None,
) -> subprocess.CompletedProcess:
    print(f"\n>>> {' '.join(command)}")
    completed = subprocess.run(command, cwd=cwd, env=env, check=False)
    if completed.returncode != 0 and not allow_fail:
        die(f"命令失败（exit {completed.returncode}）：{' '.join(command)}")
    return completed


def compose_bin() -> list[str]:
    if shutil.which("docker-compose"):
        return ["docker-compose"]
    if shutil.which("docker"):
        return ["docker", "compose"]
    die("未找到 docker-compose 或 docker，请先安装并加入 PATH。")


def compose(*arguments: str, allow_fail: bool = False) -> subprocess.CompletedProcess:
    ensure_docker_host()
    command = [*compose_bin(), "-p", PROJECT, "-f", str(COMPOSE_FILE)]
    if ENV_FILE.is_file():
        command += ["--env-file", str(ENV_FILE)]
    return run([*command, *arguments], allow_fail=allow_fail)


def load_env_file(path: Path = ENV_FILE) -> dict[str, str]:
    """读取 KEY=VALUE 文件的键值；默认是 compose 也读的部署机 .env，APK 打包另读 frontend/apk.env。"""
    values: dict[str, str] = {}
    if not path.is_file():
        return values
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#") or "=" not in stripped:
            continue
        key, _, value = stripped.partition("=")
        values[key.strip()] = value.strip()
    return values


def setting(env_file: dict[str, str], name: str, default: str) -> str:
    return os.environ.get(name) or env_file.get(name) or default


def deployment_host(env_file: dict[str, str]) -> str:
    return setting(env_file, "LMS_HOST", DEFAULT_HOST)


def check_environment(env_file: dict[str, str]) -> None:
    ensure_docker_host()
    if not shutil.which("docker") and not shutil.which("podman"):
        die("未找到 docker / podman。")
    if not COMPOSE_FILE.is_file():
        die(f"找不到 Compose 文件：{COMPOSE_FILE}")
    compose_bin()
    report_infra_state(env_file)


def report_infra_state(env_file: dict[str, str]) -> None:
    """应用服务依赖已在运行的基础设施；缺失只提示，不阻断（可能是本轮稍后才启动）。"""
    host = deployment_host(env_file)
    print(f"\n[·] 检查基础设施（{host}）：")
    print(f"    MongoDB  27017  {_tcp_state(host, 27017)}")
    print(f"    Keycloak 8085   {_http_state(setting(env_file, 'KEYCLOAK_AUTHORITY', DEFAULT_KEYCLOAK_AUTHORITY))}")


def _tcp_state(host: str, port: int) -> str:
    try:
        with socket.create_connection((host, port), timeout=3):
            return "可达"
    except OSError as error:
        return f"不可达（{error}）"


def _http_state(url: str) -> str:
    try:
        with urllib.request.urlopen(url, timeout=5) as response:
            return f"可达（HTTP {response.status}）"
    except urllib.error.HTTPError as error:
        return f"可达（HTTP {error.code}）"
    except (urllib.error.URLError, OSError) as error:
        return f"不可达（{error}）"


def resolve_services(target: str) -> list[str]:
    if target not in TARGETS:
        die(f"未知目标：{target}。可用：{', '.join(TARGETS)}")
    return list(TARGETS[target])


def build(services: list[str]) -> None:
    compose("build", *services)


def start(services: list[str]) -> None:
    compose("up", "-d", *services)


def stop(services: list[str]) -> None:
    compose("stop", *services)


def restart(services: list[str]) -> None:
    compose("restart", *services)


def deploy(target: str, services: list[str], *, no_build: bool, timeout: int) -> None:
    print(f"\n===== 部署 {target} =====")
    if not no_build:
        build(services)
    start(services)
    compose("ps", allow_fail=True)
    if not wait_until_ready(services, timeout):
        die("部署完成但就绪检查未通过，用 `python3 ops.py <目标> logs` 查看日志。")
    print_urls()


def wait_until_ready(services: list[str], timeout: int) -> bool:
    """逐个服务等探针通过（业务 API 是 /readyz，前端是 /healthz）；网关额外验证一条转发路由。"""
    probed = [name for name in services if name != "api-gateway"]
    deadline = time.monotonic() + timeout
    pending = list(probed)
    if pending:
        print(f"\n[·] 等待探针就绪（最长 {timeout}s）：{', '.join(pending)} ...")
    while pending and time.monotonic() < deadline:
        pending = [name for name in pending if not _readiness_ok(name)]
        if pending:
            time.sleep(3)

    if pending:
        print(f"\n[!] 等待 {timeout} 秒后仍未就绪：{', '.join(pending)}")
        return False

    for name in probed:
        service = SERVICES[name]
        print(f"\n[✓] {name} 就绪（http://127.0.0.1:{service['port']}{service['probe']}）")

    if "api-gateway" not in services:
        return True
    return _gateway_routing_ok(deadline)


def _readiness_ok(service: str) -> bool:
    url = f"http://127.0.0.1:{SERVICES[service]['port']}{SERVICES[service]['probe']}"
    try:
        with urllib.request.urlopen(url, timeout=5) as response:
            return 200 <= response.status < 300
    except (urllib.error.URLError, OSError):
        return False


def _gateway_routing_ok(deadline: float) -> bool:
    """网关横幅返回 20000 说明进程正常；员工路由 /api/platform/staff/v1/users 返回 401 说明路由与服务发现
    已生效（未带令牌被拒是预期结果，502/504 则说明目标服务没解析到）。路径须随网关路由表调整，
    旧版 /api/v1/users 在路由改为 {服务}/{身份段}/v1/{资源} 后已不存在，会误报 404。"""
    if not _wait_for(lambda: _status("http://127.0.0.1:5088/") == 200, deadline):
        print("\n[!] 网关横幅未返回 200。")
        return False

    status = _status("http://127.0.0.1:5088/api/platform/staff/v1/users")
    if status in (200, 401, 403):
        print(f"[✓] 网关转发正常（/api/platform/staff/v1/users → HTTP {status}，未带令牌时为 401/403）")
        return True
    print(f"\n[!] 网关转发异常（/api/platform/staff/v1/users → HTTP {status}），检查服务发现与后端服务状态。")
    return False


def _wait_for(predicate, deadline: float) -> bool:
    while time.monotonic() < deadline:
        if predicate():
            return True
        time.sleep(3)
    return False


def _status(url: str) -> int | None:
    try:
        with urllib.request.urlopen(url, timeout=5) as response:
            return response.status
    except urllib.error.HTTPError as error:
        return error.code
    except (urllib.error.URLError, OSError):
        return None


def migration_environment(env_file: dict[str, str]) -> dict[str, str]:
    """连接串通过进程环境传入容器，不展开到命令日志中。"""
    host = deployment_host(env_file)
    replica_set = setting(env_file, "MONGO_REPLICA_SET", DEFAULT_REPLICA_SET)
    connection = f"mongodb://{host}:27017/?replicaSet={replica_set}"
    configured = setting(env_file, "FLYWAY_URL", setting(env_file, "LMS_MIGRATION_MONGO_URL", connection))
    parsed = urlsplit(configured)
    if parsed.scheme not in ("mongodb", "mongodb+srv") or not parsed.netloc:
        die("请通过 FLYWAY_URL 或 LMS_MIGRATION_MONGO_URL 配置有效的 MongoDB 连接串。")
    options = parse_qsl(parsed.query, keep_blank_values=True)
    names = {name.lower() for name, _ in options}
    defaults = {"authSource": unquote(parsed.path.strip("/")) or "admin", "w": "majority"}
    options.extend((name, value) for name, value in defaults.items() if name.lower() not in names)
    url = urlunsplit(parsed._replace(path="/lms_migrations", query=urlencode(options)))
    return {
        **os.environ,
        "FLYWAY_URL": url,
        "FLYWAY_USER": setting(env_file, "FLYWAY_USER", ""),
        "FLYWAY_PASSWORD": setting(env_file, "FLYWAY_PASSWORD", ""),
    }


def migrate(env_file: dict[str, str], target: str, *, status: bool = False) -> None:
    """通过官方 Flyway 容器执行单目录 mongosh 迁移。"""
    if target != "all":
        die("迁移使用统一执行序列，请使用 all migrate / all migration-status。")
    ensure_docker_host()
    cli = "docker" if shutil.which("docker") else "podman"
    migrations = ROOT / "backend" / "migrations"
    environment = migration_environment(env_file)
    command = [cli, "run", "--rm", "--network", "host"]
    for name in ("FLYWAY_URL", "FLYWAY_USER", "FLYWAY_PASSWORD"):
        if environment[name]:
            command += ["--env", name]
    command += [
        "--mount", f"type=bind,source={migrations / 'scripts'},target=/flyway/sql,readonly",
        "--mount", f"type=bind,source={migrations / 'flyway.conf'},target=/flyway/conf/lms.conf,readonly",
        MIGRATION_IMAGE, "-configFiles=/flyway/conf/lms.conf", "info" if status else "migrate",
    ]
    run(command, env=environment)


def git_output(*arguments: str) -> str:
    completed = subprocess.run(
        ["git", "-C", str(ROOT), *arguments],
        capture_output=True, text=True, check=True,
    )
    return completed.stdout


def content_differs_from(ref: str) -> list[str]:
    """对齐到 ref 会被覆盖、且内容与 ref 不同的文件。

    只看内容差异，不看相对 HEAD 的脏状态：把本机工作区拷到部署机后（相对 HEAD 是脏的、
    内容却与远端一致）不应该拦；未跟踪文件只有 ref 里也有同名的才算会被覆盖。
    """
    changed = git_output("diff", "--name-only", ref, "--", ".").splitlines()
    untracked = git_output("ls-files", "--others", "--exclude-standard").splitlines()
    tracked_by_ref = [
        path for path in untracked
        if subprocess.run(
            ["git", "-C", str(ROOT), "cat-file", "-e", f"{ref}:{path}"],
            capture_output=True,
        ).returncode == 0
    ]
    if tracked_by_ref:
        changed += git_output("diff", "--name-only", ref, "--", *tracked_by_ref).splitlines()
    return sorted(set(changed))


def pull(force: bool) -> None:
    branch = git_output("rev-parse", "--abbrev-ref", "HEAD").strip()
    target = f"origin/{branch}"
    ensure_docker_host()
    run(["git", "-C", str(ROOT), "fetch", "origin"])
    conflicts = content_differs_from(target)
    if conflicts and not force:
        preview = "、".join(conflicts[:3]) + ("…" if len(conflicts) > 3 else "")
        die(
            f"工作区有 {len(conflicts)} 个文件与 {target} 内容不同（{preview}），pull 会丢弃它们。"
            f"确认远端已包含需要的内容后用 `pull --force`。"
        )
    run(["git", "-C", str(ROOT), "reset", "--hard", target])
    print(f"\n[✓] 代码已对齐到 {target}。")


def build_apks(env_file: dict[str, str], services: list[str]) -> None:
    """目标里每个有移动端的网页服务打一个 APK，依次执行（部署机内存有限，不并行），最后汇总产物路径。"""
    apps = [MOBILE_APPS[name] for name in services if name in MOBILE_APPS]
    if not apps:
        die("该目标没有移动端应用，可用：user / driver / client / web / all。")
    build_env = android_build_env(env_file)
    output_dir = Path(setting(env_file, "LMS_DOWNLOADS_DIR", str(DEFAULT_APK_OUTPUT_DIR))).expanduser()
    output_dir.mkdir(parents=True, exist_ok=True)
    artifacts = [build_apk(directory, prefix, build_env, output_dir) for directory, prefix in apps]
    print("\n[✓] APK 已生成：")
    for artifact in artifacts:
        print(f"    {artifact}")


def android_build_env(env_file: dict[str, str]) -> dict[str, str]:
    """组装打包环境：确认 JDK / Android SDK / Gradle / npm 可用，再注入网关地址与地图 key（都是构建期变量）。"""
    java_home = Path(setting(env_file, "JAVA_HOME", str(DEFAULT_JAVA_HOME))).expanduser()
    android_home = Path(setting(env_file, "ANDROID_HOME", str(DEFAULT_ANDROID_HOME))).expanduser()
    gradle = Path(setting(env_file, "LMS_GRADLE", str(DEFAULT_GRADLE))).expanduser()
    required = (("JDK", java_home / "bin" / "java"), ("Android SDK", android_home / "platforms"), ("Gradle", gradle))
    missing = [f"{label}（{path}）" for label, path in required if not path.exists()]
    if missing:
        die(f"找不到 {'、'.join(missing)}，可用 JAVA_HOME / ANDROID_HOME / LMS_GRADLE 指定。")
    if not shutil.which("npm"):
        die("未找到 npm，请先安装 Node.js。")
    api_base_url = apk_api_base_url()
    print(f"\n[·] APK 网关地址：{api_base_url}（来自 {APK_ENV_FILE.relative_to(ROOT)}）")
    return {
        **os.environ,
        "JAVA_HOME": str(java_home),
        "ANDROID_HOME": str(android_home),
        "ANDROID_SDK_ROOT": str(android_home),
        "LMS_GRADLE": str(gradle),
        "PATH": f"{java_home / 'bin'}{os.pathsep}{os.environ.get('PATH', '')}",
        "VITE_LMS_APK_API_ORIGIN": api_base_url,
        # 地图 key 与网页端镜像共用 .env 里的 AMAP_*（compose 以 build.args 传给网页端，这里换成 VITE_ 前缀）。
        "VITE_AMAP_WEB_KEY": setting(env_file, "AMAP_WEB_KEY", ""),
        "VITE_AMAP_SECURITY_JS_CODE": setting(env_file, "AMAP_SECURITY_JS_CODE", ""),
    }


def apk_api_base_url() -> str:
    """APK 的网关地址只取 frontend/apk.env（或同名环境变量），不读部署机 .env；不是 HTTPS 时直接停止打包。"""
    value = setting(load_env_file(APK_ENV_FILE), "LMS_APK_API_BASE_URL", DEFAULT_APK_API_BASE_URL).rstrip("/")
    if not value.startswith("https://"):
        die(f"APK 网关地址必须是 https://（当前 {value}）：APK 页面运行在 HTTPS 源下，HTTP 接口会被 WebView 当作混合内容拦截。")
    return value


def build_apk(app_directory: str, artifact_prefix: str, build_env: dict[str, str], output_dir: Path) -> Path:
    project_dir = ROOT / "frontend" / app_directory
    print(f"\n===== 打包 APK：{app_directory} =====")
    # 按 package-lock.json 安装依赖，与网页端 Dockerfile 的 npm ci 一致。
    run(["npm", "ci"], cwd=project_dir, env=build_env)
    # 复用仓库已有的 build:android-apk：先 build:h5（环境变量 VITE_LMS_APK_API_ORIGIN 决定 APK 内的网关地址），
    # 再把 H5 产物拷进 android/ 壳工程，由 Gradle assembleDebug 出包（调试签名，自动使用 ~/.android/debug.keystore）。
    run(["npm", "run", "build:android-apk"], cwd=project_dir, env=build_env)
    built = project_dir / "android" / "app" / "build" / "outputs" / "apk" / "debug" / "app-debug.apk"
    if not built.is_file():
        die(f"Gradle 结束但没有产物：{built}")
    # 复制而不是移动：android/app/build 下保留原产物，便于 Gradle 增量构建。
    target = output_dir / apk_file_name(project_dir, artifact_prefix)
    shutil.copy2(built, target)
    remove_previous_apks(target, artifact_prefix)
    return target


def remove_previous_apks(latest: Path, artifact_prefix: str) -> None:
    """新包已复制成功后，只清理下载目录中同一应用生成的旧 APK。"""
    for artifact in latest.parent.glob(f"{artifact_prefix}-*-debug.apk"):
        if artifact == latest or not artifact.is_file():
            continue
        artifact.unlink()
        print(f"[·] 已删除旧 APK：{artifact.name}")


def apk_file_name(project_dir: Path, artifact_prefix: str) -> str:
    """产物名 <前缀>-<versionName>-<yyyyMMddHHmm>-debug.apk，不包含 Git 提交哈希。"""
    gradle_script = (project_dir / "android" / "app" / "build.gradle").read_text(encoding="utf-8")
    version = re.search(r'versionName\s+"([^"]+)"', gradle_script)
    timestamp = time.strftime("%Y%m%d%H%M")
    return f"{artifact_prefix}-{version.group(1) if version else 'unknown'}-{timestamp}-debug.apk"


def logs(services: list[str], follow: bool, tail: int) -> None:
    arguments = ["logs", "--tail", str(tail)]
    if follow:
        arguments.append("-f")
    arguments += services
    compose(*arguments, allow_fail=True)


def exec_shell(services: list[str]) -> None:
    if len(services) != 1:
        die("exec 需要单个目标，例如 `python3 ops.py gateway exec`。")
    compose("exec", services[0], "sh")


def print_urls(env_file: dict[str, str] | None = None) -> None:
    host = (env_file or {}).get("LMS_HOST") or os.environ.get("LMS_HOST") or DEFAULT_HOST
    print("\n访问地址：")
    print(f"  Admin Portal    http://{host}:{SERVICES['admin-web']['port']}/portal/locations")
    print(f"  User Console    http://{host}:{SERVICES['user-web']['port']}/console/tracking")
    print(f"  Driver App      http://{host}:{SERVICES['driver-web']['port']}/")
    print(f"  Client App      http://{host}:{SERVICES['client-web']['port']}/")
    print(f"  网关横幅        http://{host}:5088/")
    print(f"  网关接口文档    http://{host}:5088/swagger")
    for name, service in SERVICES.items():
        if name in ("api-gateway", "admin-web", "user-web", "driver-web", "client-web"):
            continue
        print(f"  {name:<15} http://{host}:{service['port']}/swagger   探针 /healthz、/readyz")


def show_state() -> None:
    compose("ps", allow_fail=True)
    print_urls()


def main() -> None:
    parser = argparse.ArgumentParser(
        description="LMS 应用服务部署工具（API 网关 + 五个业务 API）",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="首次部署：python3 ops.py all migrate → python3 ops.py all deploy",
    )
    parser.add_argument("target", choices=[*TARGETS, "ps"], help="all / gateway / platform / dispatch / booking / notification / reporting / web / user / driver / client，或 ps")
    parser.add_argument(
        "operation", nargs="?",
        choices=["migrate", "migration-status", "build", "deploy", "start", "stop", "restart", "logs", "exec", "pull", "apk"],
        help="要执行的操作；target 为 ps 时可省略",
    )
    parser.add_argument("--no-build", action="store_true", help="deploy 时跳过构建，直接用现有镜像")
    parser.add_argument("--force", action="store_true", help="pull 时覆盖与远端内容不同的本地文件")
    parser.add_argument("-f", "--follow", action="store_true", help="logs 时持续跟踪")
    parser.add_argument("--tail", type=int, default=200, help="logs 显示的行数，默认 200")
    parser.add_argument("--timeout", type=int, default=180, help="等待就绪的秒数，默认 180")
    arguments = parser.parse_args()

    if arguments.target == "ps":
        show_state()
        return
    if not arguments.operation:
        parser.error("缺少操作，例如：python3 ops.py all deploy")

    services = resolve_services(arguments.target)
    operation = arguments.operation

    if operation == "pull":
        pull(arguments.force)
        return
    if operation in ("build", "deploy", "apk"):
        run(["git", "-C", str(ROOT), "pull", "--ff-only"])

    env_file = load_env_file()
    if operation == "apk":
        # 打包不涉及容器与基础设施，跳过 check_environment。
        build_apks(env_file, services)
        return

    check_environment(env_file)
    match operation:
        case "migrate":
            migrate(env_file, arguments.target)
        case "migration-status":
            migrate(env_file, arguments.target, status=True)
        case "build":
            build(services)
        case "deploy":
            deploy(arguments.target, services, no_build=arguments.no_build, timeout=arguments.timeout)
        case "start":
            start(services)
            wait_until_ready(services, arguments.timeout)
            print_urls(env_file)
        case "stop":
            stop(services)
        case "restart":
            restart(services)
            wait_until_ready(services, arguments.timeout)
        case "logs":
            logs(services, arguments.follow, arguments.tail)
        case "exec":
            exec_shell(services)


if __name__ == "__main__":
    main()
