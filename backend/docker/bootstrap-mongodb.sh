#!/usr/bin/env bash
# 初始化 LMS 开发用 MongoDB 的单节点副本集（rs0）。可重复执行。
#
# 必须在数据库主机上运行：脚本通过 docker exec 在容器内执行 mongosh。
# MongoDB 以多文档事务方式写入，单机模式不支持事务，因此必须初始化副本集；
# 成员地址必须是客户端可达的地址，否则客户端会被指到容器内网地址、连不上。
#
# 未启用授权：能连到本机的任何人都可以读写全部数据库，本配置只适合受信任的开发网络。
#
# 用法：
#   MONGO_REPLICA_HOST=192.168.30.65:27017 bash bootstrap-mongodb.sh [容器名]
#   默认容器名 lms-mongodb。MONGO_REPLICA_HOST 也可写进同目录的 .env（compose 也用同一个文件）。

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "${SCRIPT_DIR}/.env" ]; then
  set -a
  # shellcheck disable=SC1091
  . "${SCRIPT_DIR}/.env"
  set +a
fi

CONTAINER="${1:-lms-mongodb}"
MONGO_REPLICA_HOST="${MONGO_REPLICA_HOST:-localhost:27017}"

run_mongosh() { docker exec -i "${CONTAINER}" mongosh --quiet "mongodb://127.0.0.1:27017/admin" --eval "$1"; }

echo "等待 mongodb 就绪..."
ready=0
for _ in $(seq 1 60); do
  if run_mongosh 'db.adminCommand("ping").ok' >/dev/null 2>&1; then
    ready=1
    break
  fi
  sleep 2
done
if [ "${ready}" -ne 1 ]; then
  echo "mongodb 未在预期时间内就绪" >&2
  exit 1
fi

echo "初始化单节点副本集，成员地址 ${MONGO_REPLICA_HOST}"
run_mongosh "try { rs.status(); print('副本集已初始化，成员 ' + db.hello().me); } catch (e) { rs.initiate({_id: 'rs0', members: [{_id: 0, host: '${MONGO_REPLICA_HOST}'}]}); print('副本集初始化完成，成员 ${MONGO_REPLICA_HOST}'); }"

echo "等待副本集选出主节点..."
elected=0
for _ in $(seq 1 60); do
  if [ "$(run_mongosh 'db.hello().isWritablePrimary' 2>/dev/null | tr -d '[:space:]')" = "true" ]; then
    elected=1
    break
  fi
  sleep 2
done
if [ "${elected}" -ne 1 ]; then
  echo "副本集未产生主节点" >&2
  exit 1
fi

run_mongosh 'rs.status().members.map(m => m.name + " " + m.stateStr).join(", ")'
echo "完成。连接串形如 mongodb://<host>:27017/<库名>?replicaSet=rs0"
