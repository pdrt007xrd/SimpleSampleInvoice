#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="Release"
OUTPUT_DIR="./publish"
SKIP_MIGRATE="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration)
      CONFIGURATION="$2"
      shift 2
      ;;
    --output)
      OUTPUT_DIR="$2"
      shift 2
      ;;
    --skip-migrate)
      SKIP_MIGRATE="true"
      shift
      ;;
    -h|--help)
      echo "Uso: $0 [--configuration Release|Debug] [--output ./publish] [--skip-migrate]"
      exit 0
      ;;
    *)
      echo "Opcion no valida: $1"
      exit 1
      ;;
  esac
done

echo "==> Verificando dotnet"
command -v dotnet >/dev/null 2>&1 || { echo "dotnet no esta instalado"; exit 1; }

echo "==> Restore"
dotnet restore

echo "==> Build ($CONFIGURATION)"
dotnet build -c "$CONFIGURATION" --no-restore

if [[ "$SKIP_MIGRATE" == "false" ]]; then
  echo "==> Aplicando migraciones"
  if ! dotnet ef database update --no-build; then
    echo "No se pudieron aplicar migraciones automaticamente."
    echo "Verifica cadena de conexion y permisos de DB."
    exit 1
  fi
else
  echo "==> Migraciones omitidas por parametro --skip-migrate"
fi

echo "==> Publish en $OUTPUT_DIR"
dotnet publish -c "$CONFIGURATION" -o "$OUTPUT_DIR" --no-build

echo "==> Proceso completado"
echo "Salida lista para subir desde: $OUTPUT_DIR"
